# RS-IM-001 Handoff

## New / touched
- NEW game/config/ingredients.json, game/scripts/sim/IngredientCatalog.cs,
  game/scripts/sim/IngredientLedger.cs, docs/09_INGREDIENT_MODEL.md
- EDIT game/scripts/sim/SimRunState.cs (opt-in ledger: field, shift-start open,
  item.taken consume, per-minute tick), Exports.cs (`inventory_ledger.json`
  becomes the per-ingredient ledger when real ingredients are active +
  end_of_shift ingredient_waste_*), CareerHook.cs (auto-enable real ingredients),
  Main.cs/F5 export path, tools/engine-selftest (ingredient test + compile includes).

## Run the gate
    cd tools/engine-selftest && dotnet run -c Release
Expect 120/120 + 7/7 + 11/11, RESULT: PASS.

## In Godot (after the CareerHook autoload from RS-RM-001 is registered)
- Real ingredients are on automatically for every run.
- F5 = Main's export to `user://outputs/sim_<scenario>_<seed>/` (creates dir,
  null-checked). When real ingredients are active, the per-ingredient ledger is
  inside `inventory_ledger.json`; no separate `ingredient_ledger.json` is emitted.
- If F5 fails, paste the Godot console trace at the crash so the active export
  writer can be fixed directly.

## Tuning (operator_calibration_required)
Hold times/temps/costs live in config/ingredients.json. JIT batch sizes in
IngredientLedger.Batch(). All editable without code changes (except batch sizes).

## Next recommended
RS-IM-002: extend the ASC compatibility profile (RS-CX-001) to validate the
per-ingredient `inventory_ledger.json` schema; keep the legacy bucket ledger only
for real-model-off mode.
