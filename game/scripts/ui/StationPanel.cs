#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class StationPanel:DashCard{
 SimRunState? s; Label status=new();
 public StationPanel(){CardTitle="Stations";CustomMinimumSize=new Vector2(330,250);}
 public override void _Ready(){base._Ready();status=StatusLabel();}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){
  if(s==null)return;
  status.Text=$"Bottleneck: {s.BottleneckStation} | {s.BottleneckBacklogMinutes:0.0}m | Risk {s.DelayRisk}\nEquipment: {s.BottleneckEquipment} | Units {s.EquipmentCount}\nTotal load {s.KitchenLoad} / cap {s.StaffCapacity} | net {s.NetKitchenLoad}\nFryer {s.FryerLoad}/{s.FryerCapacity:0} ({s.FryerBacklogMinutes:0.0}m) | Grill {s.GrillLoad}/{s.GrillCapacity:0} ({s.GrillBacklogMinutes:0.0}m)\nAssembly {s.AssemblyLoad}/{s.AssemblyCapacity:0} ({s.AssemblyBacklogMinutes:0.0}m)\nExpo+Beverage {s.ExpoLoad}/{s.ExpoCapacity+s.BeverageCapacity:0} ({s.ExpoBacklogMinutes:0.0}m)\nOverload events: {s.OverloadSeq}";
 }
}
