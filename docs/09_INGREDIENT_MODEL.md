# 09 — Real Ingredient Model (RS-IM-001, design)

## Why

The shipped engine tracks inventory as seven abstract buckets (`main_protein`,
`side_base`, `drink_mix`, `prep_pack`, and three `cooked_*` pools) under nearly
one hold rule: a flat 30-minute prep clock and two global temperatures
(cold ≤41°F, hot ≥135°F). That single rule is the root cause of both the
"prep quality always low" reading and the 60–77% prep waste — e.g. the 260-unit
opening `prep_pack` batch expires ~220 units at 6:30 every day because one clock
governs buns, packaging, and produce alike. Tuning the par cannot fix an
abstraction that is wrong; the fix is to model real ingredients.

## The catalog (`game/config/ingredients.json`)

34 ingredients, each carrying its own:

- **storage state** — frozen, cooler, prep_cold, dry, ambient, hot_hold
- **state** — raw, prepped, cooked, ready
- **hold_time_min** — the quality/TPHC hold once the item reaches its held state;
  `null` = shelf-stable within a business day. Seven distinct values are in use:
  fries 7, cooked proteins 20, sausage/biscuit/toasted bun 30, brewed coffee 60,
  sliced tomato 120, shredded lettuce 240, diced onion 360.
- **temp_min_f / temp_max_f** — the food-safe holding range (FDA Food Code 2022):
  hot-held ≥135°F, cold-held ≤41°F, frozen ≤0°F, dry/ambient not temperature-controlled.
- **unit + unit_cost_usd** — for per-item waste costing.
- **cook_to / pan** — raw→cooked transitions and which hold pan a cooked item lands in.

Menu items carry a **bill of materials** (e.g. a deluxe burger = 2 cooked beef
patties + bun + 2 cheese + lettuce + 2 tomato + sauce + wrap), so consumption and
waste resolve to real ingredients instead of a generic "prep_pack."

All values are `operator_calibration_required` synthetic defaults grounded in
common QSR practice and the FDA Food Code — not a specific brand's standards.

## Engine wiring (landed — additive, replay-neutral)

Implemented as an additive accounting layer so the proven throughput engine
(orders, abandonment, SOS, staffing) is untouched and every existing
`(scenario, seed)` replay stays byte-identical (verified: the 120/120 self-test
passes unchanged, and a dedicated check confirms the event-stream hash is
identical with the model on vs off). The real ledger is driven by the same
deterministic `item.taken` stream.

1. **`IngredientCatalog.cs`** — loads `ingredients.json` (res:// in Godot,
   filesystem in the harness) with the JSON embedded as a fallback, so an absent
   file still runs identically. Exposes per-item hold/temp/cost, the menu BOMs,
   and the cooked→raw map.

2. **`IngredientLedger.cs`** — per-ingredient dated lots. Perishable items expire
   on their own `hold_time_min` (fries 7, cooked proteins 20, sliced tomato 120,
   lettuce 240, …); dry/frozen/cold-held items don't expire intra-day. Cooking a
   batch draws down its raw source (frozen patty → cooked patty). Per-item waste
   is costed at unit price; temperatures are audited against each item's own range.

3. **`SimRunState`** — opt-in via `EnableRealIngredients` (+ `Catalog`). Off by
   default (determinism); the Godot `CareerHook` turns it on for every run. Opens
   the ledger at shift start, consumes the menu BOM at each `item.taken`, ticks
   hold clocks and temp audit once per sim-minute.

4. **Output** — `inventory_ledger.json` becomes the per-ingredient ledger whenever
   the model is active, and `end_of_shift_summary.json` gains
   `ingredient_waste_cost_usd` / `ingredient_waste_units` / `ingredient_waste_by_item`.
   The legacy bucket inventory ledger is emitted only when the real model is off.

### Result

On career day 0 (`normal_day` seed 132195522), real food-cost waste is **$31.76 /
110 units**, entirely on items actually sold: cooked beef and chicken (20-min
hold tails), fries (7-min hold), and a small sliced-tomato tail — **zero** waste
on buns, packaging, frozen, cheese, sauces, lettuce, or onion (longer holds cover
them), and **zero phantom waste** (perishables are produced just-in-time, so an
item the day never sells is never made). The legacy bucket model reported ~$488
of phantom waste for the same run, almost all of it the opening prep batch dying
under one flat clock. Every ingredient reconciles; the frozen→cooked→sold chain
is deterministic.

### When the real model is active, it owns the user-visible signals

So the operator sees one coherent picture (not the old bucket artifacts):

- The headline `waste_units` / `waste_cost_usd` in `end_of_shift_summary.json` are
  the real per-ingredient values; the bucket figures move to
  `legacy_bucket_waste_*`.
- The legacy "prep quality low" / "prep low" warnings and the "prep_more"
  recommendation are suppressed — they were artifacts of the flat 30-minute
  `prep_pack` clock. (Real hold quality is governed per item by the catalog.)
- The health-inspection "expired hold product observed" deduction is driven by the
  real model rather than bucket prep expiry.

When the model is off (the 120/120 self-test), every one of these reverts exactly,
so legacy determinism is untouched.

### Export

Godot exports through **F5** in `Main`: it creates
`user://outputs/sim_<scenario>_<seed>/`, writes the contract files returned by
`Exports.BuildAll`, and null-checks `FileAccess`. When real ingredients are
active, the per-ingredient ledger is embedded in `inventory_ledger.json`; the
separate `ingredient_ledger.json` file is no longer emitted.
