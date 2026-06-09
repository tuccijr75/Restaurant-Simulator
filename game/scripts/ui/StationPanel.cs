#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class StationPanel:DashCard{
 SimRunState? s; Label status=new();
 public StationPanel(){CardTitle="Stations";}
 public override void _Ready(){base._Ready();status=StatusLabel();}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){
  if(s==null)return;
  status.Text=$"Backlog {s.KitchenBacklogMinutes:0.0}m | Cap/min {s.StaffCapacity}\nLoad {s.KitchenLoad} | Net/min {s.NetKitchenLoad}\nFryer {s.FryerLoad} ({s.FryerBacklogMinutes:0.0}m) | Grill {s.GrillLoad} ({s.GrillBacklogMinutes:0.0}m)\nAssembly {s.AssemblyLoad} ({s.AssemblyBacklogMinutes:0.0}m) | Expo {s.ExpoLoad} ({s.ExpoBacklogMinutes:0.0}m)";
 }
}
