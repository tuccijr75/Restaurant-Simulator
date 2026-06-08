using Godot;
namespace RestaurantSimulator;
public partial class LaborPanel:VBoxContainer{
 SimRunState? s; Label l=new();
 public override void _Ready(){var a=new Button{Text="Add Crew"};var c=new Button{Text="Cut Crew"};a.Pressed+=()=>s?.AddCrew();c.Pressed+=()=>s?.CutCrew();AddChild(a);AddChild(c);AddChild(l);}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;l.Text=$"Labor: crew {s.Crew} lead {s.Lead} mgr {s.ShiftMgr} cost ${s.LaborCost:0.00} labor {s.LaborPercent:0.0}%";}
}
