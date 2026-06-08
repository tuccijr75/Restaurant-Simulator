namespace RestaurantSimulator;
public class SimRunState{
 public int Seed=12345; public string Scenario="normal_day"; public bool Running;
 public double Minute=360; public int Orders,DriveThru,FrontCounter,Delivery,Mobile; double acc;
 public void Step(double d){if(!Running)return;Minute+=d*10;if(Minute>=1440)Minute-=1440;acc+=Rate()*d;while(acc>=1){AddOrder();acc-=1;}}
 void AddOrder(){var x=(Seed+Orders*7+(int)Minute)%10;if(x<4)DriveThru++;else if(x<7)FrontCounter++;else if(x<9)Delivery++;else Mobile++;Orders++;}
 double Rate()=>Scenario=="rush_day"?.9:Scenario=="weather_disruption"?.35:.5+(Seed%7)*.01;
 public int Tickets=>Orders/3;
 public int FryerLoad=>Delivery*5+DriveThru*3;
 public int GrillLoad=>FrontCounter*4+DriveThru*2;
 public int AssemblyLoad=>Orders*3;
 public int ExpoLoad=>Tickets*7+(Scenario=="equipment_failure"?35:0);
 public int KitchenLoad=>FryerLoad+GrillLoad+AssemblyLoad+ExpoLoad;
 public bool DelayRisk=>FryerLoad>120||GrillLoad>120||AssemblyLoad>160||ExpoLoad>120;
 public int DtSos=>120+DriveThru*3+(DelayRisk?30:0);
 public int FcSos=>90+FrontCounter*2+(DelayRisk?20:0);
 public int DelSos=>180+Delivery*4+(DelayRisk?45:0);
 public string TimeText=>$"{(int)(Minute/60):00}:{(int)(Minute%60):00}";
}
