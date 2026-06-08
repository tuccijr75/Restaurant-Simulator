using Godot;

namespace RestaurantSimulator;

public partial class AlertPanel:DashCard{
 SimRunState? s; Label status=new();
 public AlertPanel(){CardTitle="Alerts";}
 public override void _Ready(){base._Ready();status=StatusLabel();}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){
  var text=s?.AlertText??"Alerts: none";
  status.Text=text;
  if(text.StartsWith("ALERT"))DashTheme.StyleLabel(status,13,DashTheme.Danger,true);
  else if(text.StartsWith("Warning"))DashTheme.StyleLabel(status,13,DashTheme.Warn,true);
  else DashTheme.StyleLabel(status,13,DashTheme.Muted);
 }
}
