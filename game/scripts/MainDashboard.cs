using Godot;
namespace RestaurantSimulator;
public partial class MainDashboard:Control{
 public override void _Ready(){
  var box=new VBoxContainer();AddChild(box);
  box.SetAnchorsPreset(LayoutPreset.FullRect);
  box.AddChild(new Label{Text="Restaurant Simulator"});
  box.AddChild(new Label{Text="Phase 1 shell: scenario, clock, POS, KDS, labor, inventory, SOS, alerts pending."});
 }
}
