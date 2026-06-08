using Godot;
namespace RestaurantSimulator;
public partial class MainDashboard:Control{
 public override void _Ready(){
  var box=new VBoxContainer();AddChild(box);box.SetAnchorsPreset(LayoutPreset.FullRect);
  box.AddChild(new Label{Text="Restaurant Simulator"});
  var clock=new ClockPanel();
  var start=new Button{Text="Start/Pause"};start.Pressed+=clock.Toggle;
  box.Add