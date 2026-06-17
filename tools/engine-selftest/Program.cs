using System;
using RestaurantSimulator;

// tools/engine-selftest — headless release gate.
// 1) SelfTest across all 10 scenarios at the canonical gate seed (proves
//    deterministic replay, lifecycle ordering, ledger reconciliation, no item.sold).
//    With RS-RM-001's ReputationDemandMultiplier defaulting to 1.0, every one of
//    these runs is byte-identical to the pre-career engine.
// 2) CareerTest — one 7-day career week run twice, proving the multi-day reputation
//    carryover chain is itself fully deterministic and bounded.

string[] scenarios =
{
    "normal_day", "slow_day", "rush_day", "weather_disruption", "staffing_call_off",
    "equipment_failure", "local_event_surge", "school_event_surge",
    "holiday_pattern", "multi_rush_condition",
};
const int GateSeed = 12345;

int pass = 0, fail = 0;
foreach (var scenario in scenarios)
{
    string report = SelfTest.Run(scenario, GateSeed);
    Console.WriteLine(report);
    foreach (var line in report.Split('\n'))
    {
        if (line.StartsWith("[PASS]")) pass++;
        else if (line.StartsWith("[FAIL]")) fail++;
    }
    Console.WriteLine();
}

int ingredientPass = 0, ingredientFail = 0;
Console.WriteLine("=================== INGREDIENT MODEL (RS-IM-001) ===================");
{
    string? catJson = null;
    foreach (var p in new[] { "../../game/config/ingredients.json", "game/config/ingredients.json" })
        if (System.IO.File.Exists(p)) { catJson = System.IO.File.ReadAllText(p); break; }
    var cat = new IngredientCatalog();
    cat.Load(catJson ?? IngredientCatalog.Embedded);

    SimRunState RunIng(string scn, int seed)
    {
        var s = new SimRunState { Scenario = scn, Seed = seed, TimeScale = 1.0, Running = true,
            EnableRealIngredients = true, Catalog = cat };
        for (int i = 0; i < 1500 && !s.ShiftEnded; i++) s.Step(60);
        return s;
    }
    SimRunState RunPlain(string scn, int seed)
    {
        var s = new SimRunState { Scenario = scn, Seed = seed, TimeScale = 1.0, Running = true };
        for (int i = 0; i < 1500 && !s.ShiftEnded; i++) s.Step(60);
        return s;
    }

    var ia = RunIng("normal_day", 12345);
    var ib = RunIng("normal_day", 12345);
    void IC(string name, bool ok) { Console.WriteLine((ok ? "[PASS] " : "[FAIL] ") + name); if (ok) ingredientPass++; else ingredientFail++; }

    IC("catalog loaded (34 ingredients)", cat.Loaded && cat.Items.Count == 34);
    IC("real ingredient ledger active", ia.RealIngredientsActive);
    IC("deterministic ingredient ledger (hash match)",
        Exports.Sha256Hex(ia.IngredientLedgerJson) == Exports.Sha256Hex(ib.IngredientLedgerJson));
    IC("every ingredient reconciles", !ia.IngredientLedgerJson.Contains("\"reconciles\":false"));
    IC("per-item waste cost realistic (< legacy bucket waste cost)",
        ia.IngredientWasteCostUsd < ia.WasteCost && ia.IngredientWasteCostUsd >= 0);
    IC("waste spans multiple ingredients on their own clocks",
        System.Text.RegularExpressions.Regex.Matches(ia.IngredientWasteByItemJson, "cost_usd").Count >= 2);
    IC("enabling ingredients leaves event stream byte-identical (replay neutral)",
        Exports.Sha256Hex(ia.AllJsonl) == Exports.Sha256Hex(RunPlain("normal_day", 12345).AllJsonl));

    var bundle = Exports.BuildAll(ia, "2026-01-15T12:00:00Z");
    bool hasInvLedger = bundle.Exists(f => f.Name == "inventory_ledger.json" && f.Content.Contains("per_ingredient_hold_and_waste"));
    bool noSeparateIngFile = !bundle.Exists(f => f.Name == "ingredient_ledger.json");
    IC("legacy bucket ledger retired: inventory_ledger.json carries the per-ingredient ledger", hasInvLedger);
    IC("no separate ingredient_ledger.json file (folded into inventory_ledger)", noSeparateIngFile);
    var plainBundle = Exports.BuildAll(RunPlain("normal_day", 12345), "2026-01-15T12:00:00Z");
    IC("legacy mode still emits bucket inventory_ledger", plainBundle.Exists(f => f.Name == "inventory_ledger.json" && f.Content.Contains("inventory_item_id")));

    Console.WriteLine($"ingredient_waste=${ia.IngredientWasteCostUsd:0.00} ({ia.IngredientWasteUnits:0} units) vs legacy_bucket_waste=${ia.WasteCost:0.00}");
    Console.WriteLine($"INGREDIENT-MODEL TOTAL: {ingredientPass}/{ingredientPass + ingredientFail} checks passed");
    Console.WriteLine();
}

Console.WriteLine("=================== CAREER MODE (RS-RM-001) ===================");
string careerReport = CareerTest.Run(weekSeed: 777001, out string weekSummaryJson);
Console.WriteLine(careerReport);
int careerPass = 0, careerFail = 0;
foreach (var line in careerReport.Split('\n'))
{
    if (line.StartsWith("[PASS]")) careerPass++;
    else if (line.StartsWith("[FAIL]")) careerFail++;
}

try
{
    System.IO.File.WriteAllText("career_week_summary.json", weekSummaryJson);
    Console.WriteLine("wrote career_week_summary.json");
}
catch (Exception e) { Console.WriteLine("could not write career_week_summary.json: " + e.Message); }

Console.WriteLine();
Console.WriteLine($"SELF-TEST TOTAL: {pass}/{pass + fail} checks passed across {scenarios.Length} scenarios (seed {GateSeed})");
Console.WriteLine($"INGREDIENT-MODEL TOTAL: {ingredientPass}/{ingredientPass + ingredientFail} checks passed");
Console.WriteLine($"CAREER-TEST TOTAL: {careerPass}/{careerPass + careerFail} checks passed (week_seed 777001)");
Console.WriteLine((fail == 0 && careerFail == 0 && ingredientFail == 0) ? "RESULT: PASS" : "RESULT: FAIL");
Environment.Exit(fail == 0 && careerFail == 0 && ingredientFail == 0 ? 0 : 1);
