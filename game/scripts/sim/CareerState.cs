using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RestaurantSimulator;

/// RS-RM-001 — multi-day career mode.
///
/// A "career week" is 7 business days driven by one WeekSeed. Each day gets a
/// deterministic (scenario, seed) pair derived from (WeekSeed, DayIndex), and the
/// store's reputation carries over between days as a bounded demand multiplier.
/// Same WeekSeed => same scenarios, same seeds, same reputation trajectory, same
/// event streams — the single-day determinism doctrine extended longitudinally.
///
/// Security/privacy doctrine (control-pack 00): reputation is a STORE-level
/// synthetic signal derived from store-level outcomes (csat, inspection,
/// abandonment). It is never attributed to an individual employee and no
/// per-employee performance score exists anywhere in this module.
public sealed class CareerState
{
    public const string SchemaVersion = "1.0.0";
    public const string GeneratorVersion = "game-0.3.0";

    public const double ReputationStart = 70.0;
    public const double ReputationMin = 40.0;
    public const double ReputationMax = 100.0;
    public const int DaysPerWeek = 7;

    public int WeekSeed = 777001;
    public int DayIndex;                       // 0..6 = next day to run; 7 = week complete
    public double Reputation = ReputationStart;
    public readonly List<DayRecord> Days = new();

    public sealed class DayRecord
    {
        public int Day;                        // 0-based index within the week
        public string Scenario = "";
        public int Seed;
        public double ReputationBefore;        // reputation entering the day
        public double DemandMultiplierUsed;    // multiplier the day was run with
        public double Csat;
        public double InspectionScore;         // engine convention: < 0 = no inspection that day
        public int Orders;
        public int AbandonedTickets;
        public double SalesUsd;
        public double ReputationDelta;
        public double ReputationAfter;
        public string EventStreamSha256 = "";
    }

    // ----- deterministic week scheduling ---------------------------------------

    /// Scenario pool for days 1-6. normal_day is weighted 3x so a typical week is
    /// anchored in ordinary operations with 2-4 disruption days; day 0 is always
    /// normal_day to establish the week's baseline.
    static readonly string[] Pool =
    {
        "normal_day", "normal_day", "normal_day",
        "rush_day", "slow_day", "equipment_failure", "staffing_call_off",
        "weather_disruption", "local_event_surge", "school_event_surge",
        "holiday_pattern", "multi_rush_condition",
    };

    /// FNV-1a style integer mix — stable across runtimes (no string hashing, no
    /// platform-dependent GetHashCode), so weeks replay identically anywhere.
    static uint Mix(int weekSeed, int day, int salt)
    {
        unchecked
        {
            uint h = 2166136261u;
            void Eat(uint v) { h ^= v; h *= 16777619u; }
            Eat((uint)weekSeed); Eat((uint)day * 0x9E3779B9u); Eat((uint)salt * 0x85EBCA6Bu);
            h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
            return h;
        }
    }

    public static string ScenarioFor(int weekSeed, int day) =>
        day <= 0 ? "normal_day" : Pool[Mix(weekSeed, day, 1) % (uint)Pool.Length];

    /// Per-day engine seed, kept positive and away from 0.
    public static int DaySeed(int weekSeed, int day) => (int)(Mix(weekSeed, day, 2) % 900000000u) + 1000;

    // ----- reputation model ------------------------------------------------------

    /// Demand multiplier from current reputation. 70 (start) => 0.90; the band is
    /// clamped to [0.85, 1.05] so reputation can never starve or flood the store
    /// outside calibrated daily bands. Slope and band are operator_calibration_required
    /// (docs/08_CAREER_MODE.md) — synthetic defaults, not field-measured constants.
    public double DemandMultiplier =>
        Math.Clamp(0.90 + (Reputation - ReputationStart) * 0.005, 0.85, 1.05);

    /// Fold one finished day into reputation. Inputs are store-level outcomes only.
    /// delta = 0.08*(csat-85) + 0.05*(inspection-80 when inspected) - 100*max(0, abandonRate-0.05),
    /// clamped to [-6, +6] per day so one bad day dents — never destroys — a store.
    public static double ReputationDelta(double csat, double inspectionScore, int orders, int abandoned)
    {
        double delta = 0.08 * (csat - 85.0);
        if (inspectionScore >= 0) delta += 0.05 * (inspectionScore - 80.0);
        double abandonRate = orders > 0 ? (double)abandoned / orders : 0;
        delta -= 100.0 * Math.Max(0, abandonRate - 0.05);
        return Math.Clamp(delta, -6.0, 6.0);
    }

    /// Record a completed day and advance the career clock.
    public DayRecord ApplyDayResult(string scenario, int seed, double multiplierUsed,
        double csat, double inspectionScore, int orders, int abandoned, double salesUsd, string eventStreamSha)
    {
        double before = Reputation;
        double delta = ReputationDelta(csat, inspectionScore, orders, abandoned);
        Reputation = Math.Clamp(Reputation + delta, ReputationMin, ReputationMax);
        var rec = new DayRecord
        {
            Day = DayIndex, Scenario = scenario, Seed = seed,
            ReputationBefore = before, DemandMultiplierUsed = multiplierUsed,
            Csat = csat, InspectionScore = inspectionScore,
            Orders = orders, AbandonedTickets = abandoned, SalesUsd = salesUsd,
            ReputationDelta = delta, ReputationAfter = Reputation,
            EventStreamSha256 = eventStreamSha,
        };
        Days.Add(rec);
        DayIndex++;
        return rec;
    }

    public bool WeekComplete => DayIndex >= DaysPerWeek;

    // ----- persistence (stable hand-rolled JSON, invariant culture) -------------

    static string Num(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

    public string ToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{\"schema_version\":\"").Append(SchemaVersion)
          .Append("\",\"generator_version\":\"").Append(GeneratorVersion)
          .Append("\",\"synthetic_data\":true,\"data_classification\":\"INTERNAL_SIM\"")
          .Append(",\"week_seed\":").Append(WeekSeed)
          .Append(",\"day_index\":").Append(DayIndex)
          .Append(",\"reputation\":").Append(Num(Reputation))
          .Append(",\"demand_multiplier_next\":").Append(Num(DemandMultiplier))
          .Append(",\"days\":[");
        for (int i = 0; i < Days.Count; i++)
        {
            var d = Days[i];
            if (i > 0) sb.Append(',');
            sb.Append("{\"day\":").Append(d.Day)
              .Append(",\"scenario\":\"").Append(d.Scenario)
              .Append("\",\"seed\":").Append(d.Seed)
              .Append(",\"reputation_before\":").Append(Num(d.ReputationBefore))
              .Append(",\"demand_multiplier_used\":").Append(Num(d.DemandMultiplierUsed))
              .Append(",\"customer_satisfaction_avg\":").Append(Num(d.Csat))
              .Append(",\"health_inspection_score\":").Append(Num(d.InspectionScore))
              .Append(",\"orders_total\":").Append(d.Orders)
              .Append(",\"abandoned_tickets\":").Append(d.AbandonedTickets)
              .Append(",\"sales_usd\":").Append(Num(d.SalesUsd))
              .Append(",\"reputation_delta\":").Append(Num(d.ReputationDelta))
              .Append(",\"reputation_after\":").Append(Num(d.ReputationAfter))
              .Append(",\"event_stream_sha256\":\"").Append(d.EventStreamSha256)
              .Append("\"}");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    /// Minimal tolerant parser for our own stable output (no external JSON deps in
    /// the engine — same constraint as the rest of game/scripts/sim).
    public static CareerState FromJson(string json)
    {
        var c = new CareerState
        {
            WeekSeed = (int)Number(json, "\"week_seed\":", 777001),
            DayIndex = (int)Number(json, "\"day_index\":", 0),
            Reputation = Number(json, "\"reputation\":", ReputationStart),
        };
        int at = json.IndexOf("\"days\":[", StringComparison.Ordinal);
        if (at >= 0)
        {
            int i = at;
            while ((i = json.IndexOf("{\"day\":", i + 1, StringComparison.Ordinal)) >= 0)
            {
                int end = json.IndexOf('}', i);
                if (end < 0) break;
                string obj = json[i..(end + 1)];
                c.Days.Add(new DayRecord
                {
                    Day = (int)Number(obj, "\"day\":", 0),
                    Scenario = Str(obj, "\"scenario\":\""),
                    Seed = (int)Number(obj, "\"seed\":", 0),
                    ReputationBefore = Number(obj, "\"reputation_before\":", ReputationStart),
                    DemandMultiplierUsed = Number(obj, "\"demand_multiplier_used\":", 1.0),
                    Csat = Number(obj, "\"customer_satisfaction_avg\":", 0),
                    InspectionScore = Number(obj, "\"health_inspection_score\":", -1),
                    Orders = (int)Number(obj, "\"orders_total\":", 0),
                    AbandonedTickets = (int)Number(obj, "\"abandoned_tickets\":", 0),
                    SalesUsd = Number(obj, "\"sales_usd\":", 0),
                    ReputationDelta = Number(obj, "\"reputation_delta\":", 0),
                    ReputationAfter = Number(obj, "\"reputation_after\":", ReputationStart),
                    EventStreamSha256 = Str(obj, "\"event_stream_sha256\":\""),
                });
                i = end;
            }
        }
        return c;
    }

    static double Number(string s, string key, double fallback)
    {
        int i = s.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return fallback;
        i += key.Length;
        int j = i;
        while (j < s.Length && (char.IsDigit(s[j]) || s[j] == '-' || s[j] == '.')) j++;
        return double.TryParse(s[i..j], NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : fallback;
    }

    static string Str(string s, string key)
    {
        int i = s.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return "";
        i += key.Length;
        int j = s.IndexOf('"', i);
        return j < 0 ? "" : s[i..j];
    }
}
