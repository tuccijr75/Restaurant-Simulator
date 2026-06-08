namespace RestaurantSimulator;
public class SimRunState{
 public int Seed=12345; public string Scenario="normal_day",RecentEvents="",RecentJsonl=""; public bool Running,StationOverloaded;
 public double Minute=360; public int Orders,DriveThru,FrontCounter,Delivery,Mobile,EventSeq; double acc,over,recover;
 public void Step(double d){if(!Running)return;var sm=d*10;Minute+=sm;if(Minute>=1440)Minute-=1440;acc+=Rate()*d;while(acc>=1){AddOrder();acc-=1;}UpdateOverload(sm);}
 void AddOrder(){var x=(Seed+Orders*7+(int)Minute)%10;if(x<4)DriveThru++;else if(x<7)FrontCounter++;else if(x<9)Delivery++;else Mobile++;Orders++;Emit("order.created");if(Orders%3==0)Emit("ticket.updated");}
 void UpdateOverload(double m){var was=StationOverloaded;if(DelayRisk){over+=m;recover=0;if(over>=5)StationOverloaded=true;}else{over=0;if(StationOverloaded){recover+=m;if(recover>=4)StationOverloaded=false;}}if(!was&&StationOverloaded)Emit("station.overloaded");if(was&&!StationOverloaded)Emit("station.recovered");}
 void Emit(string t){var e=new SimEvent(++EventSeq,TimeText,t,Scenario,Seed);RecentEvents=e.Text+"\n"+RecentEvents;RecentJsonl=e.Jsonl+"\n"+RecentJsonl;if(RecentEvents.Length>500)RecentEvents=RecentEvents[..500];if(RecentJsonl.Length>1000)RecentJsonl=RecentJsonl[..1000];}
 double Rate()=>Scenario=="rush_day"?.9:Scenario=="weather_disruption"?.35:.5+(Seed%7)*.01;
 public int Tickets=>Orders/3;
 public int FryerLoad=>Delivery*5+DriveThru*3;
 public int GrillLoad=>FrontCounter*4+DriveThru*2;
 public int AssemblyLoad=>Orders*3;
 public int ExpoLoad=>Tickets*7+(Scenario=="equipment_failure"?35:0);
 public int KitchenLoad=>FryerLoad+GrillLoad+AssemblyLoad+ExpoLoad;
 public bool DelayRisk=>FryerLoad>120||GrillLoad>120||AssemblyLoad>160||ExpoLoad>120;
 public string AlertText=>StationOverloaded?"ALERT: station overloaded":DelayRisk?"Warning: station delay risk":"Alerts: none";
 public int DtSos=>120+DriveThru*3+(StationOverloaded?30:0);
 public int FcSos=>90+FrontCounter*2+(StationOverloaded?20:0);
 public int DelSos=>180+Delivery*4+(StationOverloaded?45:0);
 public string TimeText=>$"{(int)(Minute/60):00}:{(int)(Minute%60):00}";
}
