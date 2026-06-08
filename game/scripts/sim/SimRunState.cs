namespace RestaurantSimulator;
public class SimRunState{
 public int Seed=12345; public string Scenario="normal_day"; public bool Running;
 public double Minute=360; public int Orders; double acc;
 public void Step(double d){if(!Running)return;Minute+=d*10;if(Minute>=1440)Minute-=1440;acc+=Rate()*d;if(acc>=1){var n=(int)acc;Orders+=n;acc-=n;}}
 double Rate()=>Scenario=="rush_day"?.9:Scenario=="weather_disruption"?.35:.5+(Seed%7)*.01;
 public int Tickets=>Orders/3;
 public int Sos=>120+Tickets*8;
 public string TimeText=>$"{(int)(Minute/60):00}:{(int)(Minute%60):00}";
}
