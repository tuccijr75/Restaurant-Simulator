using Godot;
namespace RestaurantSimulator;
public partial class KdsPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){Text=$"KDS: {s?.Tickets??0} active tickets";}
}
