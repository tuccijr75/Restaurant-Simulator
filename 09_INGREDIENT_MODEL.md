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

## Engine wiring (the build, not yet landed)

This is the substantial part and the part that needs in-editor verification. The
plan, staged so each step is independently testable headless:

1. **`IngredientCatalog.cs`** — load `ingredients.json` (res:// in Godot,
   filesystem in the harness), with an embedded default equal to the JSON so an
   absent file reproduces identical runs (same pattern as `SimConfig`). Exposes
   `HoldTime(id)`, `TempRange(id)`, `Cost(id)`, `Bom(menuItem)`.

2. **Per-item hold clocks** — replace the single `prepBatches` 30-min queue and
   the three pan hold limits with per-ingredient lots `{ingredientId, qty,
   readyAt}`. Expiry uses that ingredient's own `hold_time_min`; dry/ambient
   items with `null` don't expire intra-day. This directly removes the opening
   mass-expiry and lets lettuce, buns, and patties age on separate clocks.

3. **Per-item temperature** — each held lot inherits its storage unit's
   temperature; excursions are judged against that item's own range, so a cold
   lot and a hot lot are evaluated independently rather than against two globals.

4. **BOM-driven consumption** — an order consumes the menu item's bill of
   materials (per-ingredient `item.taken`), so the inventory ledger and waste
   report break out by real ingredient.

5. **Per-item waste + cost** — `RecordWaste` keys on ingredient id and multiplies
   by `unit_cost_usd`, giving an accurate `waste_cost_usd` by item in
   `end_of_shift_summary` and the inventory ledger.

### Compatibility / determinism

The catalog is a deterministic input (same catalog + scenario + seed ⇒ same
stream). The output contract keeps reconciling, but the inventory ledger and
waste lines expand from 7 buckets to per-ingredient rows — a schema addition the
ASC compatibility profile (RS-CX-001) must be extended to cover. The headless
self-test must be re-baselined: replay determinism and lifecycle ordering are
unchanged, but absolute waste/quality numbers will move (that is the point).

### Scope note

Steps 1–5 replace the inventory core of `SimRunState`, so this is a genuine task,
not a tweak, and it must be smoke-tested in Godot 4.6 (no Godot in the build
environment). Until it lands, the engine remains at the stable baseline
(≈2% abandonment, passes the 120/120 gate); the catalog ships as the data
foundation it will run on.
