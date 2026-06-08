using Godot;
namespace RestaurantSimulator;
public partial class PosPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){Text=$"POS: {s?.Orders??0} orders";}
}
