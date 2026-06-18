using System;
using System.Text.Json;

namespace RestaurantSimulator;

/// RS-CF-001: runtime-tunable calibration, loaded from config/realism_baseline.json
/// and config/human_behavior_profiles.json when present (Godot loads via res://,
/// the headless harness via the filesystem). Defaults below equal the previously
/// inlined constants, so an absent config reproduces identical runs. Config content
/// is a deterministic input: same (config, scenario, seed) => same event stream.
public static class SimConfig
{
    // ---- service-time floors / targets (seconds) — realism_baseline.service_time_seconds
    public static int DtSosFloor = 258, DtSosTarget = 335;
    public static int FcSosFloor = 180, FcSosTarget = 300;
    public static int MobileTarget = 480;
    public static int DelSosFloor = 420, DelTarget = 720;

    // ---- front-end service labor (RS-FE-001) — derived from Intouch drive-thru
    // decomposition; operator_calibration_required
    public static int DtIntakeSec = 112, DtHandoffSec = 92;
    public static int FcIntakeSec = 78, FcHandoffSec = 58;
    public static int MobHandoffSec = 64, DelHandoffSec = 118;

    // ---- hold pans (RS-HQ-001) — fries hold from docs/06; others operator_calibration_required
    public static int HoldLimitFriedMin = 20, HoldLimitGrilledMin = 15, HoldLimitFriesMin = 7;

    // ---- patience / abandonment (RS-HQ-001) — operator_calibration_required
    public static int PatienceDtSec = 720, PatienceLobbySec = 720, PatienceMobileSec = 1200, PatienceDeliverySec = 1500;
    public static int DtBalkQueueDepth = 9;
    public static int LobbyBalkQueueDepth = 8, MobileThrottleQueueDepth = 18, DeliveryThrottleQueueDepth = 18;

    // ---- satisfaction (RS-HQ-001)
    public static double CsatPassTarget = 75;
    public static int LastOrderMinute = 1380;

    // ---- food safety (FDA Food Code 2022)
    public static double ColdMaxF = 41, HotMinF = 135;

    // ---- equipment wear (RS-IN-001) — operator_calibration_required
    public static double WearKFryer = 180, WearKGrill = 220, WearKOther = 600;
    public static double MaintenanceCost = 40, TechCalloutCost = 150;

    // ---- behavior profiles (aggregate capacity modifiers; never per-person scoring)
    public static double PaceFactorMin = 0.88, PaceFactorMax = 1.12;
    static double[] _profilePace = { 1.0, 1.18, 0.78, 0.68, 0.95, 0.93, 1.02, 1.0 };

    public static bool BaselineLoaded, ProfilesLoaded;

    public static void LoadBaseline(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            if (r.TryGetProperty("service_time_seconds", out var st))
            {
                Tgt(st, "drive_thru_total", ref DtSosFloor, ref DtSosTarget);
                Tgt(st, "front_counter_total", ref FcSosFloor, ref FcSosTarget);
                if (st.TryGetProperty("mobile_ready", out var m) && m.TryGetProperty("target", out var mt)) MobileTarget = mt.GetInt32();
                Tgt(st, "delivery_kitchen_ready", ref DelSosFloor, ref DelTarget);
            }
            if (r.TryGetProperty("food_safety", out var fs))
            {
                if (fs.TryGetProperty("cold_holding_max_f", out var c)) ColdMaxF = c.GetDouble();
                if (fs.TryGetProperty("hot_holding_min_f", out var h)) HotMinF = h.GetDouble();
            }
            BaselineLoaded = true;
        }
        catch { /* keep defaults; config is optional */ }
    }

    static void Tgt(JsonElement st, string key, ref int floor, ref int target)
    {
        if (!st.TryGetProperty(key, out var e)) return;
        if (e.TryGetProperty("min", out var mn)) floor = mn.GetInt32();
        if (e.TryGetProperty("target", out var tg)) target = tg.GetInt32();
    }

    public static void LoadProfiles(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("profiles", out var ps)) return;
            var list = new System.Collections.Generic.List<double>();
            foreach (var p in ps.EnumerateArray())
                if (p.TryGetProperty("pace_multiplier", out var pm)) list.Add(pm.GetDouble());
            if (list.Count > 0) _profilePace = list.ToArray();
            ProfilesLoaded = true;
        }
        catch { }
    }

    /// Aggregate pace factor for today's seeded crew mix at a station — a blend of
    /// three profiles chosen deterministically from (seed, station). Clamped so the
    /// effect stays a capacity nuance, never an individual signal.
    public static double CrewPace(int seed, string station)
    {
        unchecked
        {
            ulong h = (ulong)seed * 1099511628211UL;
            foreach (var ch in station) h = (h ^ ch) * 16777619UL;
            double sum = 0;
            for (int i = 0; i < 3; i++) { h = h * 6364136223846793005UL + 1442695040888963407UL; sum += _profilePace[(int)(h % (ulong)_profilePace.Length)]; }
            return Math.Clamp(sum / 3.0, PaceFactorMin, PaceFactorMax);
        }
    }

    /// Mild aggregate fatigue late in a long shift (operator_calibration_required).
    public static double Fatigue(double shiftMinutes) =>
        shiftMinutes <= 480 ? 1.0 : Math.Max(0.94, 1.0 - (shiftMinutes - 480) / 600.0 * 0.06);
}
