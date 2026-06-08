using Godot;
namespace RestaurantSimulator;
public partial class AlertPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){Text=s?.AlertText??"Alerts: none";}
}
