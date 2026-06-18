using Godot;

namespace RestaurantSimulator;

/// Crew/manager character bound to a station work spot. Animates "working" when
/// the simulation reports load at its station; occasionally walks a supply run
/// to the walk-in (visual flavor only — never feeds back into the sim).
public partial class EmployeeAgent : CharacterRig
{
    public string StationKey = "work_grill";
    public string Role = "";
    // RS-ST-002: identity + assignment carried from the roster (drives #12 click-inspect).
    public int EmpId = -1;
    public string EmpName = "";
    public string Assigned = "";
    public string Task = "On station";
    public Vector3 FaceTarget;
    public bool HasFace;
    public bool IsCashier;
    public Vector3 ServeSpot;
    public Vector3 HomeSpot, CoolerSpot;
    public bool OnBreak;
    public Vector3 BreakSpot;
    float _supplyTimer;
    bool _onSupplyRun;
    bool _sweeping;
    float _sweepTimer;
    float _returnHold;   // RS-ST-002 #12: manual "return to your station" override window
    bool _beatToggle;
    public bool Patrols;
    public System.Collections.Generic.List<Vector3> PatrolRoute = new();
    int _patrolIdx;
    float _patrolPause;
    Node3D? _greetTarget;
    bool _greeting, _waved;
    float _greetTimer, _greetCooldown;
    const float GreetRange = 5f;
    System.Random _vis = new(7);

    /// Manager interrupts patrol to greet a newly arrived customer (approach if far, then face + wave).
    public void GoGreet(Node3D customer)
    {
        if (!Patrols || _greetCooldown > 0f) return;
        _greetTarget = customer; _greeting = true; _waved = false; _greetCooldown = 8f;
    }

    /// #12: player command — drop the current idle errand and head back to the post.
    public void ReturnToStation()
    {
        _onSupplyRun = _sweeping = _greeting = false;
        ActionAnim = "";
        _returnHold = 30f;
        Task = "Returning to station";
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
            "work_counter2" => "counter",
            "work_dt"       => "window",
            "work_office"   => "office",
            _               => "",
        };
    }

    public void Drive(float delta, bool stationBusy)
    {
        if (_greetCooldown > 0f) _greetCooldown -= delta;

        // #12: player told them to get back to their post — overrides idle wandering
        // (supply runs, sweeping, patrol) for a short window, but never a scheduled break.
        if (_returnHold > 0f && !OnBreak)
        {
            _returnHold -= delta;
            Working = false;
            _onSupplyRun = _sweeping = _greeting = false;
            ActionAnim = "";
            if (StepToward(HomeSpot, delta) && HasFace) FaceToward(FaceTarget, delta);
            Task = "Returning to station";
            return;
        }

        if (Patrols) { DrivePatrol(delta); return; }

        if (OnBreak)
        {
            Task = "On break";
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
            Task = "Sweeping the lobby";
            _sweepTimer -= delta;
            if (_sweepTimer <= 0) { _sweeping = false; ActionAnim = ""; _supplyTimer = 45 + _vis.Next(90); }
            return;
        }
        if (_onSupplyRun)
        {
            Task = "Walk-in supply run";
            if (StepToward(CoolerSpot, delta))
            {
                _onSupplyRun = false;
                _supplyTimer = 45 + _vis.Next(90);
            }
            else if (DestinationBlockedSeconds > 2.0f)
            {
                _onSupplyRun = false;
                _supplyTimer = 18 + _vis.Next(30);
                Task = "Yielding aisle";
            }
            return;
        }
        // Cashiers step up to the register when a customer is ordering, otherwise stand back.
        Vector3 home = (IsCashier && stationBusy) ? ServeSpot : HomeSpot;
        if (!StepToward(home, delta)) { Task = "Walking to station"; return; }
        Working = stationBusy;
        if (HasFace) FaceToward(FaceTarget, delta);   // turn to face the equipment / customer while on station
        if (IsCashier) { Task = stationBusy ? "Serving register" : "At register"; return; }
        Task = stationBusy ? Assigned : "At station";
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
            Task = "Greeting a guest";
            if (_greetTarget == null || !IsInstanceValid(_greetTarget)) { _greeting = false; return; }  // guest already left
            Vector3 cpos = _greetTarget.Position; cpos.Y = 0;
            if (FlatDist(Position, cpos) > GreetRange)
            {
                if (!StepToward(cpos, delta) && DestinationBlockedSeconds > 2.0f) _greeting = false;
                return;
            }  // close the gap to the guest
            Moving = false;
            FaceToward(cpos, delta);                                      // turn toward the customer
            if (!_waved) { TriggerOneShot("waving", 2.5f); _waved = true; _greetTimer = 2.5f; }
            _greetTimer -= delta;
            if (_greetTimer <= 0) _greeting = false;                      // done -> resume patrol
            return;
        }
        if (PatrolRoute.Count == 0) { Moving = false; return; }
        Task = "Walking the floor";
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
