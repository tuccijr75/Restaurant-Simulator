using System;
using System.Globalization;
using System.Text;

namespace RestaurantSimulator;

/// RS-RM-001 release gate: runs one full career week (7 days) twice from the same
/// WeekSeed and proves the entire longitudinal chain is deterministic — per-day
/// scenario/seed schedule, per-day event-stream hashes, the reputation trajectory,
/// and the final serialized career state. Also asserts the reputation model stays
/// within its documented bounds and actually responds to outcomes (not constant).
public static class CareerTest
{
    public static string Run(int weekSeed, out string weekSummaryJson)
    {
        var r = new StringBuilder();
        r.AppendLine($"CAREER-TEST week_seed={weekSeed} days={CareerState.DaysPerWeek}");

        var a = RunWeek(weekSeed);
        var b = RunWeek(weekSeed);

        bool dayHashesMatch = true, schedulesMatch = true;
        for (int i = 0; i < CareerState.DaysPerWeek; i++)
        {
            if (a.Days[i].EventStreamSha256 != b.Days[i].EventStreamSha256) dayHashesMatch = false;
            if (a.Days[i].Scenario != b.Days[i].Scenario || a.Days[i].Seed != b.Days[i].Seed) schedulesMatch = false;
        }
        Check(r, "deterministic week schedule (scenario+seed per day)", schedulesMatch);
        Check(r, "deterministic replay across week (7/7 day event-stream hashes)", dayHashesMatch);
        Check(r, "deterministic final career state JSON",
            Exports.Sha256Hex(a.ToJson()) == Exports.Sha256Hex(b.ToJson()));
        Check(r, "week complete (7 day records)", a.WeekComplete && a.Days.Count == CareerState.DaysPerWeek);
        Check(r, "day 0 anchored to normal_day", a.Days[0].Scenario == "normal_day");

        bool repBounded = true, multBounded = true, deltaBounded = true;
        bool repMoved = false;
        foreach (var d in a.Days)
        {
            if (d.ReputationAfter < CareerState.ReputationMin || d.ReputationAfter > CareerState.ReputationMax) repBounded = false;
            if (d.DemandMultiplierUsed < 0.85 || d.DemandMultiplierUsed > 1.05) multBounded = false;
            if (Math.Abs(d.ReputationDelta) > 6.0 + 1e-9) deltaBounded = false;
            if (Math.Abs(d.ReputationDelta) > 1e-9) repMoved = true;
        }
        Check(r, "reputation clamped to [40,100]", repBounded);
        Check(r, "demand multiplier clamped to [0.85,1.05]", multBounded);
        Check(r, "per-day reputation delta clamped to [-6,+6]", deltaBounded);
        Check(r, "reputation responds to outcomes (non-constant)", repMoved);
        Check(r, "career carryover changes day inputs (multiplier varies after day 0)",
            MultiplierVaries(a));
        Check(r, "career JSON round-trips (parse(serialize(x)) == x)",
            CareerState.FromJson(a.ToJson()).ToJson() == a.ToJson());

        foreach (var d in a.Days)
            r.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"  day {d.Day}: {d.Scenario,-22} seed={d.Seed,-9} mult={d.DemandMultiplierUsed:0.000} csat={d.Csat:0.0} insp={d.InspectionScore:0} orders={d.Orders} aband={d.AbandonedTickets} rep {d.ReputationBefore:0.0} -> {d.ReputationAfter:0.0} ({(d.ReputationDelta >= 0 ? "+" : "")}{d.ReputationDelta:0.00})"));
        r.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"final reputation={a.Reputation:0.00} next_multiplier={a.DemandMultiplier:0.000}"));

        weekSummaryJson = a.ToJson();
        return r.ToString();
    }

    static bool MultiplierVaries(CareerState c)
    {
        for (int i = 1; i < c.Days.Count; i++)
            if (Math.Abs(c.Days[i].DemandMultiplierUsed - c.Days[0].DemandMultiplierUsed) > 1e-9) return true;
        return false;
    }

    /// One full 7-day week, headless, using exactly the path Main.cs uses:
    /// schedule the day from career state, run it, fold the outcome back in.
    public static CareerState RunWeek(int weekSeed)
    {
        var c = new CareerState { WeekSeed = weekSeed };
        while (!c.WeekComplete)
        {
            string scenario = CareerState.ScenarioFor(weekSeed, c.DayIndex);
            int seed = CareerState.DaySeed(weekSeed, c.DayIndex);
            double mult = c.DemandMultiplier;

            var s = new SimRunState
            {
                Scenario = scenario, Seed = seed, TimeScale = 1.0, Running = true,
                ReputationDemandMultiplier = mult,
            };
            for (int i = 0; i < 1500 && !s.ShiftEnded; i++) s.Step(60);

            c.ApplyDayResult(scenario, seed, mult,
                s.Csat, s.InspectionScore, s.Orders, s.AbandonedTickets, s.Sales,
                Exports.Sha256Hex(s.AllJsonl));
        }
        return c;
    }

    static void Check(StringBuilder r, string name, bool ok) => r.AppendLine((ok ? "[PASS] " : "[FAIL] ") + name);
}
