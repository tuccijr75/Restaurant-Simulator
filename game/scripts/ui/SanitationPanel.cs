using Godot;

namespace RestaurantSimulator;

public partial class SanitationPanel:DashCard{
 SimRunState? s; Label status=new();
 public SanitationPanel(){CardTitle="Sanitation";}
 public override void _Ready(){
  base._Ready();
  AddButton("Change Sanitizer",()=>s?.ChangeSanitizer(),true);
  status=StatusLabel();
 }
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;status.Text=$"Sanitizer age: {s.SanitizerAge:0}m\nDue: {s.SanitizerDue}\nCompleted tasks: {s.SanitationTasks}";}
}
