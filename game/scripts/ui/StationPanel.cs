using Godot;

namespace RestaurantSimulator;

public partial class StationPanel:DashCard{
 SimRunState? s; Label status=new();
 public StationPanel(){CardTitle="Stations";}
 public override void _Ready(){base._Ready();status=StatusLabel();}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;status.Text=$"Load: {s.KitchenLoad}  Cap: {s.StaffCapacity}  Net: {s.NetKitchenLoad}\nFryer {s.FryerLoad} | Grill {s.GrillLoad}\nAssembly {s.AssemblyLoad} | Expo {s.ExpoLoad}\nDelay risk: {s.DelayRisk}";}
}
