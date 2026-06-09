#nullable enable
using Godot;
using System;

namespace RestaurantSimulator;

public partial class LaborPanel:DashCard{
 SimRunState? s; Label status=new();
 public LaborPanel(){CardTitle="Labor";CustomMinimumSize=new Vector2(330,280);}
 public override void _Ready(){
  base._Ready();
  AddRoleRow("Crew",()=>s?.CutCrew(),()=>s?.AddCrew());
  AddRoleRow("Lead",()=>s?.CutLead(),()=>s?.AddLead());
  AddRoleRow("Shift",()=>s?.CutShiftMgr(),()=>s?.AddShiftMgr());
  AddRoleRow("Asst",()=>s?.CutAsstMgr(),()=>s?.AddAsstMgr());
  AddRoleRow("Rest",()=>s?.CutRestMgr(),()=>s?.AddRestMgr());
  var r=Row();AddRowButton(r,"Break",()=>s?.StartBreak());AddRowButton(r,"Return",()=>s?.EndBreak());AddRowButton(r,"CallOff",()=>s?.CallOff(),true);
  status=StatusLabel();
 }
 void AddRoleRow(string name,Action minus,Action plus){
  var r=Row();var l=new Label{Text=name,CustomMinimumSize=new Vector2(58,28),VerticalAlignment=VerticalAlignment.Center};DashTheme.StyleLabel(l,12,DashTheme.Text);r.AddChild(l);AddRowButton(r,"-",minus);AddRowButton(r,"+",plus,true);
 }
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){
  if(s==null)return;
  var sign=s.LaborHoursVarianceThis30>=0?"+":"";
  status.Text=$"On clock {s.TotalOnClock} | Avail {s.CoveragePool} | Covered {s.CoverageUsed}\nCrew {s.Crew} | Eff {s.EffectiveCrew} | Break {s.CrewOnBreak}\nRun {s.ProjectedLaborPercentThis30:0.0}% / Allow {s.LaborTargetPercent*100:0.0}%\nHours run {s.ScheduledLaborHoursThis30:0.00} | Allow {s.AllowedLaborHoursThis30:0.00}\nOver/Under {sign}{s.LaborHoursVarianceThis30:0.00} hrs";
 }
}
