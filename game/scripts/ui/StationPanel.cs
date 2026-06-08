using Godot;
namespace RestaurantSimulator;
public partial class StationPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;Text=$"Stations: Fry {s.FryerLoad} Grill {s.GrillLoad} Assembly {s.AssemblyLoad} Expo {s.ExpoLoad} Risk {s.DelayRisk}";}
}
