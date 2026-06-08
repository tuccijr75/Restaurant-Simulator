using Godot;
namespace RestaurantSimulator;
public partial class ClockPanel:Label{
 SimRunState? s;
 public void Bind(SimRunState state){s=state;Text="Clock "+s.TimeText;}
 public override void _Process(double d){if(s==null)return;s.Step(d);Text="Clock "+s.TimeText;}
}
