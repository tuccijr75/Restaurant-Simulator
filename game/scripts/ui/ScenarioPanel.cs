using Godot;
namespace RestaurantSimulator;
public partial class ScenarioPanel:OptionButton{
 public override void _Ready(){
  AddItem("Normal Day");AddItem("Rush Day");AddItem("Weather Disruption");AddItem("Staffing Call-Off");AddItem("Equipment Failure");
 }
 public string CurrentScenario=>GetItemText(Selected);
}
