#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class StaffingLedgerPanel:DashCard{
 SimRunState? s; Label status=null!; Button export=null!;
 public StaffingLedgerPanel(){CardTitle="Staffing Ledger";CustomMinimumSize=new Vector2(330,240);}
 public override void _Ready(){
  base._Ready();
  export=AddButton("Export Staffing Ledger",Save,true);
  status=StatusLabel();
 }
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){status.Text="Rows: "+(s?.StaffingSeq??0)+"\n"+DashTheme.Preview(s?.StaffingLedger??"",5,300);}
 void Save(){if(s==null){export.Text="No run state";return;}var f=FileAccess.Open("user://staffing_ledger.txt",FileAccess.ModeFlags.Write);if(f==null){export.Text="Export failed";return;}f.StoreString(s.StaffingLedger);f.Close();export.Text=$"Exported {s.StaffingSeq} rows";}
}
