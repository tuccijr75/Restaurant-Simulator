using Godot;
using System;
namespace RestaurantSimulator;
public partial class LaborPanel:VBoxContainer{
 SimRunState? s; Label l=new();
 public override void _Ready(){
  Add("+ Crew",()=>s?.AddCrew());Add("- Crew",()=>s?.CutCrew());
  Add("+ Lead",()=>s?.AddLead());Add("- Lead",()=>s?.CutLead());
  Add("+ Shift Mgr",()=>s?.AddShiftMgr());Add("- Shift Mgr",()=>s?.CutShiftMgr());
  Add("+ Asst Mgr",()=>s?.AddAsstMgr());Add("- Asst Mgr",()=>s?.CutAsstMgr());
  Add("+ Rest Mgr",()=>s?.AddRestMgr());Add("- Rest Mgr",()=>s?.CutRestMgr());
  Add("Start Break",()=>s?.StartBreak());Add("End Break",()=>s?.EndBreak());Add("Call-Off",()=>s?.CallOff());AddChild(l);
 }
 void Add(string t,Action a){var b=new Button{Text=t};b.Pressed+=a;AddChild(b);}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;l.Text=$"Labor: crew {s.Crew} eff {s.EffectiveCrew} break {s.CrewOnBreak} due {s.BreakDue} calloffs {s.CallOffs} capacity {s.StaffCapacity} cost ${s.LaborCost:0.00} labor {s.LaborPercent:0.0}%";}
}