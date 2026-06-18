#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class InventoryPanel:DashCard{
 SimRunState? s; Label status=new();
 public InventoryPanel(){CardTitle="Inventory";}
 public override void _Ready(){base._Ready();status=StatusLabel();}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;status.Text=$"Raw: {s.Raw} | Prep: {s.Prep}\nQuality: {s.PrepQuality}%\nWaste: {s.DisplayWasteUnits:0.#} units | ${s.DisplayWasteCost:0.00}\nFood cost: {s.FoodCostPercent:0.0}%";}
}
