#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class ScenarioPanel:DashCard{
 SimRunState? s; OptionButton pick=new(); Label status=new();
 public ScenarioPanel(){CardTitle="Scenario";}
 public override void _Ready(){
  base._Ready();
  pick.AddItem("normal_day");pick.AddItem("rush_day");pick.AddItem("weather_disruption");pick.AddItem("staffing_call_off");pick.AddItem("equipment_failure");
  pick.ItemSelected+=i=>{if(s!=null)s.Scenario=pick.GetItemText((int)i);};
  Body.AddChild(pick);
  status=StatusLabel();
 }
 public void Bind(SimRunState state){s=state;if(pick.ItemCount>0)s.Scenario=pick.GetItemText(pick.Selected);}
 public override void _Process(double d){if(s==null)return;status.Text=$"Active: {s.Scenario}\nSeed: {s.Seed}\nDaypart: {s.Daypart}";}
}
