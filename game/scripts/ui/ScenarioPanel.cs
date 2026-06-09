#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class ScenarioPanel:DashCard{
 static readonly string[] Scenarios={"normal_day","slow_day","rush_day","weather_disruption","staffing_call_off","equipment_failure","local_event_surge","school_event_surge","holiday_pattern","multi_rush_condition"};
 SimRunState? s; OptionButton pick=new(); Label status=new();
 public ScenarioPanel(){CardTitle="Scenario";}
 public override void _Ready(){
  base._Ready();
  foreach(var scenario in Scenarios)pick.AddItem(scenario);
  pick.ItemSelected+=i=>{if(s!=null)s.Scenario=pick.GetItemText((int)i);};
  Body.AddChild(pick);
  status=StatusLabel();
 }
 public void Bind(SimRunState state){s=state;if(pick.ItemCount>0)s.Scenario=pick.GetItemText(pick.Selected);}
 public override void _Process(double d){if(s==null)return;status.Text=$"Active: {s.Scenario}\nSeed: {s.Seed}\nDaypart: {s.Daypart}";}
}
