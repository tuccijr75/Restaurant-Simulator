using Godot;
using System.Globalization;

namespace RestaurantSimulator;

/// RS-GP-001 player layer: decision toasts (A/B with countdown), live objectives
/// strip, and the click-a-station popup with direct controls.
public partial class GameplayUi : CanvasLayer
{
    SimRunState _sim = null!;
    PanelContainer _toast = null!, _stationPanel = null!;
    Label _toastTitle = null!, _toastDeadline = null!, _objectives = null!, _stationInfo = null!, _stationTitle = null!;
    Button _optA = null!, _optB = null!;
    int _toastId = -1;
    string _station = "";

    public void Init(SimRunState sim)
    {
        _sim = sim;
        Layer = 3;

        // ---- objectives strip (right edge) ----
        var objBack = new ColorRect { Color = new Color(0, 0, 0, 0.5f), MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(objBack);
        objBack.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        objBack.OffsetLeft = -252; objBack.OffsetTop = 96; objBack.OffsetBottom = -72;
        _objectives = new Label { MouseFilter = Control.MouseFilterEnum.Ignore };
        _objectives.AddThemeFontSizeOverride("font_size", 13);
        _objectives.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0, 0.9f));
        _objectives.AddThemeConstantOverride("outline_size", 4);
        objBack.AddChild(_objectives);
        _objectives.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _objectives.OffsetLeft = 10; _objectives.OffsetTop = 8;

        // ---- decision toast (top center) ----
        _toast = Panel(new Color(0.10f, 0.07f, 0.05f, 0.95f), new Color(1f, 0.6f, 0.15f));
        _toast.SetAnchorsPreset(Control.LayoutPreset.CenterTop);
        _toast.OffsetLeft = -300; _toast.OffsetRight = 300; _toast.OffsetTop = 96;
        var tv = new VBoxContainer(); _toast.AddChild(tv);
        _toastTitle = StyledLabel(tv, 16, new Color(1f, 0.9f, 0.75f));
        _toastDeadline = StyledLabel(tv, 12, new Color(0.9f, 0.7f, 0.5f));
        var hb = new HBoxContainer(); tv.AddChild(hb);
        _optA = new Button { FocusMode = Control.FocusModeEnum.None };
        _optB = new Button { FocusMode = Control.FocusModeEnum.None };
        hb.AddChild(_optA); hb.AddChild(_optB);
        _optA.Pressed += () => Answer(true);
        _optB.Pressed += () => Answer(false);
        _toast.Visible = false;

        // ---- station popup (left) ----
        _stationPanel = Panel(new Color(0.05f, 0.07f, 0.10f, 0.95f), new Color(0.45f, 0.65f, 0.9f));
        _stationPanel.SetAnchorsPreset(Control.LayoutPreset.CenterLeft);
        _stationPanel.OffsetLeft = 16; _stationPanel.OffsetRight = 320; _stationPanel.OffsetTop = -150;
        var sv = new VBoxContainer(); _stationPanel.AddChild(sv);
        _stationTitle = StyledLabel(sv, 16, new Color(0.8f, 0.9f, 1f));
        _stationInfo = StyledLabel(sv, 13, new Color(0.95f, 0.95f, 0.95f));
        var row1 = new HBoxContainer(); sv.AddChild(row1);
        Btn(row1, "+ Crew", () => CovBtn(true));
        Btn(row1, "- Crew", () => CovBtn(false));
        var row2 = new HBoxContainer(); sv.AddChild(row2);
        Btn(row2, "Drop Batch", DropForStation);
        Btn(row2, "Maintain", () => _sim.MaintainWorstEquipment());
        var row3 = new HBoxContainer(); sv.AddChild(row3);
        Btn(row3, "Start Prep", () => _sim.ManualPrep());
        Btn(row3, "Temps", () => _sim.CheckTemps());
        Btn(row3, "Sanitizer", () => _sim.ChangeSanitizer());
        Btn(row3, "Close", () => _stationPanel.Visible = false);
        _stationPanel.Visible = false;
    }

    PanelContainer Panel(Color bg, Color border)
    {
        var p = new PanelContainer();
        var st = new StyleBoxFlat { BgColor = bg, BorderColor = border,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 10 };
        st.SetBorderWidthAll(2);
        p.AddThemeStyleboxOverride("panel", st);
        AddChild(p);
        return p;
    }

    Label StyledLabel(Node parent, int size, Color c)
    {
        var l = new Label();
        l.AddThemeFontSizeOverride("font_size", size);
        l.AddThemeColorOverride("font_color", c);
        parent.AddChild(l);
        return l;
    }

    void Btn(Node parent, string text, System.Action act)
    {
        var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
        b.Pressed += () => act();
        parent.AddChild(b);
    }

    void Answer(bool a) { if (_toastId >= 0) _sim.ResolveDecision(_toastId, a); _toast.Visible = false; _toastId = -1; }

    void CovBtn(bool add)
    {
        switch (_station)
        {
            case "grill": case "assembly": case "expo": if (add) _sim.AddKitchenCoverage(); else _sim.RemoveKitchenCoverage(); break;
            case "fryer": if (add) _sim.AddFryerCoverage(); else _sim.RemoveFryerCoverage(); break;
            case "dt_window": case "order_board": case "beverage": if (add) _sim.AddDriveCoverage(); else _sim.RemoveDriveCoverage(); break;
            case "pos_register_1": case "pos_register_2": case "mobile_shelf": if (add) _sim.AddCounterCoverage(); else _sim.RemoveCounterCoverage(); break;
            case "prep": case "cooler": if (add) _sim.AddPrepCoverage(); else _sim.RemovePrepCoverage(); break;
        }
    }

    void DropForStation()
    {
        if (_station == "grill") _sim.DropBatch("grilled_main");
        else if (_station == "fryer") { _sim.DropBatch("fried_main"); _sim.DropBatch("fries"); }
    }

    public void OpenStation(string id) { _station = id; _stationPanel.Visible = true; }

    public override void _Process(double delta)
    {
        var inv = CultureInfo.InvariantCulture;
        // Decision toast
        if (_sim.Decisions.Count > 0)
        {
            var d = _sim.Decisions[0];
            if (d.Id != _toastId)
            {
                _toastId = d.Id;
                _toastTitle.Text = d.Title;
                _optA.Text = d.OptionA + (d.DefaultA ? "  (default)" : "");
                _optB.Text = d.OptionB + (!d.DefaultA ? "  (default)" : "");
            }
            _toastDeadline.Text = string.Format(inv, "auto-resolves in {0:0.0} min", System.Math.Max(0, d.DeadlineMinute - _sim.Minute));
            _toast.Visible = true;
        }
        else _toast.Visible = false;

        // Objectives
        string Gauge(double v, double target, bool higherBetter) =>
            (higherBetter ? v >= target : v <= target) ? "✔" : "✘";
        double avg = _sim.Orders == 0 ? 0 : _sim.Sales / _sim.Orders;
        _objectives.Text = string.Format(inv,
            "SHIFT TARGETS  [{0}]\n\n" +
            "Sales pace {1,6:0}%  {2}\n" +
            "Labor      {3,6:0.0}%  {4}\n" +
            "CSAT       {5,6:0.0}   {6}\n" +
            "DT SOS     {7,5}s   {8}\n" +
            "Abandoned  {9,4} ({10:0.0}%)\n" +
            "Balked cars{11,4}\n\n" +
            "HOLD  fried {12:0}/{13:0}\n" +
            "      grill {14:0}/{15:0}\n" +
            "      fries {16:0}/{17:0}\n\n" +
            "Equip worst {18}\n" +
            "Inspection  {19}\n" +
            "Costs OT ${20:0} comp ${21:0}\n      maint ${22:0} runs ${23:0}",
            _sim.ManagerMode ? "MANAGER MODE" : "AUTO  (M to take over)",
            _sim.SalesPacePercent, Gauge(_sim.SalesPacePercent, 95, true),
            _sim.LaborPercent, Gauge(_sim.LaborPercent <= 0 ? 30 : _sim.LaborPercent, 30, false),
            _sim.Csat, Gauge(_sim.Csat, 75, true),
            _sim.DtSos, Gauge(_sim.DtSos, 480, false),
            _sim.AbandonedTickets, _sim.Orders == 0 ? 0 : 100.0 * _sim.AbandonedTickets / _sim.Orders,
            _sim.BalkedCars,
            _sim.HoldLevel("fried_main"), _sim.HoldCapacity("fried_main"),
            _sim.HoldLevel("grilled_main"), _sim.HoldCapacity("grilled_main"),
            _sim.HoldLevel("fries"), _sim.HoldCapacity("fries"),
            _sim.WorstEquipment,
            _sim.InspectionScore < 0 ? (_sim.InspectorIncoming ? "incoming!" : "pending") : _sim.InspectionScore.ToString("0", inv),
            _sim.OvertimePremium, _sim.CompCost, _sim.MaintSpend, _sim.SupplyRunCost);
    }
}
