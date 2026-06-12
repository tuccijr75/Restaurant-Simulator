#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class ScenarioPanel:DashCard{
 static readonly string[] Scenarios={"normal_day","slow_day","rush_day","weather_disruption","staffing_call_off","equipment_failure","local_event_surge","school_event_surge","holiday_pattern","multi_rush_condition"};
 static readonly double[] Speeds={1.0,10.0,60.0,300.0,900.0};
 SimRunState? s; OptionButton scenarioPick=new(); OptionButton speedPick=new(); Label status=new();
 public ScenarioPanel(){CardTitle="Scenario";}
 public override void _Ready(){
  base._Ready();
  foreach(var scenario in Scenarios)scenarioPick.AddItem(scenario);
  foreach(var speed in Speeds)speedPick.AddItem($"{speed:0.#}x");
  scenarioPick.ItemSelected+=i=>{if(s!=null)s.Scenario=scenarioPick.GetItemText((int)i);};
  speedPick.ItemSelected+=i=>{if(s!=null)s.TimeScale=Speeds[(int)i];};
  var scenarioRow=Row();
  scenarioRow.AddChild(FieldLabel("Scenario"));
  scenarioPick.SizeFlagsHorizontal=SizeFlags.ExpandFill;
  scenarioRow.AddChild(scenarioPick);
  var speedRow=Row();
  speedRow.AddChild(FieldLabel("Speed"));
  speedPick.SizeFlagsHorizontal=SizeFlags.ExpandFill;
  speedRow.AddChild(speedPick);
  status=StatusLabel();
 }
 public void Bind(SimRunState state){
  s=state;
  if(scenarioPick.ItemCount>0)s.Scenario=scenarioPick.GetItemText(scenarioPick.Selected);
  if(speedPick.ItemCount>0){speedPick.Select(0);s.TimeScale=Speeds[0];}
 }
 public override void _Process(double d){if(s==null)return;status.Text=$"Active: {s.Scenario}\nSeed: {s.Seed}\nDaypart: {s.Daypart}\nSpeed: {s.TimeScale:0.#}x";}
 Label FieldLabel(string text){
  var label=new Label{Text=text,CustomMinimumSize=new Vector2(72,0)};
  DashTheme.StyleLabel(label,12,DashTheme.Muted);
  return label;
 }
}
