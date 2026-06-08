using Godot;
namespace RestaurantSimulator;
public partial class InventoryControlPanel:VBoxContainer{
 SimRunState? s; Label l=new();
 public override void _Ready(){var p=new Button{Text="Prep More"};var d=new Button{Text="Discard Prep"};p.Pressed+=()=>s?.ManualPrep();d.Pressed+=()=>s?.ManualDiscard();AddChild(p);AddChild(d);AddChild(l);}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double delta){l.Text="Waste Ledger:\n"+(s?.WasteLedger??"");}
}
