using Godot;
using System;

namespace RestaurantSimulator;

public partial class LaborPanel:DashCard{
 SimRunState? s; Label status=new();
 public LaborPanel(){CardTitle="Labor";CustomMinimumSize=new Vector2(330,240);}
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
 public override void _Process(double d){if(s==null)return;status.Text=$"Crew {s.Crew} | Eff {s.EffectiveCrew} | Break {s.CrewOnBreak}\nCall-offs {s.CallOffs} | Capacity {s.StaffCapacity}\nCost ${s.LaborCost:0.00} | Labor {s.LaborPercent:0.0}%";}
}
