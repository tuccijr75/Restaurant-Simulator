#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class ExportPanel:DashCard{
 SimRunState? s; Label status=new(); Button export=new();
 public ExportPanel(){CardTitle="Exports";}
 public override void _Ready(){
  base._Ready();
  export=AddButton("Export Audit Bundle",Save,true);
  status=StatusLabel("Writes event, staffing, waste, overload, equipment, item, task, trace, and validation files to user://");
 }
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){
  if(s==null)return;
  status.Text=$"Events {s.EventSeq} | Traces {s.TraceSeq} | Validation {s.ValidationStatus}\nEquipment {s.EquipmentCount} | Items {s.ItemSeq} | Open tasks {s.OpenTaskCount}\nTarget: user:// audit bundle";
 }
 void Save(){
  if(s==null){export.Text="No run state";return;}
  s.RefreshValidation("export");
  // README output contract (8 files) into a per-run folder.
  var dir=$"user://outputs/sim_{s.Scenario}_{s.Seed}";
  DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(dir));
  var ok=true;
  foreach(var (name,content) in Exports.BuildAll(s,System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ",System.Globalization.CultureInfo.InvariantCulture)))
   ok=ok&&Write($"{dir}/{name}",content,true);
  // Existing audit bundle (kept for debugging).
  ok=ok&&Write("event_stream.jsonl",s.AllJsonl);
  ok=ok&&Write("staffing_ledger.txt",s.StaffingLedgerFull);
  ok=ok&&Write("waste_ledger.txt",s.WasteLedgerFull);
  ok=ok&&Write("overload_ledger.txt",s.OverloadLedger);
  ok=ok&&Write("equipment_ledger.txt",s.EquipmentLedger);
  ok=ok&&Write("item_ledger.txt",s.ItemLedger);
  ok=ok&&Write("task_ledger.txt",s.TaskLedger);
  ok=ok&&Write("item_catalog.json",s.ItemCatalogJson);
  ok=ok&&Write("trace_ledger.jsonl",s.TraceLedger);
  ok=ok&&Write("validation_report.txt",s.BuildValidationReport());
  if(!ok){export.Text="Export failed";return;}
  export.Text=$"Exported contract bundle ({s.EventSeq} events)";
 }
 bool Write(string name,string content,bool absolute=false){
  var f=FileAccess.Open(absolute?name:$"user://{name}",FileAccess.ModeFlags.Write);
  if(f==null)return false;
  f.StoreString(content);
  f.Close();
  return true;
 }
}
