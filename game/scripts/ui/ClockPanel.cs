#nullable enable
using Godot;

namespace RestaurantSimulator;

public partial class ClockPanel:DashCard{
 SimRunState? s; Label status=null!; Button run=null!;
 public ClockPanel(){CardTitle="Clock / Run";}
 public override void _Ready(){
  base._Ready();
  status=StatusLabel();
  run=AddButton("Start / Pause",()=>{if(s!=null)s.Running=!s.Running;},true);
 }
 public void Bind(SimRunState state){s=state;}
 public override void _Process(double d){if(s==null)return;if(!s.ExternallyDriven)s.Step(d);status.Text=$"Time: {s.TimeText}\nRunning: {s.Running}\nShift minutes: {s.ShiftMinutes:0}";}
}
