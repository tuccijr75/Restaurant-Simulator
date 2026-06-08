using Godot;
namespace RestaurantSimulator;
public partial class StaffingLedgerPanel:VBoxContainer{
 SimRunState? s; Label l=new(); Button export=new(){Text="Export Staffing Ledger"};
 public override void _Ready(){export.Pressed+=Save;AddChild(export);AddChild(l);}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){l.Text="Staffing Ledger:\n"+(s?.StaffingLedger??"");}
 void Save(){if(s==null){export.Text="No run state";return;}var f=FileAccess.Open("user://staffing_ledger.txt",FileAccess.ModeFlags.Write);if(f==null){export.Text="Export failed";return;}f.StoreString(s.StaffingLedger);f.Close();export.Text=$"Exported {s.StaffingSeq} staffing rows";}
}