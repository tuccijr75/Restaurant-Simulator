#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class CoveragePanel:DashCard{
 SimRunState? s; Label info=new();
 public CoveragePanel(){CardTitle="Coverage";CustomMinimumSize=new Vector2(330,245);}
 public override void _Ready(){
  base._Ready();
  RowCtl("Kitchen",()=>s?.RemoveKitchenCoverage(),()=>s?.AddKitchenCoverage());
  RowCtl("Fryer",()=>s?.RemoveFryerCoverage(),()=>s?.AddFryerCoverage());
  RowCtl("Drive",()=>s?.RemoveDriveCoverage(),()=>s?.AddDriveCoverage());
  RowCtl("Counter",()=>s?.RemoveCounterCoverage(),()=>s?.AddCounterCoverage());
  RowCtl("Prep",()=>s?.RemovePrepCoverage(),()=>s?.AddPrepCoverage());
  info=StatusLabel();
 }
 void RowCtl(string n,System.Action minus,System.Action plus){var r=Row();var l=new Label{Text=n,CustomMinimumSize=new Vector2(70,28),VerticalAlignment=VerticalAlignment.Center};DashTheme.StyleLabel(l,12,DashTheme.Text);r.AddChild(l);AddRowButton(r,"-",minus);AddRowButton(r,"+",plus,true);}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;info.Text=$"K {s.KitchenCoverage} F {s.FryerCoverage} D {s.DriveCoverage}\nC {s.CounterCoverage} P {s.PrepCoverage}\nUsed {s.CoverageUsed}/{s.CoveragePool} Open {s.CoverageOpen}";}
}
