using Godot;
namespace RestaurantSimulator;
public partial class ExportPanel:Button{
 SimRunState? s;
 public override void _Ready(){Text="Export JSONL";Pressed+=Save;}
 public void Bind(SimRunState st){s=st;}
 void Save(){if(s==null){Text="No run state";return;}var f=FileAccess.Open("user://event_stream.jsonl",FileAccess.ModeFlags.Write);if(f==null){Text="Export failed";return;}f.StoreString(s.AllJsonl);f.Close();Text=$"Exported {s.EventSeq} events";}
}
