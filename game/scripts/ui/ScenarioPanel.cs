using Godot;
namespace RestaurantSimulator;
public partial class ScenarioPanel:OptionButton{
 SimRunState? s;
 public override void _Ready(){AddItem("normal_day");AddItem("rush_day");AddItem("weather_disruption");AddItem("staffing_call_off");AddItem("equipment_failure");ItemSelected+=i=>{if(s!=null)s.Scenario=GetItemText((int)i);};}
 public void Bind(SimRunState state){s=state;s.Scenario=GetItemText(Selected);}
}
