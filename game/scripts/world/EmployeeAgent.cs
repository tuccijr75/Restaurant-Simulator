using Godot;

namespace RestaurantSimulator;

/// Crew/manager character bound to a station work spot. Animates "working" when
/// the simulation reports load at its station; occasionally walks a supply run
/// to the walk-in (visual flavor only — never feeds back into the sim).
public partial class EmployeeAgent : CharacterRig
{
    public string StationKey = "work_grill";
    public Vector3 HomeSpot, CoolerSpot;
    public bool OnBreak;
    public Vector3 BreakSpot;
    float _supplyTimer;
    bool _onSupplyRun;
    System.Random _vis = new(7);

    public void Init(int salt)
    {
        _vis = new System.Random(1000 + salt);
        _supplyTimer = 20 + _vis.Next(60);
    }

    public void Drive(float delta, bool stationBusy)
    {
        if (OnBreak)
        {
            if (StepToward(BreakSpot, delta)) { Moving = false; Working = false; }
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
        if (_supplyTimer <= 0 && !stationBusy) _onSupplyRun = true;
    }
}
