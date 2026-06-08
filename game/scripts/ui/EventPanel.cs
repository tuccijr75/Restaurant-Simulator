using Godot;
namespace RestaurantSimulator;
public partial class EventPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){Text="Events:\n"+(s?.RecentEvents??"");}
}
