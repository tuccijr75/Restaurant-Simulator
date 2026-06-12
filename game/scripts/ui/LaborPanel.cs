#nullable enable
using Godot;
using System;

namespace RestaurantSimulator;

public partial class LaborPanel:DashCard{
 SimRunState? s; Label status=new();
 public LaborPanel(){CardTitle="Labor";CustomMinimumSize=new Vector2(330,300);}
 public override void _Ready(){
  base._Ready();
  AddRoleRow("Crew",()=>s?.CutCrew(),()=>s?.AddCrew());
  AddRoleRow("Lead",()=>s?.CutLead(),()=>s?.AddLead());
  AddRoleRow("Shift",()=>s?.CutShiftMgr(),()=>s?.AddShiftMgr());
  AddRoleRow("Asst",()=>s?.CutAsstMgr(),()=>s?.AddAsstMgr());
  AddRoleRow("Rest",()=>s?.CutRestMgr(),()=>s?.AddRestMgr());
  var r=Row();AddRowButton(r,"Break",()=>s?.StartBreak());AddRowButton(r,"Return",()=>s?.EndBreak());AddRowButton(r,"CallOff",()=>s?.CallOff(),true);
  var r2=Row();AddRowButton(r2,"Auto Schedule",()=>s?.EnableAutoSchedule(),true);
  status=StatusLabel();
 }
 void AddRoleRow(string name,Action minus,Action plus){
  var r=Row();var l=new Label{Text=name,CustomMinimumSize=new Vector2(58,28),VerticalAlignment=VerticalAlignment.Center};DashTheme.StyleLabel(l,12,DashTheme.Text);r.AddChild(l);AddRowButton(r,"-",minus);AddRowButton(r,"+",plus,true);
 }
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){
  if(s==null)return;
  var hourSign=s.LaborHoursVarianceThis30>=0?"+":"";
  var dollarSign=s.LaborDollarsVarianceThis30>=0?"+":"";
  status.Text=$"Mode: {(s.AutoSchedule?"AUTO SCHEDULE":"MANUAL (Auto Schedule to resume)")} | Sched {SimRunState.ScheduledCrewAt((int)s.Minute)} crew\nOn clock {s.TotalOnClock} | Eff {s.EffectiveCrew} | Break {s.CrewOnBreak}\nCoverage {s.CoverageUsed}/{s.CoveragePool} | Open {s.CoverageOpen}\nLabor actual {s.LaborPercent:0.0}% | Run-rate {s.ProjectedLaborPercentThis30:0.0}%\nAllowance {s.LaborTargetPercent*100:0.0}% | Sales30 ${s.ProjectedSalesThis30:0}\nAllowed ${s.AllowedLaborDollarsThis30:0.00} vs Cost ${s.ProjectedLaborCostThis30:0.00} = {dollarSign}${s.LaborDollarsVarianceThis30:0.00}\nAllowed {s.AllowedLaborHoursThis30:0.00}h vs Scheduled {s.ScheduledLaborHoursThis30:0.00}h = {hourSign}{s.LaborHoursVarianceThis30:0.00}h";
 }
}
