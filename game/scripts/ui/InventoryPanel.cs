using Godot;
namespace RestaurantSimulator;
public partial class InventoryPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;Text=$"Inventory: raw {s.Raw} prep {s.Prep} waste {s.Waste}";}
}
