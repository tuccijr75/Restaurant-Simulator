using Godot;
namespace RestaurantSimulator;
public partial class PosPanel:Label{
 SimRunState? s; public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;Text=$"POS: {s.Orders} total | DT {s.DriveThru} FC {s.FrontCounter} DEL {s.Delivery} MOB {s.Mobile}";}
}
