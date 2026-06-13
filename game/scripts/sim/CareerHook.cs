using Godot;
using System;
using RestaurantSimulator;

// RS-RM-001 — Godot career hook (autoload singleton).
//
// Deliberately decoupled from Main.cs: this node owns career persistence and
// the F6 "advance day" action, and exposes two methods Main calls. That keeps
// the engine-verified career logic (CareerState) isolated from the 3D scene
// layer, so the only in-editor step is registering this autoload and adding the
// two call sites documented in docs/08_CAREER_MODE.md.
//
// Persistence lives at user://career/career_state.json. Reputation is a
// store-level synthetic signal — no per-employee data is read or written here.
public partial class CareerHook : Node
{
    const string CareerDir = "user://career";
    const string CareerPath = "user://career/career_state.json";

    public CareerState State { get; private set; } = new();
    bool _dayConfigured;

    public override void _Ready()
    {
        Load();
        GD.Print($"[CareerHook] week_seed={State.WeekSeed} day={State.DayIndex}/{CareerState.DaysPerWeek} reputation={State.Reputation:0.0}");
    }

    /// Called by Main._Ready BEFORE starting the sim. Configures the day's
    /// scenario, seed, and reputation demand multiplier from career state.
    /// Returns false when the week is already complete (caller may show a summary).
    public bool ConfigureSim(SimRunState sim)
    {
        if (State.WeekComplete) { _dayConfigured = false; return false; }
        sim.Scenario = CareerState.ScenarioFor(State.WeekSeed, State.DayIndex);
        sim.Seed = CareerState.DaySeed(State.WeekSeed, State.DayIndex);
        sim.ReputationDemandMultiplier = State.DemandMultiplier;
        _dayConfigured = true;
        GD.Print($"[CareerHook] day {State.DayIndex}: {sim.Scenario} seed={sim.Seed} mult={sim.ReputationDemandMultiplier:0.000}");
        return true;
    }

    /// Called on F6. Folds the finished day's store-level outcomes into reputation,
    /// persists, then reloads the scene to run the next day. No-op if the current
    /// day wasn't configured/run (prevents double-stepping an empty shift).
    public void AdvanceDay(SimRunState sim)
    {
        if (!_dayConfigured)
        {
            GD.Print("[CareerHook] no active career day to advance");
            return;
        }
        var rec = State.ApplyDayResult(
            sim.Scenario, sim.Seed, sim.ReputationDemandMultiplier,
            sim.Csat, sim.InspectionScore, sim.Orders, sim.AbandonedTickets, sim.Sales,
            Exports.Sha256Hex(sim.AllJsonl));
        GD.Print($"[CareerHook] day {rec.Day} done: rep {rec.ReputationBefore:0.0} -> {rec.ReputationAfter:0.0} ({(rec.ReputationDelta >= 0 ? "+" : "")}{rec.ReputationDelta:0.00})");
        Save();
        _dayConfigured = false;
        GetTree().ReloadCurrentScene();
    }

    public void ResetWeek(int weekSeed)
    {
        State = new CareerState { WeekSeed = weekSeed };
        Save();
    }

    void Load()
    {
        try
        {
            if (FileAccess.FileExists(CareerPath))
            {
                using var f = FileAccess.Open(CareerPath, FileAccess.ModeFlags.Read);
                State = CareerState.FromJson(f.GetAsText());
                return;
            }
        }
        catch (Exception e) { GD.PrintErr("[CareerHook] load failed, starting new week: " + e.Message); }
        State = new CareerState();
        Save();
    }

    void Save()
    {
        try
        {
            DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(CareerDir));
            using var f = FileAccess.Open(CareerPath, FileAccess.ModeFlags.Write);
            f.StoreString(State.ToJson());
        }
        catch (Exception e) { GD.PrintErr("[CareerHook] save failed: " + e.Message); }
    }
}
