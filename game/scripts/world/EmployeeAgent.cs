using Godot;

namespace RestaurantSimulator;

/// Crew/manager character bound to a station work spot. Animates "working" when
/// the simulation reports load at its station; occasionally walks a supply run
/// to the walk-in (visual flavor only — never feeds back into the sim).
public partial class EmployeeAgent : CharacterRig
{
    public string StationKey = "work_grill";
    public string Role = "";
    public Vector3 HomeSpot, CoolerSpot;
    public bool OnBreak;
    public Vector3 BreakSpot;
    float _supplyTimer;
    bool _onSupplyRun;
    bool _sweeping;
    float _sweepTimer;
    bool _beatToggle;
    System.Random _vis = new(7);

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
}
