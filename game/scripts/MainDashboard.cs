using Godot;
namespace RestaurantSimulator;
public partial class MainDashboard:Control{
 public override void _Ready(){
  var b=new VBoxContainer();AddChild(b);
  b.AddChild(new Label{Text="Restaurant Simulator"});
  var c=new ClockPanel();var s=new Button{Text="Start/Pause"};s.Pressed+=c.Toggle;
  b.AddChild(new ScenarioPanel());b.AddChild(s);b.AddChild(c);
  b.AddChild(new PosPanel());b.AddChild(new KdsPanel());b.AddChild(new LaborPanel());
  b.AddChild(new InventoryPanel());b.AddChild(new SosPanel());b.AddChild(new AlertPanel());
 }
}
