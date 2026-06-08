using Godot;

namespace RestaurantSimulator;

public partial class MainDashboard : Control
{
    private Label _clock = new();
    private bool _run;
    private double _min = 360;

    public override void _Ready()
    {
        foreach (Node c in GetChildren()) c.QueueFree();
        var root = new VBoxContainer();
        AddChild(root);
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 20; root.OffsetTop = 16;
        root.OffsetRight = -20; root.OffsetBottom = -16;
        root.AddChild(new Label { Text = "Restaurant Simulator" });
        var row = new HBoxContainer(); root.AddChild(row);
        var scn = new OptionButton(); row.AddChild(scn);
        scn.AddItem("Normal Day"); scn.AddItem("Rush Day"); scn.AddItem("Weather"); scn.AddItem("Call-Off");
        var btn = new Button { Text = "Start/Pause" }; row.AddChild(btn);
        btn.Pressed += () => _run = !_run;
        _clock.Text = "06:00"; row.AddChild(_clock