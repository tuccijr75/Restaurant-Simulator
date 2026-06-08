using Godot;
namespace RestaurantSimulator;
public partial class MainDashboard:Control{
 public override void _Ready(){
  var st=new SimRunState();var b=new VBoxContainer();AddChild(b);
  var sc=new ScenarioPanel();sc.Bind(st);var c=new ClockPanel();c.Bind(st);
  var p=new PosPanel();p.Bind(st);var k=new KdsPanel();k.Bind(st);
  var sp=new StationPanel();sp.Bind(st);var so=new SosPanel();so.Bind(st);var inv=new InventoryPanel();inv.Bind(st);
  var a=new AlertPanel();a.Bind(st);var e=new EventPanel();e.Bind(st);var j=new JsonlPanel();j.Bind(st);var x=new ExportPanel();x.Bind(st);
  var btn=new Button{Text="Start/Pause"};btn.Pressed+=()=>st.Running=!st.Running;
  b.AddChild(new Label{Text="Restaurant Simulator"});b.AddChild(sc);b.AddChild(btn);b.AddChild(c);
  b.AddChild(p);b.AddChild(k);b.AddChild(sp);b.AddChild(new LaborPanel());
  b.AddChild(inv);b.AddChild(so);b.AddChild(a);b.AddChild(e);b.AddChild(j);b.AddChild(x);
 }
}
