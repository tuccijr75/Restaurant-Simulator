using Godot;

namespace RestaurantSimulator;

public partial class KdsPanel:DashCard{
 SimRunState? s; Label status=new();
 public KdsPanel(){CardTitle="KDS";}
 public override void _Ready(){base._Ready();status=StatusLabel();}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;status.Text=$"Active tickets: {s.Tickets}\nRecent ticket events appear in Events/JSONL.\nTicket updates every 3 orders.";}
}
