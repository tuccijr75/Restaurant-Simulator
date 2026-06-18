#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class PosPanel:DashCard{
 SimRunState? s; Label status=null!;
 public PosPanel(){CardTitle="POS";CustomMinimumSize=new Vector2(330,300);}
 public override void _Ready(){
  base._Ready();
  status=StatusLabel();
  status.CustomMinimumSize=new Vector2(0,220);
  status.SizeFlagsVertical=Control.SizeFlags.ExpandFill;
 }
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){
  if(s==null)return;
  var avg=s.Orders==0?0:s.Sales/s.Orders;
  status.Text=$"Open tickets {s.Tickets} | Completed {s.CompletedTickets}\n"
   +$"Sales ${s.Sales:0} | Avg ${avg:0.00} | Pace {s.SalesPacePercent:0}%\n"
   +$"This 30m: {s.TicketsThis30} orders / ${s.SalesThis30:0}  Proj ${s.ProjectedSalesThis30:0}\n"
   +$"Channels  DT {s.DriveThru}  FC {s.FrontCounter}  MOB {s.Mobile}  DEL {s.Delivery}\n"
   +$"SOS  DT {s.DtSos}s  Counter {s.FcSos}s  Delivery {s.DelSos}s\n"
   +$"Register coverage  Counter {s.CounterCoverage}  Drive {s.DriveCoverage}  Open labor {s.CoverageOpen}\n"
   +$"Queue risk {(s.DelayRisk?"YES":"no")} | Abandoned {s.AbandonedTickets} | Balked {s.BalkedCars}\n\n"
   +s.PosTicketSummary();
 }
}
