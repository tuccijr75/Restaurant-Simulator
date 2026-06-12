using Godot;
using System.Globalization;

namespace RestaurantSimulator;

/// CCTV-style overlay for the 3D view. Anchor-based layout so it adapts to any
/// window size: top ops strip (left) + camera tag (right) on a backdrop, bottom
/// alert/help strip, centered start prompt, and the self-test report panel.
public partial class Hud3D : CanvasLayer
{
    SimRunState _sim = null!;
    CameraDirector _cams = null!;
    Label _ops = null!, _camTag = null!, _alert = null!, _help = null!, _prompt = null!, _report = null!;
    PanelContainer _reportPanel = null!;
    float _blink;

    public void Init(SimRunState sim, CameraDirector cams)
    {
        _sim = sim;
        _cams = cams;

        // ---- top strip ----
        var top = Backdrop(Control.LayoutPreset.TopWide);
        top.OffsetBottom = 84;

        _ops = MakeLabel(14, new Color(0.92f, 0.95f, 0.92f));
        _ops.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _ops.OffsetLeft = 14; _ops.OffsetTop = 8;
        top.AddChild(_ops);

        _camTag = MakeLabel(16, new Color(1f, 0.93f, 0.88f));
        _camTag.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _camTag.OffsetLeft = -620; _camTag.OffsetRight = -14; _camTag.OffsetTop = 10;
        _camTag.HorizontalAlignment = HorizontalAlignment.Right;
        top.AddChild(_camTag);

        // ---- bottom strip ----
        var bottom = Backdrop(Control.LayoutPreset.BottomWide);
        bottom.OffsetTop = -62;

        _alert = MakeLabel(15, new Color(1f, 0.55f, 0.4f));
        _alert.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        _alert.OffsetLeft = 14; _alert.OffsetTop = 4;
        bottom.AddChild(_alert);

        _help = MakeLabel(12, new Color(0.78f, 0.8f, 0.83f));
        _help.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        _help.OffsetLeft = 14; _help.OffsetTop = -24;
        _help.Text = "SPACE start/pause · M manager mode · click a station for controls · 1-9,0 cams · O overhead · C next · T tour · F free cam · R roof · TAB dashboard · F9 self-test · F5 export";
        bottom.AddChild(_help);

        // ---- center start prompt ----
        _prompt = MakeLabel(26, new Color(1f, 0.92f, 0.75f));
        _prompt.SetAnchorsPreset(Control.LayoutPreset.Center);
        _prompt.OffsetLeft = -260; _prompt.OffsetRight = 260; _prompt.OffsetTop = -120;
        _prompt.HorizontalAlignment = HorizontalAlignment.Center;
        _prompt.Text = "PRESS  SPACE  TO  START  THE  SHIFT";
        AddChild(_prompt);

        // ---- self-test / export report ----
        _reportPanel = new PanelContainer { Visible = false };
        _reportPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
        _reportPanel.OffsetLeft = -340; _reportPanel.OffsetRight = 340;
        _reportPanel.OffsetTop = -160;
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.07f, 0.09f, 0.92f),
            BorderColor = new Color(0.75f, 0.16f, 0.12f),
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 12, ContentMarginBottom = 12
        };
        style.SetBorderWidthAll(2);
        _reportPanel.AddThemeStyleboxOverride("panel", style);
        AddChild(_reportPanel);
        _report = new Label();
        _report.AddThemeFontSizeOverride("font_size", 13);
        _reportPanel.AddChild(_report);
    }

    ColorRect Backdrop(Control.LayoutPreset preset)
    {
        var bar = new ColorRect { Color = new Color(0, 0, 0, 0.55f), MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(bar);
        bar.SetAnchorsPreset(preset);
        return bar;
    }

    Label MakeLabel(int size, Color color)
    {
        var l = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", color);
        l.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
        l.AddThemeConstantOverride("outline_size", 5);
        return l;
    }

    public bool ReportVisible => _reportPanel.Visible;
    public void ShowReport(string text) { _report.Text = text + "\n\n(F9 closes)"; _reportPanel.Visible = true; }
    public void HideReport() => _reportPanel.Visible = false;

    public override void _Process(double delta)
    {
        _blink += (float)delta;
        var inv = CultureInfo.InvariantCulture;
        _ops.Text =
            $"{_sim.TimeText}  {_sim.Daypart.ToUpper()}  ·  {_sim.Scenario}  ·  seed {_sim.Seed}  ·  {(_sim.Running ? _sim.TimeScale.ToString("0", inv) + "x" : "PAUSED")}  ·  {(_sim.AutoSchedule ? "AUTO STAFF" : "MANUAL STAFF")}\n" +
            string.Format(inv, "Orders {0}  (DT {1} / Lobby {2} / Mob {3} / Del {4})   Done {5}   Board {6}   Crew {7} (sched {8})",
                _sim.Orders, _sim.DriveThru, _sim.FrontCounter, _sim.Mobile, _sim.Delivery,
                _sim.CompletedTickets, _sim.Tickets, _sim.EffectiveCrew, SimRunState.ScheduledCrewAt((int)_sim.Minute)) + "\n" +
            string.Format(inv, "Sales ${0:0}   Avg ${1:0.00}   Labor {2:0.0}%   Waste {3}u   SOS  DT {4}s / FC {5}s / DEL {6}s",
                _sim.Sales, _sim.Orders == 0 ? 0 : _sim.Sales / _sim.Orders, _sim.LaborPercent, _sim.Waste, _sim.DtSos, _sim.FcSos, _sim.DelSos);

        bool rec = (_blink % 1.2f) < 0.7f;
        _camTag.Text = $"{_cams.CurrentName}   {(rec ? "● REC" : "   REC")}   {SimEvent.BusinessDay} {_sim.TimeText}{(_cams.TourOn ? "   [TOUR]" : "")}";

        var alert = _sim.AlertText;
        _alert.Text = alert == "Alerts: none" ? "" : alert;
        _alert.AddThemeColorOverride("font_color",
            alert.StartsWith("ALERT") ? new Color(1f, 0.3f, 0.25f) : new Color(1f, 0.75f, 0.35f));

        _prompt.Visible = !_sim.Running && !_sim.ShiftEnded;
        if (_prompt.Visible) _prompt.Modulate = new Color(1, 1, 1, 0.65f + 0.35f * Mathf.Sin(_blink * 3f));
    }
}
