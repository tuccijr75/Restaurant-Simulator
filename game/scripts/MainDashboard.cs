using Godot;
namespace RestaurantSimulator;
public partial class MainDashboard:Control{
 public override void _Ready(){
  var st=new SimRunState();var b=new VBoxContainer();AddChild(b);
  b.AddChild(new Label{Text="Restaurant Simulator"});
  var sc=new ScenarioPanel();sc.Bind(st);var c=new ClockPanel();c.Bind(st);
  var btn=new Button{Text="Start/Pause"};btn.Pressed+=()=>st.Running=!st.Running;
  b.AddChild(sc);b.AddChild(btn);b.AddChild(c);
  b.AddChild(new PosPanel());b.AddChild(new KdsPanel());b.AddChild(new LaborPanel());
  b.AddChild(new InventoryPanel());b.AddChild(new SosPanel());b.AddChild(new AlertPanel());
 }
}
