using Godot;

namespace RestaurantSimulator;

public partial class ExportPanel:DashCard{
 SimRunState? s; Label status=new(); Button export=new();
 public ExportPanel(){CardTitle="Exports";}
 public override void _Ready(){
  base._Ready();
  export=AddButton("Export JSONL",Save,true);
  status=StatusLabel("Event stream export writes to user://event_stream.jsonl");
 }
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;status.Text=$"Events ready: {s.EventSeq}\nJSONL chars: {s.AllJsonl.Length}\nTarget: user://event_stream.jsonl";}
 void Save(){if(s==null){export.Text="No run state";return;}var f=FileAccess.Open("user://event_stream.jsonl",FileAccess.ModeFlags.Write);if(f==null){export.Text="Export failed";return;}f.StoreString(s.AllJsonl);f.Close();export.Text=$"Exported {s.EventSeq} events";}
}
