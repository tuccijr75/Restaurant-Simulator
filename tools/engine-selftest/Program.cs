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
Console.WriteLine($"CAREER-TEST TOTAL: {careerPass}/{careerPass + careerFail} checks passed (week_seed 777001)");
Console.WriteLine((fail == 0 && careerFail == 0) ? "RESULT: PASS" : "RESULT: FAIL");
Environment.Exit(fail == 0 && careerFail == 0 ? 0 : 1);
