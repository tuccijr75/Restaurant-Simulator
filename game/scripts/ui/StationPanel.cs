using Godot;
namespace RestaurantSimulator;
public partial class StationPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;Text=$"Station: kitchen load {s.KitchenLoad} | delay risk {s.DelayRisk}";}
}
