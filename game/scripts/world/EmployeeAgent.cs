using Godot;

namespace RestaurantSimulator;

/// Crew/manager character bound to a station work spot. Animates "working" when
/// the simulation reports load at its station; occasionally walks a supply run
/// to the walk-in (visual flavor only — never feeds back into the sim).
public partial class EmployeeAgent : CharacterRig
{
    public string StationKey = "work_grill";
    public string Role = "";
    public Vector3 FaceTarget;
    public bool HasFace;
    public Vector3 HomeSpot, CoolerSpot;
    public bool OnBreak;
    public Vector3 BreakSpot;
    float _supplyTimer;
    bool _onSupplyRun;
    bool _sweeping;
    float _sweepTimer;
    bool _beatToggle;
    public bool Patrols;
    public System.Collections.Generic.List<Vector3> PatrolRoute = new();
    int _patrolIdx;
    float _patrolPause;
    Vector3 _greetPos;
    bool _greeting, _waved;
    float _greetTimer, _greetCooldown;
    const float GreetRange = 5f;
    System.Random _vis = new(7);

    /// Manager interrupts patrol to greet a newly arrived customer (approach if far, then face + wave).
    public void GoGreet(Vector3 customerPos)
    {
        if (!Patrols || _greetCooldown > 0f) return;
        _greetPos = customerPos; _greeting = true; _waved = false; _greetCooldown = 8f;
    }

    public void Init(int salt)
    {
        _vis = new System.Random(1000 + salt);
        _supplyTimer = 20 + _vis.Next(60);
        // RS-VS-002: each station drives a matching work animation on the model.
        // The value is matched (case-insensitive substring) against the model's
        // clip names; falls back to a generic work/idle clip if absent.
        WorkAnimKey = StationKey switch
        {
            "work_grill"    => "grill",
            "work_fryer"    => "fry",
            "work_assembly" => "assembl",
            "work_expo"     => "expo",
            "work_beverage" => "bev",
            "work_prep"     => "prep",
            "work_counter"  => "counter",
            "work_dt"       => "window",
            "work_office"   => "office",
            _               => "",
        };
    }

    public void Drive(float delta, bool stationBusy)
    {
        if (_greetCooldown > 0f) _greetCooldown -= delta;
        if (Patrols) { DrivePatrol(delta); return; }

        if (OnBreak)
        {
            if (Seated) return;                                   // sitting / mid-transition: hold
            if (StepToward(BreakSpot, delta)) { Working = false; RequestSeated(true); }  // arrived -> sit down
            else ActionAnim = "";                                 // still walking -> walk
            return;
        }
        if (Seated) { RequestSeated(false); return; }             // break ended -> stand up, hold until done

        if (_sweeping)
        {
            Working = false;
            ActionAnim = "sweeping";   // ping-pong loop is set on the clip itself
            _sweepTimer -= delta;
            if (_sweepTimer <= 0) { _sweeping = false; ActionAnim = ""; _supplyTimer = 45 + _vis.Next(90); }
            return;
        }
        if (_onSupplyRun)
        {
            if (StepToward(CoolerSpot, delta))
            {
                _onSupplyRun = false;
                _supplyTimer = 45 + _vis.Next(90);
            }
            return;
        }
        if (!StepToward(HomeSpot, delta)) return;
        Working = stationBusy;
        if (HasFace) FaceToward(FaceTarget, delta);   // turn to face the equipment while on station
        _supplyTimer -= delta;
        if (_supplyTimer <= 0 && !stationBusy)
        {
            // Alternate the idle beat between a sweeping pass (crew only) and a
            // walk-in supply run.
            bool canSweep = StationKey != "work_office";
            if (canSweep && _beatToggle) { _sweeping = true; _sweepTimer = 8 + _vis.Next(6); }
            else _onSupplyRun = true;
            _beatToggle = !_beatToggle;
        }
    }

    void DrivePatrol(float delta)
    {
        if (_greeting)
        {
            if (FlatDist(Position, _greetPos) > GreetRange) { StepToward(_greetPos, delta); return; }  // get within range
            Moving = false;
            FaceToward(_greetPos, delta);                                 // turn toward the customer
            if (!_waved) { TriggerOneShot("waving", 2.5f); _waved = true; _greetTimer = 2.5f; }
            _greetTimer -= delta;
            if (_greetTimer <= 0) _greeting = false;                      // done -> resume patrol
            return;
        }
        if (PatrolRoute.Count == 0) { Moving = false; return; }
        if (StepToward(PatrolRoute[_patrolIdx % PatrolRoute.Count], delta))  // arrived at a stop
        {
            Moving = false;
            _patrolPause -= delta;
            if (_patrolPause <= 0)
            {
                _patrolIdx = (_patrolIdx + 1) % PatrolRoute.Count;
                _patrolPause = 2.5f + _vis.Next(3);                       // pause and look over the station
            }
        }
    }

    static float FlatDist(Vector3 a, Vector3 b) { a.Y = 0; b.Y = 0; return (a - b).Length(); }
}
