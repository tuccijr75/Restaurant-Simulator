using System;
using System.Collections.Generic;
using System.Text;

namespace RestaurantSimulator;

// RS-ST-002: named-employee roster + rotating weekly schedule.
// Deterministic from a seed. Derives its crew head-count from the same daypart
// curve the sim schedules against (ScheduledCrewAt), so what you see on the floor
// equals what the sim has on the clock. Crew shifts are <=6h (most 4-5h) with
// labor-law breaks; managers cover the day as a rotating Manager-On-Duty.
// Read by the presentation layer (who is present, where) and the POS schedule panel.

public enum CrewRole { Crew, Lead, ShiftManager, AssistantManager, RestaurantManager }

public sealed class Employee
{
    public int Id;
    public string Name = "";
    public CrewRole Role;
    public string Home = "";   // usual station key, cosmetic

    // ---- RS-ST-003 static aptitudes (0-100), randomized once, never change ----
    // Each maps to exactly one downstream effect (see Roster.TickStats / Eff*).
    public int Reliability;  // attendance: lower => higher call-off / tardiness chance
    public int Speed;        // base work pace (feeds EffSpeed)
    public int Accuracy;     // base correctness (feeds EffAccuracy)
    public int Stamina;      // resists Fatigue accrual
    public int Composure;    // resists Stress accrual
    public int Teamwork;     // relieves Stress for the line; small assist to pace
    public int Disposition;  // personal attitude baseline (seeds starting Attitude)

    // ---- dynamic state (0-100), evolves over the shift/day ----
    public float Attitude;   // morale, pulled by management effectiveness + stress
    public float Motivation; // drifts up/down once Attitude crosses thresholds
    public float Stress;     // rises with station load, eased by Composure/Teamwork/breaks
    public float Fatigue;    // rises with time worked, eased by Stamina, recovers on break

    // Derived effectiveness in [0,1] — what the stats actually *do*. Display now;
    // can later scale sim throughput (separate, gate-rebaselined change).
    public float EffSpeed =>
        Mathf01(Speed) * (0.70f + 0.30f * Motivation / 100f)
                       * (1f - 0.30f * Stress / 100f)
                       * (1f - 0.25f * Fatigue / 100f)
                       * (1f + 0.05f * Teamwork / 100f);
    public float EffAccuracy =>
        Clamp01(Mathf01(Accuracy) * (1f - 0.40f * Stress / 100f - 0.30f * Fatigue / 100f)
                                  + 0.10f * Motivation / 100f);
    // Attendance risk for the day (used by call-off logic): lower reliability => higher.
    public float CallOffRisk => Clamp01((100 - Reliability) / 100f * 0.12f);

    static float Mathf01(int v) => v / 100f;
    static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;

    public void InitDynamic()
    {
        Attitude = Disposition;          // start at personal baseline
        Motivation = Disposition;        // motivation begins aligned with attitude
        Stress = 8f;
        Fatigue = 0f;
    }
}

public struct Shift
{
    public int EmployeeId;
    public string Name;
    public CrewRole Role;
    public int Start;          // minute-of-day [0,1440)
    public int End;            // exclusive
    public int BreakStart;     // 0 = no break
    public int BreakLen;       // minutes
    public string Station;     // assigned work_* key (or "floor"/"work_office")
    public bool IsMod;         // manager-on-duty for this shift

    public int LengthMin => End - Start;
    public bool ActiveAt(int m) => m >= Start && m < End;
    public bool OnBreakAt(int m) => BreakLen > 0 && m >= BreakStart && m < BreakStart + BreakLen;
}

public sealed class Roster
{
    public const int Open = 360;      // 06:00
    public const int Close = 1440;    // 24:00
    public const int MaxCrewShift = 360;   // 6h hard cap for crew (labor law)

    public readonly List<Employee> Employees = new();
    public readonly List<Shift>[] Days = new List<Shift>[7];

    // Critical-first station ladder: the k-th concurrent crew member mans Ladder[k-1].
    // So 3 crew => drive-thru + one counter + one cook (the guaranteed-manned set);
    // extra crew fan out to expo, fryer, assembly, second register, prep, beverage.
    static readonly string[] Ladder =
    {
        "work_dt", "work_counter", "work_grill", "work_expo", "work_fryer",
        "work_assembly", "work_counter2", "work_prep", "work_beverage"
    };

    // Daypart crew curve — MUST mirror SimRunState.ScheduledCrewAt so the visible
    // head-count matches the sim's on-clock crew.
    public static int ScheduledCrew(int m)
    {
        if (m < Open || m >= Close) return 0;
        if (m < 630) return 4;     // breakfast
        if (m < 660) return 3;     // mid-morning trough
        if (m < 705) return 6;     // lunch build
        if (m < 855) return 7;     // lunch peak
        if (m < 975) return 5;     // afternoon
        if (m < 1245) return 7;    // dinner
        if (m < 1320) return 5;    // dinner taper
        return 4;                  // late night
    }

    public Roster(int seed) { Build(seed); }

    static readonly string[] First =
    {
        "Mia","Liam","Ava","Noah","Sofia","Ethan","Zoe","Lucas","Maya","Diego",
        "Ivy","Owen","Nora","Kai","Lena","Jonah","Aria","Cruz","Tess","Rohan",
        "Elsa","Marco","Priya","Cole","Nadia","Felix","Quinn","Hana","Theo","Yara",
        "Dion","Esme","Reed","Lola","Sami"
    };
    static readonly string[] Last =
    {
        "Reyes","Park","Okafor","Nguyen","Silva","Brooks","Haddad","Costa","Frost","Mehta",
        "Bauer","Ortiz","Lowe","Kane","Shah","Voss","Adler","Munoz","Pace","Roth"
    };

    void Build(int seed)
    {
        var rng = new Rng(seed);

        // --- pool: ~34 employees (24 crew, 4 leads, 3 shift mgrs, 2 asst, 1 GM) ---
        int id = 0;
        var usedNames = new HashSet<string>();
        string PickName()
        {
            for (int tries = 0; tries < 200; tries++)
            {
                var n = First[rng.Next(First.Length)] + " " + Last[rng.Next(Last.Length)];
                if (usedNames.Add(n)) return n;
            }
            var fallback = First[rng.Next(First.Length)] + " " + Last[rng.Next(Last.Length)] + " " + (id + 1);
            usedNames.Add(fallback);
            return fallback;
        }
        // Triangular roll centered ~65, clamped [20,95]: individual variation, no zeros.
        int Stat() => Math.Clamp(30 + rng.Next(36) + rng.Next(36), 20, 95);
        void Add(CrewRole role, int count, string home)
        {
            for (int i = 0; i < count; i++)
            {
                var e = new Employee
                {
                    Id = id++, Role = role, Home = home, Name = PickName(),
                    Reliability = Stat(), Speed = Stat(), Accuracy = Stat(),
                    Stamina = Stat(), Composure = Stat(), Teamwork = Stat(),
                    Disposition = Math.Clamp(40 + rng.Next(31), 35, 75),
                };
                e.InitDynamic();
                Employees.Add(e);
            }
        }
        Add(CrewRole.Crew, 24, "work_grill");
        Add(CrewRole.Lead, 4, "work_expo");
        Add(CrewRole.ShiftManager, 3, "work_office");
        Add(CrewRole.AssistantManager, 2, "work_office");
        Add(CrewRole.RestaurantManager, 1, "work_office");

        var crew = Employees.FindAll(e => e.Role == CrewRole.Crew);
        var leads = Employees.FindAll(e => e.Role == CrewRole.Lead);
        var mgrs = Employees.FindAll(e => e.Role >= CrewRole.ShiftManager);
        foreach (var e in Employees) _byId[e.Id] = e;

        for (int day = 0; day < 7; day++)
            Days[day] = BuildDay(day, crew, leads, mgrs);
    }

    List<Shift> BuildDay(int day, List<Employee> crew, List<Employee> leads, List<Employee> mgrs)
    {
        var shifts = new List<Shift>();
        var usedToday = new HashSet<int>();

        // Rotate who works which day so everyone gets days off across the week.
        int crewPtr = (day * 7) % crew.Count;
        Employee NextCrew()
        {
            for (int tries = 0; tries < crew.Count; tries++)
            {
                var e = crew[crewPtr % crew.Count]; crewPtr++;
                if (usedToday.Add(e.Id)) return e;
            }
            return crew[crewPtr++ % crew.Count];   // pool exhausted (won't happen at these sizes)
        }

        // --- crew: decompose the daypart curve into horizontal layers ---------------
        // Layer k is staffed whenever ScheduledCrew(m) >= k; each maximal run becomes
        // one or more <=6h shifts. Coverage at any minute == ScheduledCrew(m) exactly,
        // and layer k always maps to the same station (critical stations are layers 1-3).
        int maxReq = 7;
        for (int k = 1; k <= maxReq; k++)
        {
            string station = Ladder[Math.Min(k - 1, Ladder.Length - 1)];
            int m = Open;
            while (m < Close)
            {
                if (ScheduledCrew(m) < k) { m++; continue; }
                int a = m;
                while (m < Close && ScheduledCrew(m) >= k) m++;
                int b = m;                       // run [a,b) needs one body on `station`
                int len = b - a;
                int chunks = (len + MaxCrewShift - 1) / MaxCrewShift;   // ceil
                for (int c = 0; c < chunks; c++)
                {
                    int s = a + (int)((long)len * c / chunks);
                    int e = a + (int)((long)len * (c + 1) / chunks);
                    shifts.Add(MakeShift(NextCrew(), s, e, station, false));
                }
            }
        }

        // --- managers: rotating Manager-On-Duty, exactly one at a time, all day ------
        int span = Close - Open;
        int modShifts = (span + 479) / 480;            // ~3 shifts of <=8h
        int mgrPtr = (day * 3) % mgrs.Count;
        for (int c = 0; c < modShifts; c++)
        {
            int s = Open + (int)((long)span * c / modShifts);
            int e = Open + (int)((long)span * (c + 1) / modShifts);
            var mgr = mgrs[mgrPtr % mgrs.Count]; mgrPtr++;
            shifts.Add(MakeShift(mgr, s, e, "work_office", true));
        }

        // --- leads: floor support across the two peaks ------------------------------
        int leadPtr = day % leads.Count;
        foreach (var (s, e) in new[] { (705, 855), (1020, 1245) })
        {
            var ld = leads[leadPtr % leads.Count]; leadPtr++;
            shifts.Add(MakeShift(ld, s, e, "work_expo", false));
        }

        return shifts;
    }

    static Shift MakeShift(Employee emp, int start, int end, string station, bool mod)
    {
        var sh = new Shift
        {
            EmployeeId = emp.Id, Name = emp.Name, Role = emp.Role,
            Start = start, End = end, Station = station, IsMod = mod
        };
        int len = end - start;
        // Labor-law break: 30-min meal over 5h, else a 10-min rest for >=2h. Mid-shift.
        if (len > 300) { sh.BreakLen = 30; sh.BreakStart = start + len / 2 - 15; }
        else if (len >= 120) { sh.BreakLen = 10; sh.BreakStart = start + len / 2 - 5; }
        return sh;
    }

    // Everyone on the clock at (day, minute), with live break state.
    public IEnumerable<(Shift shift, bool onBreak)> PresentAt(int day, int minute)
    {
        foreach (var s in Days[day % 7])
            if (s.ActiveAt(minute)) yield return (s, s.OnBreakAt(minute));
    }

    // ---- RS-ST-003 stat evolution ------------------------------------------------
    // Rates are per sim-minute. Slow knobs (attitude/motivation) move over a shift;
    // fast knobs (stress) track rushes within minutes. All values clamp to [0,100].
    const float MgrAttRate = 0.50f, CrewAttRate = 0.35f;   // how fast attitude chases its target
    const float MotivRise = 0.05f, MotivFall = 0.06f;      // motivation drift once past a threshold
    const float MotivHi = 70f, MotivLo = 45f;              // dead zone: rise above 70, fall below 45
    const float StressUp = 0.90f, StressDown = 0.50f;      // stress climbs faster than it recovers
    const float FatigueAccrue = 0.10f, FatigueRecover = 1.20f;

    readonly Dictionary<int, Employee> _byId = new();

    static float Approach(float v, float t, float step) => v < t ? Math.Min(t, v + step) : v > t ? Math.Max(t, v - step) : v;
    static float Clampf(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;

    // Advance dynamic stats for everyone currently on shift.
    //   storePerf  : 1.0 = normal day; >1 above normal (boosts managers), <1 below.
    //   stationLoad: station-key -> 0..1 busyness (drives crew stress at their post).
    // Chain: store performance -> manager attitude -> crew attitude -> (threshold) -> motivation.
    public void TickStats(int day, int minute, float dtMin, float storePerf, Func<string, float>? stationLoad)
    {
        if (dtMin <= 0) return;
        // 1) managers on duty react to how the store is running, then set the tone.
        float mgrSum = 0; int mc = 0;
        foreach (var (s, _) in PresentAt(day, minute))
        {
            if (s.Role < CrewRole.ShiftManager || !_byId.TryGetValue(s.EmployeeId, out var m)) continue;
            float target = Clampf(55f + (storePerf - 1f) * 60f, 20f, 95f);   // above normal -> upbeat
            m.Attitude = Approach(m.Attitude, target, MgrAttRate * dtMin);
            mgrSum += m.Attitude; mc++;
        }
        float mgmt = mc > 0 ? mgrSum / mc : 50f;
        // 2) crew + leads: management tone lifts attitude; station load drives stress.
        foreach (var (s, onBreak) in PresentAt(day, minute))
        {
            if (s.Role >= CrewRole.ShiftManager || !_byId.TryGetValue(s.EmployeeId, out var e)) continue;
            float load = stationLoad != null ? Clampf(stationLoad(s.Station), 0f, 1f) : 0f;

            float stressTarget = Clampf(load * 100f - e.Composure * 0.40f - e.Teamwork * 0.10f, 0f, 100f);
            if (onBreak) stressTarget *= 0.3f;
            e.Stress = Approach(e.Stress, stressTarget, (e.Stress < stressTarget ? StressUp : StressDown) * dtMin);

            if (onBreak) e.Fatigue = Math.Max(0f, e.Fatigue - FatigueRecover * dtMin);
            else e.Fatigue = Clampf(e.Fatigue + FatigueAccrue * (1f - e.Stamina / 200f) * dtMin, 0f, 100f);

            float attTarget = Clampf(e.Disposition + (mgmt - 50f) * 0.7f - e.Stress * 0.12f, 10f, 95f);
            e.Attitude = Approach(e.Attitude, attTarget, CrewAttRate * dtMin);

            if (e.Attitude > MotivHi) e.Motivation = Clampf(e.Motivation + MotivRise * dtMin, 0f, 100f);
            else if (e.Attitude < MotivLo) e.Motivation = Clampf(e.Motivation - MotivFall * dtMin, 0f, 100f);
        }
    }

    public Shift? ModAt(int day, int minute)
    {
        foreach (var s in Days[day % 7])
            if (s.IsMod && s.ActiveAt(minute)) return s;
        return null;
    }

    static string Hhmm(int m) => $"{(m / 60) % 24:00}:{m % 60:00}";

    // Compact day view for the POS schedule panel.
    public string DayReport(int day)
    {
        var sb = new StringBuilder();
        var list = new List<Shift>(Days[day % 7]);
        list.Sort((x, y) => x.Start != y.Start ? x.Start.CompareTo(y.Start) : x.Station.CompareTo(y.Station));
        foreach (var s in list)
        {
            string br = s.BreakLen > 0 ? $"  break {Hhmm(s.BreakStart)}-{Hhmm(s.BreakStart + s.BreakLen)}" : "";
            string role = s.IsMod ? "MOD" : s.Role.ToString();
            sb.Append($"{Hhmm(s.Start)}-{Hhmm(s.End)}  {s.Name,-16} {role,-18} {s.Station}{br}\n");
        }
        return sb.ToString();
    }

    // Tiny deterministic RNG (xorshift) so the roster is reproducible per seed.
    struct Rng
    {
        uint _s;
        public Rng(int seed) { _s = seed == 0 ? 0x9E3779B9u : (uint)seed; }
        public int Next(int n) { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return (int)(_s % (uint)n); }
    }
}
