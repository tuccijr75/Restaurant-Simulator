#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class InventoryControlPanel:DashCard{
 SimRunState? s; Label status=new();
 public InventoryControlPanel(){CardTitle="Prep / Waste";CustomMinimumSize=new Vector2(330,220);}
 public override void _Ready(){
  base._Ready();
  var r=Row();AddRowButton(r,"Prep +",()=>s?.ManualPrep(),true);AddRowButton(r,"Discard -",()=>s?.ManualDiscard());
  status=StatusLabel();
 }
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double delta){status.Text="Waste Ledger:\n"+DashTheme.Preview(s?.WasteLedger??"",5,300);}
}
