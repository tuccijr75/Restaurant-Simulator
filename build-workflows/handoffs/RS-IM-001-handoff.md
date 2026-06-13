# RS-IM-001 Handoff

## New / touched
- NEW game/config/ingredients.json, game/scripts/sim/IngredientCatalog.cs,
  game/scripts/sim/IngredientLedger.cs, docs/09_INGREDIENT_MODEL.md
- EDIT game/scripts/sim/SimRunState.cs (opt-in ledger: field, shift-start open,
  item.taken consume, per-minute tick), Exports.cs (ingredient_ledger.json +
  end_of_shift ingredient_waste_*), CareerHook.cs (auto-enable + F8 hardened export),
  tools/engine-selftest (ingredient test + compile includes).

## Run the gate
    cd tools/engine-selftest && dotnet run -c Release
Expect 120/120 + 7/7 + 11/11, RESULT: PASS.

## In Godot (after the CareerHook autoload from RS-RM-001 is registered)
- Real ingredients are on automatically for every run.
- F5 = Main's export (may still crash — see below). F8 = hardened export to
  user://outputs/sim_<scenario>_<seed>/ (creates dir, null-checked) incl. ingredient_ledger.json.
- If F5 still crashes, paste the Godot console trace at the crash; likely a
  missing-user://outputs NRE in Main's writer, a 2-line fix once confirmed.

## Tuning (operator_calibration_required)
Hold times/temps/costs live in config/ingredients.json. JIT batch sizes in
IngredientLedger.Batch(). All editable without code changes (except batch sizes).

## Next recommended
RS-IM-002: extend the ASC compatibility profile (RS-CX-001) to validate the
per-ingredient ledger schema; then retire the legacy bucket inventory_ledger.json.

---
## UPDATE (from first in-editor F8 export, seed 132195522)
F8 export confirmed working in Godot. Fixed three issues the export surfaced:
1. Phantom waste ($15.41 of $25.80): perishables were seeded at open even when the
   menu never used them. Now perishables open EMPTY and are produced just-in-time,
   so unused items never waste. Phantom now $0.
2. Lettuce/tomato never consumed: classic_burger BOM now includes shredded_lettuce
   + tomato_slice, so produce flows through (491 each on the sample day).
3. Legacy signals still visible: when EnableRealIngredients is on, the bucket
   "prep quality low"/"prep low" alerts and "prep_more" recommendation are
   suppressed, end_of_shift waste headline = real values (legacy under
   legacy_bucket_waste_*), and the inspection "expired hold product" note is
   driven by the real model. With the model off, everything reverts (120/120 intact).
Career week test now also runs with real ingredients on, matching in-editor.
Verified: real waste $31.76/110u, 0 phantom, 0 prep-quality-low alerts, inspection
100/clean. Gate 120/120 + 7/7 + 11/11 PASS.
