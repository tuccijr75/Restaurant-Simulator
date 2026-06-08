using Godot;
using System;
namespace RestaurantSimulator;
public partial class LaborPanel:VBoxContainer{
 SimRunState? s; Label l=new();
 public override void _Ready(){
  Add("+ Crew",()=>{if(s!=null)s.Crew++;});Add("- Crew",()=>{if(s!=null&&s.Crew>1)s.Crew--;});
  Add("+ Lead",()=>{if(s!=null)s.Lead++;});Add("- Lead",()=>{if(s!=null&&s.Lead>0)s.Lead--;});
  Add("+ Shift Mgr",()=>{if(s!=null)s.ShiftMgr++;});Add("- Shift Mgr",()=>{if(s!=null&&s.ShiftMgr>1)s.ShiftMgr--;});
  Add("+ Asst Mgr",()=>{if(s!=null)s.AsstMgr++;});Add("- Asst Mgr",()=>{if(s!=null&&s.AsstMgr>0)s.AsstMgr--;});
  Add("+ Rest Mgr",()=>{if(s!=null)s.RestMgr++;});Add("- Rest Mgr",()=>{if(s!=null&&s.RestMgr>0)s.RestMgr--;});
  Add("Start Break",()=>s?.StartBreak());Add("End Break",()=>s?.EndBreak());Add("Call-Off",()=>s?.CallOff());AddChild(l);
 }
 void Add(string t,Action a){var b=new Button{Text=t};b.Pressed+=a;AddChild(b);}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;l.Text=$"Labor: crew {s.Crew} eff {s.EffectiveCrew} break {s.CrewOnBreak} due {s.BreakDue} calloffs {s.CallOffs} cost ${s.LaborCost:0.00} labor {s.LaborPercent:0.0}%";}
}
