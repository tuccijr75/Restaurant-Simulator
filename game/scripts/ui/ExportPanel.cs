using Godot;
namespace RestaurantSimulator;
public partial class ExportPanel:Button{
 SimRunState? s;
 public override void _Ready(){Text="Export JSONL";Pressed+=Save;}
 public void Bind(SimRunState st){s=st;}
 void Save(){if(s==null)return;using var f=FileAccess.Open("user://event_stream.jsonl",FileAccess.ModeFlags.Write);f.StoreString(s.AllJsonl);Text="Exported JSONL";}
}
