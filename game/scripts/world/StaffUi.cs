using Godot;
using System.Linq;

namespace RestaurantSimulator;

/// RS-ST-002 staff overlay. Two pieces, both reading the roster as source of truth:
///   #12 click-inspect — click a crew member to see name / position / live task +
///                       stats, with a "Return to station" command.
///   #10 schedule panel — the day's roster (in / break / out / position / MOD),
///                        toggled with P.
/// Presentation only: it reads AgentManager + Roster and issues a visual command;
/// it never touches the deterministic sim.
public partial class StaffUi : CanvasLayer
{
    AgentManager _agents = null!;
    EmployeeAgent? _selected;
    float _refresh;
    bool _scheduleVisible;

    Panel _inspect = null!;
    Label _inspectLabel = null!;
    Button _returnBtn = null!;
    Panel _schedule = null!;
    Label _scheduleLabel = null!;

    public void Init(AgentManager agents) => _agents = agents;

    public override void _Ready()
    {
        Layer = 6;

        // ---- #12 inspect card (bottom-left) ----
        _inspect = new Panel { Visible = false };
        _inspect.AnchorTop = 1; _inspect.AnchorBottom = 1;
        _inspect.OffsetLeft = 16; _inspect.OffsetRight = 372;
        _inspect.OffsetTop = -256; _inspect.OffsetBottom = -16;
        AddChild(_inspect);

        var vb = new VBoxContainer();
        vb.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        vb.OffsetLeft = 14; vb.OffsetTop = 14; vb.OffsetRight = -14; vb.OffsetBottom = -14;
        vb.AddThemeConstantOverride("separation", 10);
        _inspect.AddChild(vb);

        _inspectLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        _inspectLabel.AddThemeFontSizeOverride("font_size", 14);
        _inspectLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vb.AddChild(_inspectLabel);

        _returnBtn = new Button { Text = "Return to station" };
        _returnBtn.FocusMode = Control.FocusModeEnum.None;
        _returnBtn.Pressed += OnReturnPressed;
        vb.AddChild(_returnBtn);

        // ---- #10 schedule panel (top-right) ----
        _schedule = new Panel { Visible = false };
        _schedule.AnchorLeft = 1; _schedule.AnchorRight = 1;
        _schedule.OffsetLeft = -560; _schedule.OffsetRight = -16;
        _schedule.OffsetTop = 16; _schedule.OffsetBottom = 660;
        AddChild(_schedule);

        var sc = new ScrollContainer();
        sc.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        sc.OffsetLeft = 14; sc.OffsetTop = 14; sc.OffsetRight = -14; sc.OffsetBottom = -14;
        _schedule.AddChild(sc);

        _scheduleLabel = new Label();
        _scheduleLabel.AddThemeFontSizeOverride("font_size", 13);
        _scheduleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        sc.AddChild(_scheduleLabel);
    }

    void OnReturnPressed()
    {
        if (_selected != null && IsInstanceValid(_selected)) _selected.ReturnToStation();
    }

    // Returns true if a staff member was clicked (so the caller stops there).
    public bool TryPickEmployee(Vector2 screenPos)
    {
        if (_agents == null) return false;
        var cam = GetViewport().GetCamera3D();
        if (cam == null) return false;

        EmployeeAgent? best = null;
        float bestScore = 1f;
        foreach (var e in _agents.Staff)
        {
            if (e == null || !IsInstanceValid(e)) continue;
            var foot = e.GlobalPosition + Vector3.Up * 0.15f;
            var head = e.GlobalPosition + Vector3.Up * 1.8f;
            if (cam.IsPositionBehind(foot) || cam.IsPositionBehind(head)) continue;

            var screenFoot = cam.UnprojectPosition(foot);
            var screenHead = cam.UnprojectPosition(head);
            float screenHeight = screenFoot.DistanceTo(screenHead);
            float pickRadius = Mathf.Clamp(screenHeight * 0.45f, 38f, 98f);
            float nearest = DistanceToProjectedBody(cam, e.GlobalPosition, screenPos);
            float score = nearest / pickRadius;
            if (score < bestScore) { bestScore = score; best = e; }
        }

        _selected = best;
        _inspect.Visible = best != null;
        if (best != null) UpdateInspect();
        return best != null;
    }

    static float DistanceToProjectedBody(Camera3D cam, Vector3 origin, Vector2 screenPos)
    {
        var right = cam.GlobalTransform.Basis.X.Normalized() * 0.24f;
        var samples = new[]
        {
            origin + Vector3.Up * 0.35f,
            origin + Vector3.Up * 0.95f,
            origin + Vector3.Up * 1.45f,
            origin + Vector3.Up * 0.95f + right,
            origin + Vector3.Up * 0.95f - right,
        };
        float best = float.MaxValue;
        foreach (var p in samples)
        {
            if (cam.IsPositionBehind(p)) continue;
            best = Mathf.Min(best, cam.UnprojectPosition(p).DistanceTo(screenPos));
        }
        return best;
    }

    public void ToggleSchedule()
    {
        _scheduleVisible = !_scheduleVisible;
        _schedule.Visible = _scheduleVisible;
        if (_scheduleVisible) RenderSchedule();
    }

    public override void _Process(double delta)
    {
        if (_agents == null) return;
        _refresh -= (float)delta;
        if (_refresh > 0) return;
        _refresh = 0.4f;

        if (_selected != null && !IsInstanceValid(_selected)) { _selected = null; _inspect.Visible = false; }
        if (_inspect.Visible) UpdateInspect();
        if (_scheduleVisible) RenderSchedule();
    }

    void UpdateInspect()
    {
        if (_selected == null || !IsInstanceValid(_selected)) { _inspect.Visible = false; return; }
        var emp = _agents.StaffRoster.Employees.FirstOrDefault(e => e.Id == _selected.EmpId);
        string stats = emp != null
            ? $"\n\nAttitude {emp.Attitude:0}   Motivation {emp.Motivation:0}"
            + $"\nStress {emp.Stress:0}   Fatigue {emp.Fatigue:0}"
            + $"\n\nReliability {emp.Reliability}  Speed {emp.Speed}  Accuracy {emp.Accuracy}"
            + $"\nStamina {emp.Stamina}  Composure {emp.Composure}  Teamwork {emp.Teamwork}"
            : "";
        _inspectLabel.Text = $"{_selected.EmpName}\n{RoleLabel(_selected.Role)}  —  {_selected.Assigned}"
                           + $"\nTask: {_selected.Task}{stats}";
    }

    void RenderSchedule()
    {
        int day = _agents.RosterDay, min = _agents.SimMinute;
        var mod = _agents.StaffRoster.ModAt(day, min);
        string head = $"DAY {day} — STAFF SCHEDULE\nNow {min / 60:00}:{min % 60:00}    MOD: {(mod.HasValue ? mod.Value.Name : "—")}\n"
                    + "in–out   name             role               position   break\n"
                    + "------------------------------------------------------------\n";
        _scheduleLabel.Text = head + _agents.StaffRoster.DayReport(day);
    }

    static string RoleLabel(string role) => role switch
    {
        "shift_manager" => "Shift Manager",
        "assistant_manager" => "Assistant Manager",
        "restaurant_manager" => "General Manager",
        _ => "Crew",
    };
}
