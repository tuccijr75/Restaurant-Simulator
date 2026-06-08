using Godot;
namespace RestaurantSimulator;
public partial class TemperaturePanel:VBoxContainer{
 SimRunState? s; Label l=new();
 public override void _Ready(){var b=new Button{Text="Check Temps"};b.Pressed+=()=>s?.CheckTemps();AddChild(b);AddChild(l);}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;l.Text=$"Temperatures: cooler {s.CoolerTemp:0.0}F hot hold {s.HotHoldTemp:0.0}F check age {s.TempCheckAge:0}m due {s.TempCheckDue}";}
}