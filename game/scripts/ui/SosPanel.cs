#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class SosPanel:DashCard{
 SimRunState? s; Label status=new();
 public SosPanel(){CardTitle="SOS";}
 public override void _Ready(){base._Ready();status=StatusLabel();}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;status.Text=$"Drive-thru: {s.DtSos}s\nFront counter: {s.FcSos}s\nDelivery ready: {s.DelSos}s";}
}
