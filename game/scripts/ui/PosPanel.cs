#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class PosPanel:DashCard{
 SimRunState? s; Label status=new();
 public PosPanel(){CardTitle="POS";}
 public override void _Ready(){base._Ready();status=StatusLabel();}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;status.Text=$"Orders: {s.Orders}\nDrive-thru: {s.DriveThru}  Front: {s.FrontCounter}\nDelivery: {s.Delivery}  Mobile: {s.Mobile}";}
}
