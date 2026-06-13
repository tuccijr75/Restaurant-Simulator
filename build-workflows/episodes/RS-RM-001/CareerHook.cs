using Godot;
using System;
using System.Reflection;
using RestaurantSimulator;

// RS-RM-001 — Godot career hook (autoload singleton), self-sufficient version.
//
// Requires ZERO edits to Main.cs. Register this script as an autoload named
// "CareerHook" and it does the rest:
//   * locates the running SimRunState in the current scene by reflection,
//   * configures each day's scenario/seed/reputation-multiplier BEFORE the sim
//     starts (guarded on EventSeq==0 so it never mutates a run in progress),
//   * handles F6 in _Input — which, as an autoload, runs ahead of Main's _Input
//     focus-interceptor in tree order, so F6 is no longer swallowed.
//
// On F6 it folds the finished day's store-level outcomes into reputation, saves
// to user://career/career_state.json, and reloads the scene for the next day.
// Reputation is a store-level synthetic signal only — no per-employee data.
public partial class CareerHook : Node
{
    const string CareerDir = "user://career";
    const string CareerPath = "user://career/career_state.json";

    public CareerState State { get; private set; } = new();

    Node _sceneSeen;
    SimRunState _sim;
    bool _configured;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // keep working while the sim is paused
        Load();
        GD.Print($"[CareerHook] ready — week_seed={State.WeekSeed} day={State.DayIndex}/{CareerState.DaysPerWeek} reputation={State.Reputation:0.0} (press F6 to advance the day, F7 to reset the week)");
    }

    public override void _Process(double delta)
    {
        var scene = GetTree()?.CurrentScene;
        if (scene == null) return;

        // New scene (initial load or post-reload): re-find and re-configure.
        if (scene != _sceneSeen)
        {
            _sceneSeen = scene;
            _sim = null;
            _configured = false;
        }

        if (_sim == null) _sim = FindSim(scene);

        // Configure the day's scenario/seed/multiplier exactly once, before the
        // operator starts the shift (no events emitted yet => safe to set seed).
        if (_sim != null && !_configured && _sim.EventSeq == 0 && !State.WeekComplete)
        {
            ConfigureSim(_sim);
            _configured = true;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;

        if (key.Keycode == Key.F6)
        {
            if (_sim == null) _sim = FindSim(GetTree()?.CurrentScene);
            if (_sim == null) { GD.Print("[CareerHook] F6: no SimRunState found in the scene"); return; }
            AdvanceDay(_sim);
            GetViewport().SetInputAsHandled();
        }
        else if (key.Keycode == Key.F7)
        {
            ResetWeek(State.WeekSeed);
            GD.Print($"[CareerHook] F7: week reset (week_seed={State.WeekSeed}); reload or restart to begin day 0");
            GetViewport().SetInputAsHandled();
        }
    }

    public bool ConfigureSim(SimRunState sim)
    {
        if (State.WeekComplete)
        {
            GD.Print($"[CareerHook] week complete — final reputation {State.Reputation:0.0}. Press F7 to start a new week.");
            return false;
        }
        sim.Scenario = CareerState.ScenarioFor(State.WeekSeed, State.DayIndex);
        sim.Seed = CareerState.DaySeed(State.WeekSeed, State.DayIndex);
        sim.ReputationDemandMultiplier = State.DemandMultiplier;
        GD.Print($"[CareerHook] configured day {State.DayIndex}: scenario={sim.Scenario} seed={sim.Seed} demand_mult={sim.ReputationDemandMultiplier:0.000} (reputation {State.Reputation:0.0})");
        return true;
    }

    public void AdvanceDay(SimRunState sim)
    {
        if (!_configured || sim.EventSeq == 0)
        {
            GD.Print("[CareerHook] F6: no day has run yet — start the shift (SPACE) before advancing");
            return;
        }
        if (!sim.ShiftEnded)
            GD.Print($"[CareerHook] note: advancing before close — folding partial-day totals (orders={sim.Orders})");

        var rec = State.ApplyDayResult(
            sim.Scenario, sim.Seed, sim.ReputationDemandMultiplier,
            sim.Csat, sim.InspectionScore, sim.Orders, sim.AbandonedTickets, sim.Sales,
            Exports.Sha256Hex(sim.AllJsonl));
        GD.Print($"[CareerHook] day {rec.Day} ({rec.Scenario}) folded: csat={rec.Csat:0.0} insp={rec.InspectionScore:0} orders={rec.Orders} aband={rec.AbandonedTickets} => reputation {rec.ReputationBefore:0.0} -> {rec.ReputationAfter:0.0} ({(rec.ReputationDelta >= 0 ? "+" : "")}{rec.ReputationDelta:0.00})");
        Save();

        if (State.WeekComplete)
            GD.Print($"[CareerHook] week complete — final reputation {State.Reputation:0.0}. Press F7 to start a new week.");

        _configured = false;
        _sim = null;
        GetTree().ReloadCurrentScene();
    }

    public void ResetWeek(int weekSeed)
    {
        State = new CareerState { WeekSeed = weekSeed };
        _configured = false;
        _sim = null;
        Save();
    }

    // ---- locate the SimRunState living inside the scene (any node, any field/prop) ----
    static SimRunState FindSim(Node root)
    {
        if (root == null) return null;
        var found = ScanNode(root);
        if (found != null) return found;
        foreach (var child in root.GetChildren())
        {
            var s = FindSim(child);
            if (s != null) return s;
        }
        return null;
    }

    static SimRunState ScanNode(Node node)
    {
        var t = node.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        for (var cur = t; cur != null && cur != typeof(Node); cur = cur.BaseType)
        {
            foreach (var f in cur.GetFields(flags))
                if (typeof(SimRunState).IsAssignableFrom(f.FieldType))
                {
                    try { if (f.GetValue(node) is SimRunState s) return s; } catch { }
                }
            foreach (var p in cur.GetProperties(flags))
                if (p.CanRead && p.GetIndexParameters().Length == 0 && typeof(SimRunState).IsAssignableFrom(p.PropertyType))
                {
                    try { if (p.GetValue(node) is SimRunState s) return s; } catch { }
                }
        }
        return null;
    }

    // ---- persistence ----
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
