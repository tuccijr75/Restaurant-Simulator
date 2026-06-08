using Godot;
namespace RestaurantSimulator;
public partial class SosPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){Text=$"SOS: {s?.Sos??0}s estimate";}
}
