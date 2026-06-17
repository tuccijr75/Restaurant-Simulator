using Godot;
using System.Globalization;

namespace RestaurantSimulator;

/// 3D entry point. Owns the single SimRunState (deterministic fixed-timestep core),
/// builds the world, camera grid, character agents, CCTV HUD, and embeds the
/// existing 2D operations dashboard as a TAB overlay sharing the same sim.
public partial class Main : Node3D
{
    SimRunState _sim = null!;
    WorldLayout _world = null!;
    CameraDirector _cams = null!;
    AgentManager _agents = null!;
    Hud3D _hud = null!;
    GameplayUi _gameplay = null!;
    VitalsAndFx _vitals = null!;
    StaffUi _staffUi = null!;
    CanvasLayer _dashLayer = null!;
    bool _roofManualHide;

    public override void _Ready()
    {
        LoadConfig();   // RS-CF-001: config is a deterministic input
        _sim = new SimRunState { ExternallyDriven = true, TimeScale = 10.0 };

        _world = WorldBuilder.Build(this);

        _cams = new CameraDirector { Name = "Cameras" };
        AddChild(_cams);
        _cams.Build(this, _world);

        _agents = new AgentManager { Name = "Agents" };
        AddChild(_agents);
        _agents.Init(_sim, _world);

        _hud = new Hud3D { Name = "Hud" };
        AddChild(_hud);
        _hud.Init(_sim, _cams);

        _gameplay = new GameplayUi { Name = "Gameplay" };
        AddChild(_gameplay);
        _gameplay.Init(_sim);

        _vitals = new VitalsAndFx { Name = "Vitals" };
        AddChild(_vitals);
        _vitals.Init(_sim, _world);

        _staffUi = new StaffUi { Name = "StaffUi" };
        AddChild(_staffUi);
        _staffUi.Init(_agents);

        // Existing 2D dashboard, shared sim, hidden until TAB.
        // MUST be created BEFORE KillFocus(_dashLayer): _dashLayer was previously
        // assigned after the KillFocus calls, so KillFocus(null) threw in _Ready,
        // aborted construction, and left _dashLayer null for the whole session —
        // which then NRE'd on every _dashLayer.Visible access (TAB, clicks, F5 path).
        _dashLayer = new CanvasLayer { Visible = false, Layer = 5, Name = "Dashboard" };
        AddChild(_dashLayer);
        var dash = new MainDashboard { Shared = _sim };
        dash.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _dashLayer.AddChild(dash);

        // Once any Button is clicked it grabs keyboard focus, and Godot's GUI
        // focus navigation then consumes TAB (and SPACE re-presses the button)
        // before game input ever sees it. Two-layer kill: nothing in the panel
        // UI is focusable, and the focus-traversal actions are unbound. Runs after
        // the dashboard exists so its buttons are actually covered.
        KillFocus(_dashLayer);
        KillFocus(_gameplay);
        KillFocus(_staffUi);
        if (InputMap.HasAction("ui_focus_next")) InputMap.ActionEraseEvents("ui_focus_next");
        if (InputMap.HasAction("ui_focus_prev")) InputMap.ActionEraseEvents("ui_focus_prev");
    }

    public override void _Process(double delta)
    {
        _sim.Step(delta);
        // Roof auto-hides for the overhead camera and a high free cam, plus the R toggle.
        bool hide = _roofManualHide || _cams.IsOverhead || _cams.IsFreeHigh;
        if (_world.RoofGroup.Visible == hide) _world.RoofGroup.Visible = !hide;
        UpdateDayNight();
    }

    // RS-VS-001: the sun tracks the simulated clock — warm dawn, white noon, amber
    // dusk; after dark the lot poles and interior ceiling lights carry the scene.
    void UpdateDayNight()
    {
        float m = (float)_sim.Minute;                       // 360..1439
        float day = Mathf.Clamp((m - 360f) / 900f, 0f, 1f); // 06:00..21:00 arc
        float elev = -8f - 58f * Mathf.Sin(day * Mathf.Pi); // -8 dawn -> -66 noon -> -8 dusk
        _world.Sun.RotationDegrees = new Vector3(elev, -35f + 70f * day, 0);
        bool night = m >= 1230 || m < 375;                  // after ~20:30
        float dusk = Mathf.Clamp((m - 1140f) / 90f, 0f, 1f); // 19:00..20:30 fade
        _world.Sun.LightEnergy = night ? 0.05f : Mathf.Lerp(1.15f, 0.15f, dusk) * Mathf.Clamp(0.35f + 0.65f * Mathf.Sin(day * Mathf.Pi), 0f, 1f);
        _world.Sun.LightColor = new Color(1f, Mathf.Lerp(0.78f, 0.97f, Mathf.Sin(day * Mathf.Pi)), Mathf.Lerp(0.55f, 0.92f, Mathf.Sin(day * Mathf.Pi)));
        var topDay = new Color(0.35f, 0.55f, 0.85f); var topNight = new Color(0.04f, 0.05f, 0.10f);
        var horDay = new Color(0.75f, 0.82f, 0.9f); var horNight = new Color(0.10f, 0.10f, 0.16f);
        float darkness = night ? 1f : dusk * 0.85f;
        _world.SkyMat.SkyTopColor = topDay.Lerp(topNight, darkness);
        _world.SkyMat.SkyHorizonColor = horDay.Lerp(horNight, darkness);
        foreach (var l in _world.LotLights) l.LightEnergy = Mathf.Lerp(0.0f, 1.6f, darkness);
        foreach (var l in _world.InteriorLights) l.LightEnergy = Mathf.Lerp(0.25f, 1.3f, darkness);
    }

    // RS-GP-001: click a station to open its control panel. Returns true if a
    // station was hit, so the caller can fall through to employee picking otherwise.
    bool TryPickStation(Vector2 screenPos)
    {
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return false;
        var from = cam.ProjectRayOrigin(screenPos);
        var to = from + cam.ProjectRayNormal(screenPos) * 80f;
        var q = PhysicsRayQueryParameters3D.Create(from, to);
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(q);
        if (hit.Count == 0) return false;
        var collider = hit["collider"].As<Node>();
        if (collider != null && collider.HasMeta("station"))
        {
            _gameplay.OpenStation(collider.GetMeta("station").AsString());
            return true;
        }
        return false;
    }

    static void KillFocus(Node n)
    {
        if (n == null) return;   // defensive: never dereference a node that isn't built yet
        if (n is Control c) c.FocusMode = Control.FocusModeEnum.None;
        foreach (Node ch in n.GetChildren()) KillFocus(ch);
    }

    void LoadConfig()
    {
        var baseline = FileAccess.Open("res://config/realism_baseline.json", FileAccess.ModeFlags.Read);
        if (baseline != null) { SimConfig.LoadBaseline(baseline.GetAsText()); baseline.Close(); }
        var profiles = FileAccess.Open("res://config/human_behavior_profiles.json", FileAccess.ModeFlags.Read);
        if (profiles != null) { SimConfig.LoadProfiles(profiles.GetAsText()); profiles.Close(); }
    }

    // TAB is also Godot's focus-traversal key: once any dashboard button holds
    // focus, _UnhandledInput never sees TAB again. Intercept it BEFORE the GUI,
    // and drop keyboard focus whenever the dashboard toggles so a previously
    // clicked button can't keep eating SPACE/arrow keys afterward.
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey k || !k.Pressed || k.Echo) return;
        if (k.Keycode == Key.Tab)
        {
            _dashLayer.Visible = !_dashLayer.Visible;
            GetViewport().GuiReleaseFocus();
            GetViewport().SetInputAsHandled();
        }
        else if (k.Keycode == Key.Escape && _dashLayer.Visible)
        {
            _dashLayer.Visible = false;
            GetViewport().GuiReleaseFocus();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mm)
        {
            _cams.FreeLook(mm.Relative);
            return;
        }
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left
            && Input.MouseMode != Input.MouseModeEnum.Captured && !_dashLayer.Visible)
        {
            if (!TryPickStation(mb.Position)) _staffUi.TryPickEmployee(mb.Position);   // station first, then a crew member
            return;
        }
        if (@event is not InputEventKey k || !k.Pressed || k.Echo) return;

        switch (k.Keycode)
        {
            case Key.Space: _sim.Running = !_sim.Running; break;
            case Key.C: _cams.NextCam(); break;
            case Key.T: _cams.ToggleTour(); break;
            case Key.F: _cams.ToggleFree(); break;
            case Key.O: _cams.Overhead(); break;
            case Key.Key1: _cams.Select(0); break;
            case Key.Key2: _cams.Select(1); break;
            case Key.Key3: _cams.Select(2); break;
            case Key.Key4: _cams.Select(3); break;
            case Key.Key5: _cams.Select(4); break;
            case Key.Key6: _cams.Select(5); break;
            case Key.Key7: _cams.Select(6); break;
            case Key.Key8: _cams.Select(7); break;
            case Key.Key9: _cams.Select(8); break;
            case Key.Key0: _cams.Select(9); break;
            case Key.R: _roofManualHide = !_roofManualHide; break;
            case Key.M: _sim.ToggleManagerMode(); break;
            case Key.P: _staffUi.ToggleSchedule(); break;   // #10 staff schedule
            case Key.F9:
                if (_hud.ReportVisible) _hud.HideReport();
                else _hud.ShowReport(SelfTest.Run(_sim.Scenario, _sim.Seed));
                break;
            case Key.F5:
                ExportNow();
                break;
            case Key.Escape:
                if (Input.MouseMode == Input.MouseModeEnum.Captured) _cams.ToggleFree();
                break;
        }
    }

    void ExportNow()
    {
        var dir = $"user://outputs/sim_{_sim.Scenario}_{_sim.Seed}";
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(dir));
        var stamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
        var files = Exports.BuildAll(_sim, stamp);
        foreach (var (name, content) in files)
        {
            var f = FileAccess.Open($"{dir}/{name}", FileAccess.ModeFlags.Write);
            if (f == null) { _hud.ShowReport($"Export failed: {dir}/{name}"); return; }
            f.StoreString(content);
            f.Close();
        }
        _hud.ShowReport($"Exported {files.Count}-file output contract to {ProjectSettings.GlobalizePath(dir)}");
    }
}
