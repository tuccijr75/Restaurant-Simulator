using Godot;
namespace RestaurantSimulator;
public partial class SosPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;Text=$"SOS: DT {s.DtSos}s FC {s.FcSos}s DEL {s.DelSos}s";}
}
