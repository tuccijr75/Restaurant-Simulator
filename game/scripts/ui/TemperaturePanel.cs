using Godot;

namespace RestaurantSimulator;

public partial class TemperaturePanel:DashCard{
 SimRunState? s; Label status=new();
 public TemperaturePanel(){CardTitle="Temperatures";}
 public override void _Ready(){
  base._Ready();
  AddButton("Check Temps",()=>s?.CheckTemps(),true);
  status=StatusLabel();
 }
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;status.Text=$"Cooler: {s.CoolerTemp:0.0}F\nHot hold: {s.HotHoldTemp:0.0}F\nCheck age: {s.TempCheckAge:0}m\nDue: {s.TempCheckDue}";}
}
