using Godot;

namespace RestaurantSimulator;

public partial class MainDashboard : Control
{
    private Label _clock = new();
    private bool _running;
    private double _minutes = 360;

    public override void _Ready()
    {
        foreach (Node child in GetChildren()) child.QueueFree();
        SetAnchorsPreset(LayoutPreset.FullRect);

        var root = new VBoxContainer();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 20; root.OffsetTop = 16;
        root.OffsetRight = -20; root.OffsetBottom = -16;
        AddChild(root);

        root.AddChild(new Label { Text = "Restaurant Simulator - Manager Dashboard" });

        var bar = new HBoxContainer();
        root.AddChild(bar);

        var scenario = new OptionButton();
        foreach (var name in new[] { "Normal Day", "Rush Day", "Weather Disruption", "Staffing Call-Off", "Health Inspection"