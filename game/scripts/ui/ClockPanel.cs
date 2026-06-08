using Godot;
namespace RestaurantSimulator;
public partial class ClockPanel:Label{
 double m=360; bool run;
 public override void _Ready(){Text="Clock 06:00";}
 public override void _Process(double d){if(!run)return;m+=d*10;Text=$"Clock {(int)(m/60):00}:{(int)(m%60):00}";}
 public void Toggle(){run=!run;}
}
