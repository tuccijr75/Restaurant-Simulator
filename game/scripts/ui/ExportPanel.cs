#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class ExportPanel:DashCard{
 SimRunState? s; Label status=new(); Button export=new();
 public ExportPanel(){CardTitle="Exports";}
 public override void _Ready(){
  base._Ready();
  export=AddButton("Export Audit Bundle",Save,true);
  status=StatusLabel("Writes event stream, staffing, waste, overload, equipment, item, trace, and validation reports to user://");
 }
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){
  if(s==null)return;
  status.Text=$"Events {s.EventSeq} | Traces {s.TraceSeq} | Validation {s.ValidationStatus}\nOverloads {s.OverloadSeq} | Equipment {s.EquipmentCount} | Items {s.ItemSeq}\nTarget: user://event_stream.jsonl + audit files";
 }
 void Save(){
  if(s==null){export.Text="No run state";return;}
  s.RefreshValidation("export");
  if(!Write("event_stream.jsonl",s.AllJsonl)||!Write("staffing_ledger.txt",s.StaffingLedger)||!Write("waste_ledger.txt",s.WasteLedger)||!Write("overload_ledger.txt",s.OverloadLedger)||!Write("equipment_ledger.txt",s.EquipmentLedger)||!Write("item_ledger.txt",s.ItemLedger)||!Write("item_catalog.json",s.ItemCatalogJson)||!Write("trace_ledger.jsonl",s.TraceLedger)||!Write("validation_report.txt",s.BuildValidationReport())){export.Text="Export failed";return;}
  export.Text=$"Exported {s.EventSeq} events + {s.TraceSeq} traces";
 }
 bool Write(string name,string content){
  var f=FileAccess.Open($"user://{name}",FileAccess.ModeFlags.Write);
  if(f==null)return false;
  f.StoreString(content);
  f.Close();
  return true;
 }
}
