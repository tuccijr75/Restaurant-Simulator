using System;
using System.Collections.Generic;

namespace RestaurantSimulator;

public class SimRunState{
 class Ticket{
  public int Id; public string Channel="drive_thru"; public double Created; public double Grill,Fryer,Assembly,Beverage,Expo; public bool Done=>Grill<=0&&Fryer<=0&&Assembly<=0&&Beverage<=0&&Expo<=0;
 }

 public int Seed=12345; public string Scenario="normal_day",RecentEvents="",RecentJsonl="",AllJsonl="",WasteLedger="",StaffingLedger=""; public bool Running,StationOverloaded;
 public double Minute=360,TimeScale=1.0,PrepAge,LaborCost,ShiftMinutes,BreakTimer,SanitizerAge,TempCheckAge,CoolerTemp=38,HotHoldTemp=145; public int Orders,DriveThru,FrontCounter,Delivery,Mobile,EventSeq,WasteSeq,StaffingSeq,Raw=500,Prep=120,Waste,Crew=6,Lead=1,ShiftMgr=1,AsstMgr=0,RestMgr=0,CrewOnBreak,BreaksTaken,CallOffs,SanitationTasks,CompletedTickets,FriesSold,DrinksSold,MainsSold; double acc,over,recover;
 readonly List<Ticket> activeTickets=new(); readonly int[] tickets30=new int[48]; readonly double[] sales30=new double[48];

 public void Step(double d){if(!Running)return;var sm=Math.Max(0,d)*TimeScale/60.0;Minute+=sm;ShiftMinutes+=sm;if(Minute>=1440)Minute-=1440;LaborCost+=LaborHourly*sm/60;if(CrewOnBreak>0)BreakTimer+=sm;if(Prep>0)PrepAge+=sm;SanitizerAge+=sm;TempCheckAge+=sm;UpdateTemperatures();ProcessTickets(sm);acc+=RatePerSimMinute()*sm;while(acc>=1){AddOrder();acc-=1;}if(PrepAge>=30)ExpirePrep();if(Prep<80)DoPrep();UpdateOverload(sm);}

 void AddOrder(){
  var ch=Channel();Orders++;if(ch=="drive_thru")DriveThru++;else if(ch=="front_counter")FrontCounter++;else if(ch=="delivery")Delivery++;else Mobile++;
  var orderId=$"ord_{Orders:000000}";var ticketId=Orders;var mainChicken=Roll(1)<500;var hasFries=Roll(2)<650;var hasDrink=Roll(3)<600;var itemCount=1+(hasFries?1:0)+(hasDrink?1:0);var check=CheckAmount(itemCount);
  var b=(int)(Minute/30)%48;tickets30[b]++;sales30[b]+=check;
  Emit("order.created",$"{{\"order_id\":\"{orderId}\",\"channel\":\"{ch}\",\"estimated_items\":{itemCount},\"check_amount\":{check:0.00},\"total_orders\":{Orders}}}");
  var t=new Ticket{Id=ticketId,Channel=ch,Created=Minute};
  if(mainChicken){MainsSold++;t.Fryer+=Range(180,300,420,4);t.Assembly+=Range(30,60,120,5);t.Expo+=Range(10,25,60,6);Sell(orderId,"chicken_main","fryer",1);}else{MainsSold++;t.Grill+=Range(60,120,240,7);t.Assembly+=Range(30,60,120,8);t.Expo+=Range(10,25,60,9);Sell(orderId,"burger_main","grill",1);}
  if(hasFries){FriesSold++;t.Fryer+=Range(120,180,210,10);t.Assembly+=Range(8,15,30,11);Sell(orderId,"fries","fryer",1);}
  if(hasDrink){DrinksSold++;t.Beverage+=Range(10,25,45,12);Sell(orderId,"fountain_drink","beverage_expo",1);}
  var draw=1+(hasFries?1:0);if(Prep>=draw)Prep-=draw;else Prep=0;
  activeTickets.Add(t);Emit("ticket.updated",$"{{\"ticket_id\":\"tkt_{ticketId:000000}\",\"order_id\":\"{orderId}\",\"status\":\"queued\",\"channel\":\"{ch}\",\"active_tickets\":{Tickets}}}");
 }

 void Sell(string orderId,string item,string station,int qty){Emit("item.sold",$"{{\"order_id\":\"{orderId}\",\"item_id\":\"{item}\",\"quantity\":{qty},\"station_ids\":[\"{station}\"],\"inventory_draw\":{{\"prep_units\":{qty}}}}}");}

 void ProcessTickets(double sm){
  ProcessGrill(GrillCapacity*sm);ProcessFryer(FryerCapacity*sm);ProcessBeverage(BeverageCapacity*sm);ProcessAssembly(AssemblyCapacity*sm);ProcessExpo(ExpoCapacity*sm);
  for(var i=activeTickets.Count-1;i>=0;i--){var t=activeTickets[i];if(!t.Done)continue;CompletedTickets++;var age=(int)((Minute-t.Created+1440)%1440*60);Emit("ticket.updated",$"{{\"ticket_id\":\"tkt_{t.Id:000000}\",\"order_id\":\"ord_{t.Id:000000}\",\"status\":\"completed\",\"channel\":\"{t.Channel}\",\"queue_seconds\":{age},\"active_tickets\":{activeTickets.Count-1}}}");activeTickets.RemoveAt(i);}
 }
 void ProcessGrill(double c){for(var i=0;i<activeTickets.Count&&c>0;i++){var t=activeTickets[i];if(t.Grill<=0)continue;var x=Math.Min(t.Grill,c);t.Grill-=x;c-=x;}}
 void ProcessFryer(double c){for(var i=0;i<activeTickets.Count&&c>0;i++){var t=activeTickets[i];if(t.Fryer<=0)continue;var x=Math.Min(t.Fryer,c);t.Fryer-=x;c-=x;}}
 void ProcessBeverage(double c){for(var i=0;i<activeTickets.Count&&c>0;i++){var t=activeTickets[i];if(t.Beverage<=0)continue;var x=Math.Min(t.Beverage,c);t.Beverage-=x;c-=x;}}
 void ProcessAssembly(double c){for(var i=0;i<activeTickets.Count&&c>0;i++){var t=activeTickets[i];if(t.Grill>0||t.Fryer>0||t.Assembly<=0)continue;var x=Math.Min(t.Assembly,c);t.Assembly-=x;c-=x;}}
 void ProcessExpo(double c){for(var i=0;i<activeTickets.Count&&c>0;i++){var t=activeTickets[i];if(t.Grill>0||t.Fryer>0||t.Assembly>0||t.Beverage>0||t.Expo<=0)continue;var x=Math.Min(t.Expo,c);t.Expo-=x;c-=x;}}

 public void ManualPrep(){DoPrep();} public void ManualDiscard(){if(Prep<=0)return;var w=Prep;Prep=0;PrepAge=0;RecordWaste("manager_discard",w);}
 public void AddCrew(){Crew++;RecordStaffing("role_added","crew_member","crew_shift_pool","kitchen","assigned","manager_adjustment");} public void CutCrew(){if(Crew<=1||EffectiveCrew<=1)return;Crew--;RecordStaffing("role_removed","crew_member","crew_shift_pool","kitchen","ended","manager_adjustment");}
 public void AddLead(){Lead++;RecordStaffing("role_added","team_leader","crew_shift_lead","floor","assigned","manager_adjustment");} public void CutLead(){if(Lead<=0)return;Lead--;RecordStaffing("role_removed","team_leader","crew_shift_lead","floor","ended","manager_adjustment");}
 public void AddShiftMgr(){ShiftMgr++;RecordStaffing("role_added","shift_manager","manager_shift","floor","assigned","manager_adjustment");} public void CutShiftMgr(){if(ShiftMgr<=1)return;ShiftMgr--;RecordStaffing("role_removed","shift_manager","manager_shift","floor","ended","manager_adjustment");}
 public void AddAsstMgr(){AsstMgr++;RecordStaffing("role_added","assistant_manager","manager_assistant","floor","assigned","manager_adjustment");} public void CutAsstMgr(){if(AsstMgr<=0)return;AsstMgr--;RecordStaffing("role_removed","assistant_manager","manager_assistant","floor","ended","manager_adjustment");}
 public void AddRestMgr(){RestMgr++;RecordStaffing("role_added","restaurant_manager","manager_restaurant","floor","assigned","manager_adjustment");} public void CutRestMgr(){if(RestMgr<=0)return;RestMgr--;RecordStaffing("role_removed","restaurant_manager","manager_restaurant","floor","ended","manager_adjustment");}
 public void StartBreak(){if(EffectiveCrew<=1)return;CrewOnBreak++;BreakTimer=0;RecordStaffing("break_started","crew_member","crew_shift_break","break","break","break_coverage");} public void EndBreak(){if(CrewOnBreak<=0)return;CrewOnBreak--;BreaksTaken++;BreakTimer=0;RecordStaffing("break_ended","crew_member","crew_shift_break","kitchen","returned","break_coverage");} public void CallOff(){if(Crew<=1)return;Crew--;CallOffs++;if(CrewOnBreak>Crew-1)CrewOnBreak=Math.Max(0,Crew-1);RecordStaffing("call_off","crew_member","crew_shift_calloff","kitchen","call_off","call_off");}
 public void ChangeSanitizer(){SanitizerAge=0;SanitationTasks++;} public void CheckTemps(){TempCheckAge=0;UpdateTemperatures();}

 void RecordStaffing(string action,string role,string worker,string station,string movement,string reason){StaffingSeq++;var src=$"evt_{EventSeq+1:000000}";StaffingLedger=$"{StaffingSeq} {TimeText} {movement} {role} {worker} station {station} eff {EffectiveCrew} cap {StaffCapacity} reason {reason} src {src}\n"+StaffingLedger;if(StaffingLedger.Length>800)StaffingLedger=StaffingLedger[..800];Emit("staff.assignment.updated",$"{{\"assignment_id\":\"asg_{StaffingSeq:000000}\",\"action\":\"{action}\",\"role_id\":\"{role}\",\"synthetic_worker_ref\":\"{worker}\",\"station_id\":\"{station}\",\"movement_type\":\"{movement}\",\"reason\":\"{reason}\",\"crew\":{Crew},\"effective_crew\":{EffectiveCrew},\"on_break\":{CrewOnBreak},\"call_offs\":{CallOffs},\"staff_capacity\":{StaffCapacity}}}");}
 void DoPrep(){var n=Raw>=120?120:Raw;if(n<=0)return;Raw-=n;Prep+=n;PrepAge=0;Emit("prep.confirmed",$"{{\"prep_units\":{n},\"raw_remaining\":{Raw},\"prep_available\":{Prep}}}");}
 void ExpirePrep(){var w=Prep/4;if(w<1)w=1;Prep-=w;PrepAge=0;RecordWaste("hold_time_expired",w);}
 void RecordWaste(string r,int u){Waste+=u;WasteSeq++;WasteLedger=$"{WasteSeq} {TimeText} {r} {u}u ${u*0.75:0.00}\n"+WasteLedger;if(WasteLedger.Length>500)WasteLedger=WasteLedger[..500];Emit("waste.recorded",$"{{\"reason\":\"{r}\",\"waste_units\":{u},\"waste_cost\":{u*0.75:0.00},\"total_waste\":{Waste},\"prep_available\":{Prep}}}");}

 string Channel(){var x=Roll(20);return x<600?"drive_thru":x<760?"front_counter":x<860?"delivery":"mobile";}
 int Roll(int salt){var v=(long)Seed*1103515245L+(long)(Orders+1)*12345L+(long)salt*9973L+(long)((int)Minute)*31L;return (int)(Math.Abs(v)%1000);}
 double Range(double min,double target,double max,int salt){var r=Roll(salt)/999.0;return r<.5?min+(target-min)*(r*2):target+(max-target)*((r-.5)*2);}
 double CheckAmount(int items)=>10.0+(Roll(30)%201)/100.0+Math.Max(0,items-2)*1.25;

 void UpdateOverload(double m){var was=StationOverloaded;if(DelayRisk){over+=m;recover=0;if(over>=5)StationOverloaded=true;}else{over=0;if(StationOverloaded){recover+=m;if(recover>=4)StationOverloaded=false;}}if(!was&&StationOverloaded)Emit("station.overloaded",StationPayload());if(was&&!StationOverloaded)Emit("station.recovered",StationPayload());}
 void UpdateTemperatures(){CoolerTemp=38+(Scenario=="equipment_failure"?4:0)+Math.Min(3,PrepAge/20);HotHoldTemp=145-(Scenario=="equipment_failure"?9:0)-(StationOverloaded?3:0)-Math.Min(6,PrepAge/10);}
 string StationPayload()=>$"{{\"active_tickets\":{Tickets},\"completed_tickets\":{CompletedTickets},\"fryer_load\":{FryerLoad},\"grill_load\":{GrillLoad},\"assembly_load\":{AssemblyLoad},\"expo_load\":{ExpoLoad},\"kitchen_load\":{KitchenLoad},\"staff_capacity\":{StaffCapacity},\"net_load\":{NetKitchenLoad}}}";
 void Emit(string t,string p){var e=new SimEvent(++EventSeq,TimeText,t,Scenario,Seed,Daypart,p);AllJsonl+=e.Jsonl+"\n";RecentEvents=e.Text+"\n"+RecentEvents;RecentJsonl=e.Jsonl+"\n"+RecentJsonl;if(RecentEvents.Length>500)RecentEvents=RecentEvents[..500];if(RecentJsonl.Length>1000)RecentJsonl=RecentJsonl[..1000];}

 double RatePerSimMinute(){if(Minute<360)return 0;var daily=Scenario=="rush_day"?1180:Scenario=="weather_disruption"?650:920;return daily*DaypartShare()/DaypartMinutes()*Curve()*ScenarioMultiplier();}
 double ScenarioMultiplier()=>Scenario=="rush_day"?1.10:Scenario=="weather_disruption"?.75:Scenario=="equipment_failure"?.95:1.0;
 double DaypartShare()=>Daypart=="breakfast"?.18:Daypart=="mid_morning"?.07:Daypart=="lunch"?.30:Daypart=="afternoon"?.10:Daypart=="dinner"?.30:.05;
 double DaypartMinutes()=>Daypart=="breakfast"?240:Daypart=="mid_morning"?90:Daypart=="lunch"?150:Daypart=="afternoon"?150:Daypart=="dinner"?240:210;
 double Curve(){var peak=Daypart=="breakfast"?480:Daypart=="mid_morning"?615:Daypart=="lunch"?750:Daypart=="afternoon"?930:Daypart=="dinner"?1095:1260;var start=Daypart=="breakfast"?360:Daypart=="mid_morning"?600:Daypart=="lunch"?690:Daypart=="afternoon"?840:Daypart=="dinner"?990:1230;var end=Daypart=="breakfast"?600:Daypart=="mid_morning"?690:Daypart=="lunch"?840:Daypart=="afternoon"?990:Daypart=="dinner"?1230:1440;var half=Math.Max(1,Math.Max(peak-start,end-peak));var shape=1-Math.Min(1,Math.Abs(Minute-peak)/half);return .75+.5*shape;}

 double SumGrill(){double n=0;foreach(var t in activeTickets)n+=Math.Max(0,t.Grill);return n;} double SumFryer(){double n=0;foreach(var t in activeTickets)n+=Math.Max(0,t.Fryer);return n;} double SumAssembly(){double n=0;foreach(var t in activeTickets)n+=Math.Max(0,t.Assembly);return n;} double SumBeverage(){double n=0;foreach(var t in activeTickets)n+=Math.Max(0,t.Beverage);return n;} double SumExpo(){double n=0;foreach(var t in activeTickets)n+=Math.Max(0,t.Expo);return n;}
 int CountActive(string ch){var n=0;foreach(var t in activeTickets)if(t.Channel==ch)n++;return n;}
 double GrillCapacity=>300+EffectiveCrew*45+Lead*30+ShiftMgr*20; double FryerCapacity=>360+EffectiveCrew*60+Lead*30+ShiftMgr*20; double AssemblyCapacity=>260+EffectiveCrew*55+Lead*35+ShiftMgr*25; double BeverageCapacity=>300+EffectiveCrew*60+Lead*20+ShiftMgr*20; double ExpoCapacity=>320+EffectiveCrew*50+Lead*35+ShiftMgr*30;

 public double Sales=>Orders*11.0; public double WasteCost=>Waste*0.75; public double FoodCostPercent=>Sales<=0?0:WasteCost/Sales*100; public double LaborHourly=>Crew*16+Lead*18+ShiftMgr*22+AsstMgr*28+RestMgr*35; public double LaborPercent=>Sales<=0?0:LaborCost/Sales*100;
 public int EffectiveCrew=>Crew-CrewOnBreak; public bool BreakDue=>ShiftMinutes>240&&BreaksTaken==0; public int StaffCapacity=>(int)(GrillCapacity+FryerCapacity+AssemblyCapacity+BeverageCapacity+ExpoCapacity); public int NetKitchenLoad=>KitchenLoad-StaffCapacity*5;
 public int PrepQuality=>PrepAge>=30?0:100-(int)(PrepAge*3); public int Tickets=>activeTickets.Count; public int FryerLoad=>(int)Math.Ceiling(SumFryer()); public int GrillLoad=>(int)Math.Ceiling(SumGrill()); public int AssemblyLoad=>(int)Math.Ceiling(SumAssembly()); public int ExpoLoad=>(int)Math.Ceiling(SumExpo()+SumBeverage());
 public int KitchenLoad=>FryerLoad+GrillLoad+AssemblyLoad+ExpoLoad; public bool DelayRisk=>Tickets>30||KitchenLoad>StaffCapacity*6||FryerLoad>FryerCapacity*8||GrillLoad>GrillCapacity*8||AssemblyLoad>AssemblyCapacity*8||ExpoLoad>(ExpoCapacity+BeverageCapacity)*8;
 public bool SanitizerDue=>SanitizerAge>=120; public bool TempCheckDue=>TempCheckAge>=120; public bool TempOutOfRange=>CoolerTemp>41||HotHoldTemp<135;
 public string AlertText=>StationOverloaded?"ALERT: station overloaded":TempOutOfRange?"ALERT: temperature out of range":TempCheckDue?"Warning: temperature check due":SanitizerDue?"Warning: sanitizer change due":BreakDue?"Warning: break due":DelayRisk?"Warning: station delay risk":PrepQuality<50?"Warning: prep quality low":Prep<80?"Warning: prep low":"Alerts: none";
 public int DtSos=>(int)Math.Min(900,258+CountActive("drive_thru")*18+(StationOverloaded?90:0)); public int FcSos=>(int)Math.Min(720,180+CountActive("front_counter")*20+(StationOverloaded?75:0)); public int DelSos=>(int)Math.Min(2100,420+CountActive("delivery")*35+(StationOverloaded?180:0));
 public int TicketsThis30=>tickets30[(int)(Minute/30)%48]; public int TicketsThis60=>tickets30[(int)(Minute/30)%48]+tickets30[((int)(Minute/30)+47)%48]; public double SalesThis30=>sales30[(int)(Minute/30)%48]; public double SalesThis60=>sales30[(int)(Minute/30)%48]+sales30[((int)(Minute/30)+47)%48];
 public string Daypart=>Minute<360?"late_night":Minute<600?"breakfast":Minute<690?"mid_morning":Minute<840?"lunch":Minute<990?"afternoon":Minute<1230?"dinner":"late_night";
 public string TimeText=>$"{(int)(Minute/60):00}:{(int)(Minute%60):00}";
}
