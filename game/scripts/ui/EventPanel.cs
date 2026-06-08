using Godot;

namespace RestaurantSimulator;

public partial class EventPanel:DashCard{
 SimRunState? s; Label status=new();
 public EventPanel(){CardTitle="Events";CustomMinimumSize=new Vector2(330,220);}
 public override void _Ready(){base._Ready();status=StatusLabel();}
 public void Bind(SimRunState st){s=st;}
 public override void _Process(double d){status.Text=DashTheme.Preview(s?.RecentEvents??"",6,320);}
}
