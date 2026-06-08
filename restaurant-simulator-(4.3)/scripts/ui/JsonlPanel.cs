using Godot;
namespace RestaurantSimulator;
public partial class JsonlPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){Text="JSONL Export Preview:\n"+(s?.RecentJsonl??"");}
}
