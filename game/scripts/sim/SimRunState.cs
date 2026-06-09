using System;
using System.Collections.Generic;
using System.Globalization;

namespace RestaurantSimulator;

public class SimRunState{
 class Ticket{
  public int Id;
  public string Channel="drive_thru";
  public double Created,Grill,Fryer,Assembly,Beverage,Expo;
  public bool Done=>Grill<=0&&Fryer<=0&&Assembly<=0&&Beverage<=0&&Expo<=0;
 }

 readonly List<Ticket> activeTickets=new();
 readonly int[] tickets30=new int[48];
 readonly double[] sales30=new double[48];
 readonly Dictionary<string,double> inventory=new(){["main_protein"]=520,["side_base"]=420,["drink_mix"]=300,["prep_pack"]=260};
 double acc,over,recover;

 public int Seed=12345,Orders,DriveThru,FrontCounter,Delivery,Mobile,EventSeq,WasteSeq,StaffingSeq,Raw=500,Prep=120,Waste,Crew=6,Lead=1,ShiftMgr=1,AsstMgr=0,RestMgr=0,CrewOnBreak,BreaksTaken,CallOffs,SanitationTasks,CompletedTickets,FriesSold,DrinksSold,MainsSold;
 public int KitchenCoverage=1,FryerCoverage=1,DriveCoverage=2,CounterCoverage=1,PrepCoverage=1;
 public double Minute=360,TimeScale=1.0,PrepAge,LaborCost,ShiftMinutes,BreakTimer,SanitizerAge,TempCheckAge,CoolerTemp=38,HotHoldTemp=145;
 public string Scenario="normal_day",RecentEvents="",RecentJsonl="",AllJsonl="",WasteLedger="",StaffingLedger="";
 public bool Running,StationOverloaded;

 public void Step(double d){
  if(!Running)return;
  ClampCoverage();
  var sm=Math.Max(0,d)*TimeScale/60.0;
  Minute=(Minute+sm)%1440;
  ShiftMinutes+=sm;
  LaborCost+=LaborHourly*sm/60;
  if(CrewOnBreak>0)BreakTimer+=sm;
  if(Prep>0)PrepAge+=sm;
  SanitizerAge+=sm;
  TempCheckAge+=sm;
  UpdateTemperatures();
  ProcessTickets(sm);
  acc+=RatePerSimMinute()*sm;
  while(acc>=1){AddOrder();acc-=1;}
  if(PrepAge>=30)ExpirePrep();
  if(Prep<80&&PrepCoverage>0)DoPrep("auto_threshold");
  UpdateOverload(sm);
 }

 void AddOrder(){
  var ch=Channel();
  Orders++;
  if(ch=="drive_thru")DriveThru++;else if(ch=="lobby")FrontCounter++;else if(ch=="delivery")Delivery++;else Mobile++;
  var oid=$"ord_{Orders:000000}";
  var mainChicken=Roll(1)<500;
  var hasFries=Roll(2)<650;
  var hasDrink=Roll(3)<600;
  var count=1+(hasFries?1:0)+(hasDrink?1:0);
  var check=CheckAmount(count);
  var b=(int)(Minute/30)%48;
  tickets30[b]++;
  sales30[b]+=check;
  Emit("order.created",$"{{\"order_id\":\"{oid}\",\"customer_segment\":\"{CustomerSegment}\",\"channel\":\"{ch}\",\"estimated_items\":{count},\"expected_ticket_seconds\":{ExpectedTicketSeconds(ch,count)}}}");
  var t=new Ticket{Id=Orders,Channel=ch,Created=Minute};
  if(mainChicken){
   MainsSold++;
   t.Fryer+=Range(180,300,420,4);t.Assembly+=Range(30,60,120,5);t.Expo+=Range(10,25,60,6);
   Sell(oid,"fried_main","fryer",new Dictionary<string,double>{{"main_protein",1},{"prep_pack",.5}});
  }else{
   MainsSold++;
   t.Grill+=Range(60,120,240,7);t.Assembly+=Range(30,60,120,8);t.Expo+=Range(10,25,60,9);
   Sell(oid,"grilled_main","grill",new Dictionary<string,double>{{"main_protein",1},{"prep_pack",.5}});
  }
  if(hasFries){FriesSold++;t.Fryer+=Range(120,180,210,10);t.Assembly+=Range(8,15,30,11);Sell(oid,"side","fryer",new Dictionary<string,double>{{"side_base",1}});}
  if(hasDrink){DrinksSold++;t.Beverage+=Range(10,25,45,12);Sell(oid,"beverage","beverage",new Dictionary<string,double>{{"drink_mix",.25}});}
  Prep=Math.Max(0,Prep-(1+(hasFries?1:0)));
  activeTickets.Add(t);
  Emit("ticket.updated",$"{{\"ticket_id\":\"tkt_{t.Id:000000}\",\"order_id\":\"{oid}\",\"status\":\"queued\",\"queue_seconds\":0,\"station_id\":\"assembly\"}}");
 }

 void Sell(string oid,string item,string station,Dictionary<string,double> draw){
  foreach(var kv in draw)inventory[kv.Key]=Math.Max(0,inventory.GetValueOrDefault(kv.Key)-kv.Value);
  Emit("item.sold",$"{{\"order_id\":\"{oid}\",\"item_id\":\"{item}\",\"quantity\":1,\"station_ids\":[\"{station}\"],\"inventory_draw\":{DrawJson(draw)}}}");
 }

 void ProcessTickets(double sm){
  ProcessGrill(GrillCapacity*sm);ProcessFryer(FryerCapacity*sm);ProcessBeverage(BeverageCapacity*sm);ProcessAssembly(AssemblyCapacity*sm);ProcessExpo(ExpoCapacity*sm);
  for(var i=activeTickets.Count-1;i>=0;i--){
   var t=activeTickets[i];if(!t.Done)continue;
   CompletedTickets++;
   var sec=(int)((Minute-t.Created+1440)%1440*60);
   Emit("ticket.updated",$"{{\"ticket_id\":\"tkt_{t.Id:000000}\",\"order_id\":\"ord_{t.Id:000000}\",\"status\":\"completed\",\"queue_seconds\":{sec},\"station_id\":\"assembly\"}}");
   activeTickets.RemoveAt(i);
  }
 }
 void ProcessGrill(double c){for(var i=0;i<activeTickets.Count&&c>0;i++){var t=activeTickets[i];if(t.Grill<=0)continue;var x=Math.Min(t.Grill,c);t.Grill-=x;c-=x;}}
 void ProcessFryer(double c){for(var i=0;i<activeTickets.Count&&c>0;i++){var t=activeTickets[i];if(t.Fryer<=0)continue;var x=Math.Min(t.Fryer,c);t.Fryer-=x;c-=x;}}
 void ProcessBeverage(double c){for(var i=0;i<activeTickets.Count&&c>0;i++){var t=activeTickets[i];if(t.Beverage<=0)continue;var x=Math.Min(t.Beverage,c);t.Beverage-=x;c-=x;}}
 void ProcessAssembly(double c){for(var i=0;i<activeTickets.Count&&c>0;i++){var t=activeTickets[i];if(t.Grill>0||t.Fryer>0||t.Assembly<=0)continue;var x=Math.Min(t.Assembly,c);t.Assembly-=x;c-=x;}}
 void ProcessExpo(double c){for(var i=0;i<activeTickets.Count&&c>0;i++){var t=activeTickets[i];if(t.Grill>0||t.Fryer>0||t.Assembly>0||t.Beverage>0||t.Expo<=0)continue;var x=Math.Min(t.Expo,c);t.Expo-=x;c-=x;}}

 public void AddKitchenCoverage(){AddCov(ref KitchenCoverage,"grill");}public void RemoveKitchenCoverage(){RemoveCov(ref KitchenCoverage,"grill");}
 public void AddFryerCoverage(){AddCov(ref FryerCoverage,"fryer");}public void RemoveFryerCoverage(){RemoveCov(ref FryerCoverage,"fryer");}
 public void AddDriveCoverage(){AddCov(ref DriveCoverage,"drive_thru");}public void RemoveDriveCoverage(){RemoveCov(ref DriveCoverage,"drive_thru");}
 public void AddCounterCoverage(){AddCov(ref CounterCoverage,"lobby");}public void RemoveCounterCoverage(){RemoveCov(ref CounterCoverage,"lobby");}
 public void AddPrepCoverage(){AddCov(ref PrepCoverage,"prep");}public void RemovePrepCoverage(){RemoveCov(ref PrepCoverage,"prep");}
 void AddCov(ref int v,string station){if(CoverageOpen<=0)return;v++;RecordStaffing("coverage_unit","coverage_unit",null,station,"manager_adjustment");}
 void RemoveCov(ref int v,string station){if(v<=0)return;v--;RecordStaffing("coverage_unit","coverage_unit",station,null,"manager_adjustment");}
 void ClampCoverage(){while(CoverageUsed>CoveragePool){if(PrepCoverage>0)PrepCoverage--;else if(CounterCoverage>0)CounterCoverage--;else if(DriveCoverage>0)DriveCoverage--;else if(KitchenCoverage>0)KitchenCoverage--;else if(FryerCoverage>0)FryerCoverage--;else break;}}

 public void ManualPrep(){if(PrepCoverage>0)DoPrep("manager_adjustment");}
 public void ManualDiscard(){if(Prep<=0)return;var w=Prep;Prep=0;PrepAge=0;RecordWaste("quality_discard",w,"assembly");}
 public void ChangeSanitizer(){SanitizerAge=0;SanitationTasks++;}
 public void CheckTemps(){TempCheckAge=0;UpdateTemperatures();}
 public void AddCrew(){Crew++;RecordStaffing("crew_member","crew_shift_pool",null,"grill","manager_adjustment");}
 public void CutCrew(){if(Crew<=0)return;Crew--;ClampCoverage();RecordStaffing("crew_member","crew_shift_pool","grill",null,"manager_adjustment");}
 public void AddLead(){Lead++;RecordStaffing("team_leader","crew_shift_lead",null,"floor","manager_adjustment");}
 public void CutLead(){if(Lead<=0)return;Lead--;ClampCoverage();RecordStaffing("team_leader","crew_shift_lead","floor",null,"manager_adjustment");}
 public void AddShiftMgr(){ShiftMgr++;RecordStaffing("shift_manager","manager_shift",null,"floor","manager_adjustment");}
 public void CutShiftMgr(){if(ShiftMgr<=0)return;ShiftMgr--;ClampCoverage();RecordStaffing("shift_manager","manager_shift","floor",null,"manager_adjustment");}
 public void AddAsstMgr(){AsstMgr++;RecordStaffing("assistant_manager","manager_assistant",null,"floor","manager_adjustment");}
 public void CutAsstMgr(){if(AsstMgr<=0)return;AsstMgr--;ClampCoverage();RecordStaffing("assistant_manager","manager_assistant","floor",null,"manager_adjustment");}
 public void AddRestMgr(){RestMgr++;RecordStaffing("restaurant_manager","manager_restaurant",null,"floor","manager_adjustment");}
 public void CutRestMgr(){if(RestMgr<=0)return;RestMgr--;ClampCoverage();RecordStaffing("restaurant_manager","manager_restaurant","floor",null,"manager_adjustment");}
 public void StartBreak(){if(CrewOnBreak>=Crew)return;CrewOnBreak++;BreakTimer=0;ClampCoverage();RecordStaffing("crew_member","crew_shift_break","grill",null,"break_coverage");}
 public void EndBreak(){if(CrewOnBreak<=0)return;CrewOnBreak--;BreaksTaken++;BreakTimer=0;RecordStaffing("crew_member","crew_shift_break",null,"grill","break_coverage");}
 public void CallOff(){if(Crew<=0)return;Crew--;CallOffs++;if(CrewOnBreak>Crew)CrewOnBreak=Crew;ClampCoverage();RecordStaffing("cook","crew_shift_calloff","fryer",null,"call_off");}

 void RecordStaffing(string role,string worker,string? from,string? to,string reason){
  StaffingSeq++;
  var src=$"evt_{EventSeq+1:000000}";
  StaffingLedger=$"{StaffingSeq} {TimeText} {role} {worker} {from ?? "none"}->{to ?? "none"} eff {EffectiveCrew} cap {StaffCapacity} coverage {CoverageUsed}/{CoveragePool} reason {reason} src {src}\n"+StaffingLedger;
  if(StaffingLedger.Length>800)StaffingLedger=StaffingLedger[..800];
  Emit("staff.assignment.updated",$"{{\"assignment_id\":\"asg_{StaffingSeq:000000}\",\"synthetic_worker_ref\":\"{worker}\",\"role_id\":\"{role}\",\"from_station_id\":{JsonStringOrNull(from)},\"to_station_id\":{JsonStringOrNull(to)},\"reason\":\"{StaffReason(reason)}\"}}");
 }
 void DoPrep(string reason){
  var n=Math.Min(Raw,Math.Max(40,PrepCoverage*80));if(n<=0)return;
  Raw-=n;Prep+=n;PrepAge=0;inventory["prep_pack"]+=n;
  Emit("prep.confirmed",$"{{\"prep_batch_id\":\"prep_{EventSeq+1:000000}\",\"inventory_item_id\":\"prep_pack\",\"quantity\":{Num(n)},\"unit\":\"units\",\"station_id\":\"prep\",\"confirmed_by_role\":\"cook\"}}");
 }
 void ExpirePrep(){var w=Math.Max(1,Prep/4);Prep-=w;PrepAge=0;RecordWaste("holding_time_exceeded",w,"assembly");}
 void RecordWaste(string r,int u,string station){
  Waste+=u;WasteSeq++;inventory["prep_pack"]=Math.Max(0,inventory["prep_pack"]-u);
  WasteLedger=$"{WasteSeq} {TimeText} {r} {u}u ${u*0.75:0.00}\n"+WasteLedger;
  if(WasteLedger.Length>500)WasteLedger=WasteLedger[..500];
  Emit("waste.recorded",$"{{\"waste_id\":\"waste_{WasteSeq:000000}\",\"inventory_item_id\":\"prep_pack\",\"quantity\":{Num(u)},\"unit\":\"units\",\"reason\":\"{r}\",\"station_id\":\"{station}\"}}");
 }

 string Channel(){
  var x=Roll(20);
  return x<600?"drive_thru":x<760?"lobby":x<860?"delivery":"mobile";
 }
 int Roll(int salt){var v=(long)Seed*1103515245L+(long)(Orders+1)*12345L+(long)salt*9973L+(long)((int)Minute)*31L;return (int)(Math.Abs(v)%1000);}
 double Range(double min,double target,double max,int salt){var r=Roll(salt)/999.0;return r<.5?min+(target-min)*(r*2):target+(max-target)*((r-.5)*2);}
 double CheckAmount(int items)=>10.0+(Roll(30)%201)/100.0+Math.Max(0,items-2)*1.25;
 int ExpectedTicketSeconds(string channel,int items)=>150+items*35+(channel=="delivery"?160:channel=="mobile"?45:channel=="lobby"?25:0);

 void UpdateOverload(double m){
  var was=StationOverloaded;
  if(DelayRisk){over+=m;recover=0;if(over>=5)StationOverloaded=true;}else{over=0;if(StationOverloaded){recover+=m;if(recover>=4)StationOverloaded=false;}}
  if(!was&&StationOverloaded)Emit("station.overloaded",StationPayload(true));
  if(was&&!StationOverloaded)Emit("station.recovered",StationPayload(false));
 }
 void UpdateTemperatures(){CoolerTemp=38+(Scenario=="equipment_failure"?4:0)+Math.Min(3,PrepAge/20);HotHoldTemp=145-(Scenario=="equipment_failure"?9:0)-(StationOverloaded?3:0)-Math.Min(6,PrepAge/10);}
 string StationPayload(bool overloaded){
  if(overloaded)return $"{{\"station_id\":\"assembly\",\"load_units\":{Num(KitchenLoad)},\"capacity_units\":{Num(StaffCapacity)},\"duration_minutes\":5,\"primary_cause\":\"{OverloadCause}\"}}";
  return $"{{\"station_id\":\"assembly\",\"load_units\":{Num(KitchenLoad)},\"capacity_units\":{Num(StaffCapacity)},\"recovery_duration_minutes\":4,\"recovery_reason\":\"queue_cleared\"}}";
 }
 void Emit(string t,string p){
  var e=new SimEvent(++EventSeq,TimeText,t,Scenario,Seed,Daypart,p);
  AllJsonl+=e.Jsonl+"\n";
  RecentEvents=e.Text+"\n"+RecentEvents;
  RecentJsonl=e.Jsonl+"\n"+RecentJsonl;
  if(RecentEvents.Length>500)RecentEvents=RecentEvents[..500];
  if(RecentJsonl.Length>1000)RecentJsonl=RecentJsonl[..1000];
 }

 double RatePerSimMinute(){if(Minute<360)return 0;var daily=Scenario=="slow_day"?620:Scenario=="rush_day"?1180:Scenario=="weather_disruption"?760:Scenario=="local_event_surge"?1050:Scenario=="school_event_surge"?980:Scenario=="holiday_pattern"?840:Scenario=="multi_rush_condition"?1240:920;return daily*DaypartShare()/DaypartMinutes()*Curve()*ScenarioMultiplier();}
 double ScenarioMultiplier()=>Scenario=="rush_day"?1.10:Scenario=="weather_disruption"?.86:Scenario=="equipment_failure"?.95:Scenario=="staffing_call_off"?.97:Scenario=="multi_rush_condition"?1.18:1.0;
 double DaypartShare()=>Daypart=="breakfast"?.18:Daypart=="mid_morning"?.07:Daypart=="lunch"?.30:Daypart=="afternoon"?.10:Daypart=="dinner"?.30:.05;
 double DaypartMinutes()=>Daypart=="breakfast"?240:Daypart=="mid_morning"?90:Daypart=="lunch"?150:Daypart=="afternoon"?150:Daypart=="dinner"?240:210;
 double Curve(){var peak=Daypart=="breakfast"?480:Daypart=="mid_morning"?615:Daypart=="lunch"?750:Daypart=="afternoon"?930:Daypart=="dinner"?1095:1260;var start=Daypart=="breakfast"?360:Daypart=="mid_morning"?600:Daypart=="lunch"?690:Daypart=="afternoon"?840:Daypart=="dinner"?990:1230;var end=Daypart=="breakfast"?600:Daypart=="mid_morning"?690:Daypart=="lunch"?840:Daypart=="afternoon"?990:Daypart=="dinner"?1230:1440;var half=Math.Max(1,Math.Max(peak-start,end-peak));var shape=1-Math.Min(1,Math.Abs(Minute-peak)/half);return .75+.5*shape;}
 double Sum(Func<Ticket,double> f){double n=0;foreach(var t in activeTickets)n+=Math.Max(0,f(t));return n;}
 int CountActive(string ch){var n=0;foreach(var t in activeTickets)if(t.Channel==ch)n++;return n;}
 double StaffScenarioMultiplier=>Scenario=="staffing_call_off"?.74:Scenario=="multi_rush_condition"?.78:Scenario=="holiday_pattern"?.92:1.0;
 double EquipmentMultiplier(string station)=>Scenario=="equipment_failure"&&station=="fryer"?.55:Scenario=="multi_rush_condition"&&station=="fryer"?.70:Scenario=="multi_rush_condition"&&station=="beverage"?.82:1.0;
 double GrillCapacity=>KitchenCoverage*90*StaffScenarioMultiplier*EquipmentMultiplier("grill");
 double FryerCapacity=>FryerCoverage*125*StaffScenarioMultiplier*EquipmentMultiplier("fryer");
 double AssemblyCapacity=>(KitchenCoverage*70+CounterCoverage*15)*StaffScenarioMultiplier*EquipmentMultiplier("assembly");
 double BeverageCapacity=>(CounterCoverage*50+DriveCoverage*55)*StaffScenarioMultiplier*EquipmentMultiplier("beverage");
 double ExpoCapacity=>(DriveCoverage*70+CounterCoverage*45+KitchenCoverage*15)*StaffScenarioMultiplier;

 public double Sales=>Orders*11.0;public double WasteCost=>Waste*0.75;public double FoodCostPercent=>Sales<=0?0:WasteCost/Sales*100;
 public double LaborHourly=>Crew*16+Lead*18+ShiftMgr*22+AsstMgr*28+RestMgr*35;public double LaborPercent=>Sales<=0?0:LaborCost/Sales*100;
 public int TotalOnClock=>Crew+Lead+ShiftMgr+AsstMgr+RestMgr;public int EffectiveCrew=>Math.Max(0,Crew-CrewOnBreak);public int CoveragePool=>Math.Max(0,TotalOnClock-CrewOnBreak);public int CoverageUsed=>KitchenCoverage+FryerCoverage+DriveCoverage+CounterCoverage+PrepCoverage;public int CoverageOpen=>Math.Max(0,CoveragePool-CoverageUsed);public int AssignableLabor=>CoveragePool;public int StationAssigned=>CoverageUsed;public int AvailableLabor=>CoverageOpen;
 public int PaidHeadcount=>TotalOnClock;public double AverageLaborRate=>PaidHeadcount<=0?16.0:LaborHourly/PaidHeadcount;public double LaborTargetPercent=>ProjectedSalesThis30<150?0.35:ProjectedSalesThis30<300?0.32:ProjectedSalesThis30<600?0.30:0.28;public double HalfHourElapsedMinutes=>Math.Max(1.0,Minute%30.0);public double HalfHourProgress=>Math.Min(1.0,HalfHourElapsedMinutes/30.0);public double ProjectedSalesThis30=>SalesThis30/HalfHourProgress;public double ProjectedLaborCostThis30=>LaborHourly*0.5;public double ProjectedLaborPercentThis30=>ProjectedSalesThis30<=0?0:ProjectedLaborCostThis30/ProjectedSalesThis30*100.0;public double AllowedLaborDollarsThis30=>ProjectedSalesThis30*LaborTargetPercent;public double AllowedLaborHoursThis30=>AverageLaborRate<=0?0:AllowedLaborDollarsThis30/AverageLaborRate;public double ScheduledLaborHoursThis30=>PaidHeadcount*0.5;public double LaborHoursVarianceThis30=>AllowedLaborHoursThis30-ScheduledLaborHoursThis30;
 public bool BreakDue=>ShiftMinutes>240&&BreaksTaken==0;public int StaffCapacity=>(int)(GrillCapacity+FryerCapacity+AssemblyCapacity+BeverageCapacity+ExpoCapacity);public int NetKitchenLoad=>KitchenLoad-StaffCapacity;public double KitchenBacklogMinutes=>StaffCapacity<=0?(Tickets>0?999:0):KitchenLoad/(double)StaffCapacity;public double FryerBacklogMinutes=>FryerCapacity<=0?(FryerLoad>0?999:0):FryerLoad/FryerCapacity;public double GrillBacklogMinutes=>GrillCapacity<=0?(GrillLoad>0?999:0):GrillLoad/GrillCapacity;public double AssemblyBacklogMinutes=>AssemblyCapacity<=0?(AssemblyLoad>0?999:0):AssemblyLoad/AssemblyCapacity;public double ExpoBacklogMinutes=>(ExpoCapacity+BeverageCapacity)<=0?(ExpoLoad>0?999:0):ExpoLoad/(ExpoCapacity+BeverageCapacity);
 public int PrepQuality=>PrepAge>=30?0:100-(int)(PrepAge*3);public int Tickets=>activeTickets.Count;public int FryerLoad=>(int)Math.Ceiling(Sum(t=>t.Fryer));public int GrillLoad=>(int)Math.Ceiling(Sum(t=>t.Grill));public int AssemblyLoad=>(int)Math.Ceiling(Sum(t=>t.Assembly));public int ExpoLoad=>(int)Math.Ceiling(Sum(t=>t.Expo)+Sum(t=>t.Beverage));public int KitchenLoad=>FryerLoad+GrillLoad+AssemblyLoad+ExpoLoad;public bool DelayRisk=>Tickets>30||(StaffCapacity<=0&&Tickets>0)||KitchenBacklogMinutes>10||FryerBacklogMinutes>8||GrillBacklogMinutes>8||AssemblyBacklogMinutes>7||ExpoBacklogMinutes>7;
 public bool SanitizerDue=>SanitizerAge>=120;public bool TempCheckDue=>TempCheckAge>=120;public bool TempOutOfRange=>CoolerTemp>41||HotHoldTemp<135;public string AlertText=>StationOverloaded?"ALERT: station overloaded":TempOutOfRange?"ALERT: temperature out of range":TempCheckDue?"Warning: temperature check due":SanitizerDue?"Warning: sanitizer change due":BreakDue?"Warning: break due":DelayRisk?"Warning: station delay risk":PrepQuality<50?"Warning: prep quality low":Prep<80?"Warning: prep low":"Alerts: none";public int DtSos=>(int)Math.Min(900,258+CountActive("drive_thru")*18+(DriveCoverage<=0?120:0)+(StationOverloaded?90:0));public int FcSos=>(int)Math.Min(720,180+CountActive("lobby")*20+(CounterCoverage<=0?120:0)+(StationOverloaded?75:0));public int DelSos=>(int)Math.Min(2100,420+CountActive("delivery")*35+(StationOverloaded?180:0));public int TicketsThis30=>tickets30[(int)(Minute/30)%48];public int TicketsThis60=>tickets30[(int)(Minute/30)%48]+tickets30[((int)(Minute/30)+47)%48];public double SalesThis30=>sales30[(int)(Minute/30)%48];public double SalesThis60=>sales30[(int)(Minute/30)%48]+sales30[((int)(Minute/30)+47)%48];public string Daypart=>Minute<360?"late_night":Minute<600?"breakfast":Minute<690?"mid_morning":Minute<840?"lunch":Minute<990?"afternoon":Minute<1230?"dinner":"late_night";public string TimeText=>$"{(int)(Minute/60):00}:{(int)(Minute%60):00}";

 string CustomerSegment=>Daypart=="breakfast"?"commuter_breakfast":Daypart=="dinner"?"family_dinner":Daypart=="late_night"?"late_night_guest":"general_guest";
 string OverloadCause=>Scenario=="equipment_failure"?"equipment_constraint":Scenario=="staffing_call_off"?"staffing_gap":Scenario=="multi_rush_condition"?"multi_rush":Scenario=="rush_day"||Scenario=="local_event_surge"||Scenario=="school_event_surge"?"rush_demand":"menu_mix";
 string StaffReason(string reason)=>reason=="call_off"?"call_off":reason=="break_coverage"?"break_coverage":reason=="manager_adjustment"?"manager_adjustment":"rush_support";
 string JsonStringOrNull(string? v)=>v==null?"null":$"\"{v}\"";
 string Num(double n)=>n.ToString("0.##",CultureInfo.InvariantCulture);
 string DrawJson(Dictionary<string,double> draw){var parts=new List<string>();foreach(var kv in draw)parts.Add($"\"{kv.Key}\":{Num(kv.Value)}");return "{"+string.Join(",",parts)+"}";}
}
