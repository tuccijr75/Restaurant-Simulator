namespace RestaurantSimulator;
public class SimRunState{
 public int Seed=12345;
 public string Scenario="normal_day";
 public bool Running;
 public double Minute=360;
 public int Orders;
 public void Step(double d){if(!Running)return;Minute+=d*10;if(Minute>=1440)Minute-=1440;}
 public string TimeText=>$"{(int)(Minute/60):00}:{(int)(Minute%60):00}";
}
