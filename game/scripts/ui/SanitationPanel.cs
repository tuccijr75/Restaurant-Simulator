using Godot;
namespace RestaurantSimulator;
public partial class SanitationPanel:VBoxContainer{
 SimRunState? s; Label l=new();
 public override void _Ready(){var b=new Button{Text="Change Sanitizer"};b.Pressed+=()=>s?.ChangeSanitizer();AddChild(b);AddChild(l);}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){if(s==null)return;l.Text=$"Sanitation: sanitizer age {s.SanitizerAge:0}m due {s.SanitizerDue} tasks {s.SanitationTasks}";}
}