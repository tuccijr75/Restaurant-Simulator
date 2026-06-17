# RS-IM-001 — Real Per-Ingredient Inventory, Hold & Waste Model

Lane: Engine / Godot
Status: ready_for_review
Previous: RS-RM-001 — multi-day career mode

## Problem
Inventory was 7 abstract buckets under one near-flat rule (30-min prep clock + two
global temps), producing 60-77% phantom prep waste (the 260-unit opening prep
batch expiring daily) and a permanent "prep quality low" reading. Per-item hold
time/temperature were not modeled.

## Built
- game/config/ingredients.json — 34 real ingredients (storage state, own hold time,
  food-safe temp range, unit, cost) + 8 menu bills-of-materials + cook_to chains.
- game/scripts/sim/IngredientCatalog.cs — loader (res:// or fs) with embedded JSON.
- game/scripts/sim/IngredientLedger.cs — per-ingredient dated lots, each expiring on
  its own hold clock; cooking depletes raw source; per-item waste costed; temp audit.
- SimRunState — opt-in EnableRealIngredients/Catalog; opens ledger at shift start,
  consumes BOM at item.taken, ticks hold clocks per sim-minute. Replay-neutral.
- Exports — `inventory_ledger.json` becomes the per-ingredient ledger when the
  real model is active; end_of_shift gains ingredient_waste_* fields.
- CareerHook — enables the model on every Godot run; F5 remains the canonical
  Godot export path.

## Acceptance (headless, verified)
- 120/120 self-test unchanged (event stream byte-identical with model on vs off).
- 7/7 ingredient checks: catalog loads (34), ledger deterministic + reconciles per
  item, per-item waste < legacy bucket, waste spans multiple items, replay-neutral.
- normal_day seed 12345: real waste $46.54 (fries + proteins), zero on dry/cold/frozen,
  vs ~$472 legacy phantom. Full frozen->cooked->sold chain reconciles.

## Doctrine
Ingredient data is a deterministic input; no per-employee signal. Hold times/temps
are operator_calibration_required (FDA Food Code 2022 grounded).

## Blockers
In-editor Godot 4.6 smoke test (autoload + F5 export). If F5 fails, capture the
Godot console trace for the active `Main` export writer.
