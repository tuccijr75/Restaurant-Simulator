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
 class EquipmentUnit{
  public string Id="",Station="",Family="";
  public double BaseCapacity,Load,Assigned,Processed;
  public int Tasks;
 }
 class ItemSpec{
  public string Id="",Family="",Station="";
  public double CookSeconds,HoldMinutes,AssemblySeconds,ExpoSeconds,Price;
 }

 readonly List<Ticket> activeTickets=new();
 readonly List<EquipmentUnit> equipment=new();
 readonly Dictionary<string,ItemSpec> items=new();
 readonly int[] tickets30=new int[48];
 readonly double[] sales30=new double[48];
 readonly Dictionary<string,double> inventory=new(){["main_protein"]=520,["side_base"]=420,["drink_mix"]=300,["prep_pack"]=260};
 double acc,over,recover,nextTraceMinute=360,lastOverloadMinutes;
 bool equipmentReady;
 string activeOverloadStation="assembly",lastValidationKey="";

 public int Seed=12345,Orders,DriveThru,FrontCounter,Delivery,Mobile,EventSeq,WasteSeq,StaffingSeq,TraceSeq,ValidationSeq,OverloadSeq,ItemSeq,Raw=500,Prep=120,Waste,Crew=6,Lead=1,ShiftMgr=1,AsstMgr=0,RestMgr=0,CrewOnBreak,BreaksTaken,CallOffs,SanitationTasks,CompletedTickets,FriesSold,DrinksSold,MainsSold;
 public int KitchenCoverage=1,FryerCoverage=1,DriveCoverage=2,CounterCoverage=1,PrepCoverage=1;
 public double Minute=360,TimeScale=1.0,PrepAge,LaborCost,ShiftMinutes,BreakTimer,SanitizerAge,TempCheckAge,CoolerTemp=38,HotHoldTemp=145,SalesTotal;
 public string Scenario="normal_day",RecentEvents="",RecentJsonl="",AllJsonl="",WasteLedger="",StaffingLedger="",TraceLedger="",ValidationLedger="",OverloadLedger="",EquipmentLedger="",ItemLedger="",ValidationStatus="OK";
 public bool Running,StationOverloaded,ShiftStarted,ShiftEnded;

 public void Step(double d){
  EnsureEquipment();
  if(!Running)return;
  EnsureShiftStarted();
  if(ShiftEnded)return;
  ClampCoverage();
  var sm=Math.Max(0,d)*TimeScale/60.0;
  var finalStep=false;
  if(Minute+sm>=1439){sm=Math.Max(0,1439-Minute);finalStep=true;}
  Minute+=sm;
  ShiftMinutes+=sm;
  LaborCost+=LaborHourly*sm/60;
  if(CrewOnBreak>0)BreakTimer+=sm;
  if(Prep>0)PrepAge+=sm;
  SanitizerAge+=sm;
  TempCheckAge+=sm;
  UpdateTemperatures();
  ProcessEquipment(sm);
  ProcessTickets(sm);
  acc+=RatePerSimMinute()*sm;
  while(acc>=1){AddOrder();acc-=1;}
  if(PrepAge>=30)ExpirePrep();
  if(Prep<80&&PrepCoverage>0)DoPrep("auto_threshold");
  UpdateOverload(sm);
  if(Minute>=nextTraceMinute||finalStep){Trace("interval");nextTraceMinute=Math.Floor(Minute/15.0)*15.0+15.0;}
  RefreshValidation("step");
  if(finalStep)EndShift();
 }

 void AddOrder(){
  var ch=Channel();
  Orders++;
  if(ch=="drive_thru")DriveThru++;else if(ch=="lobby")FrontCounter++;else if(ch=="delivery")Delivery++;else Mobile++;
  var oid=$"ord_{Orders:000000}";
  var mainFried=Roll(1)<500;
  var hasFries=Roll(2)<650;
  var hasDrink=Roll(3)<600;
  var count=1+(hasFries?1:0)+(hasDrink?1:0);
  var check=CheckAmount(count);
  SalesTotal+=check;
  var b=(int)(Minute/30)%48;
  tickets30[b]++;
  sales30[b]+=check;
  Emit("order.created",$"{{\"order_id\":\"{oid}\",\"customer_segment\":\"{CustomerSegment}\",\"channel\":\"{ch}\",\"estimated_items\":{count},\"expected_ticket_seconds\":{ExpectedTicketSeconds(ch,count)}}}");
  var t=new Ticket{Id=Orders,Channel=ch,Created=Minute};
  var main=mainFried?items["fried_main"]:items["grilled_main"];
  MainsSold++;
  QueueItem(t,oid,main,new Dictionary<string,double>{{"main_protein",1},{"prep_pack",.5}});
  if(hasFries){FriesSold++;QueueItem(t,oid,items["side"],new Dictionary<string,double>{{"side_base",1}});}
  if(hasDrink){DrinksSold++;QueueItem(t,oid,items["beverage"],new Dictionary<string,double>{{"drink_mix",.25}});}
  Prep=Math.Max(0,Prep-(1+(hasFries?1:0)));
  activeTickets.Add(t);
  Emit("ticket.updated",$"{{\"ticket_id\":\"tkt_{t.Id:000000}\",\"order_id\":\"{oid}\",\"status\":\"queued\",\"queue_seconds\":0,\"station_id\":\"assembly\"}}");
 }

 void QueueItem(Ticket t,string oid,ItemSpec spec,Dictionary<string,double> draw){
  var primary=AddEquipmentTask(spec,oid,spec.Family,spec.CookSeconds,"cook");
  if(spec.Station=="fryer")t.Fryer+=spec.CookSeconds;else if(spec.Station=="grill")t.Grill+=spec.CookSeconds;else if(spec.Station=="beverage")t.Beverage+=spec.CookSeconds;
  if(spec.AssemblySeconds>0){t.Assembly+=spec.AssemblySeconds;AddEquipmentTask(spec,oid,"assembly",spec.AssemblySeconds,"assembly");}
  if(spec.ExpoSeconds>0){t.Expo+=spec.ExpoSeconds;AddEquipmentTask(spec,oid,"expo",spec.ExpoSeconds,"expo");}
  Sell(oid,spec,primary,draw);
 }

 string AddEquipmentTask(ItemSpec spec,string oid,string family,double seconds,string phase){
  var unit=SelectEquipment(family);
  if(unit==null)return "unassigned";
  unit.Load+=seconds;
  unit.Assigned+=seconds;
  unit.Tasks++;
  EquipmentLedger+=$"{TimeText} assign order {oid} item {spec.Id} phase {phase} equipment {unit.Id} family {family} cook {Num(seconds)}s hold {Num(spec.HoldMinutes)}m load {Num(unit.Load)} cap_min {Num(EquipmentCapacity(unit))}\n";
  return unit.Id;
 }

 EquipmentUnit? SelectEquipment(string family){
  EnsureEquipment();
  EquipmentUnit? best=null;
  var bestRatio=double.MaxValue;
  foreach(var e in equipment){
   if(e.Family!=family||!EquipmentAvailable(e))continue;
   var cap=Math.Max(1,EquipmentCapacity(e));
   var ratio=e.Load/cap;
   if(ratio<bestRatio){best=e;bestRatio=ratio;}
  }
  if(best!=null)return best;
  foreach(var e in equipment){if(e.Family==family){best=e;break;}}
  return best;
 }

 void Sell(string oid,ItemSpec spec,string equipmentId,Dictionary<string,double> draw){
  foreach(var kv in draw)inventory[kv.Key]=Math.Max(0,inventory.GetValueOrDefault(kv.Key)-kv.Value);
  ItemSeq++;
  ItemLedger+=$"{ItemSeq} {TimeText} order {oid} item {spec.Id} equipment {equipmentId} station {spec.Station} cook {Num(spec.CookSeconds)}s hold {Num(spec.HoldMinutes)}m assembly {Num(spec.AssemblySeconds)}s expo {Num(spec.ExpoSeconds)}s price ${spec.Price:0.00}\n";
  Emit("item.sold",$"{{\"order_id\":\"{oid}\",\"item_id\":\"{spec.Id}\",\"quantity\":1,\"station_ids\":[\"{equipmentId}\"],\"inventory_draw\":{DrawJson(draw)}}}");
 }

 void ProcessEquipment(double sm){
  EnsureEquipment();
  foreach(var e in equipment){
   var cap=EquipmentCapacity(e)*sm;
   var done=Math.Min(e.Load,Math.Max(0,cap));
   e.Load-=done;
   e.Processed+=done;
  }
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
 public void ChangeSanitizer(){SanitizerAge=0;SanitationTasks++;Trace("sanitizer_changed");}
 public void CheckTemps(){TempCheckAge=0;UpdateTemperatures();Trace("temperature_checked");}
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
  EnsureShiftStarted();
  StaffingSeq++;
  var src=$"evt_{EventSeq+1:000000}";
  StaffingLedger+=$"{StaffingSeq} {TimeText} {role} {worker} {from ?? "none"}->{to ?? "none"} eff {EffectiveCrew} cap {StaffCapacity} coverage {CoverageUsed}/{CoveragePool} labor_pct {Num(LaborPercent)} projected_labor_pct {Num(ProjectedLaborPercentThis30)} allowed_hrs {Num(AllowedLaborHoursThis30)} variance_hrs {Num(LaborHoursVarianceThis30)} reason {reason} src {src}\n";
  Emit("staff.assignment.updated",$"{{\"assignment_id\":\"asg_{StaffingSeq:000000}\",\"synthetic_worker_ref\":\"{worker}\",\"role_id\":\"{role}\",\"from_station_id\":{JsonStringOrNull(from)},\"to_station_id\":{JsonStringOrNull(to)},\"reason\":\"{StaffReason(reason)}\"}}");
 }
 void DoPrep(string reason){
  var n=Math.Min(Raw,Math.Max(40,PrepCoverage*80));if(n<=0)return;
  Raw-=n;Prep+=n;PrepAge=0;inventory["prep_pack"]+=n;
  Trace($"prep_{reason}");
  Emit("prep.confirmed",$"{{\"prep_batch_id\":\"prep_{EventSeq+1:000000}\",\"inventory_item_id\":\"prep_pack\",\"quantity\":{Num(n)},\"unit\":\"units\",\"station_id\":\"prep\",\"confirmed_by_role\":\"cook\"}}");
 }
 void ExpirePrep(){var w=Math.Max(1,Prep/4);Prep-=w;PrepAge=0;RecordWaste("holding_time_exceeded",w,"assembly");}
 void RecordWaste(string r,int u,string station){
  Waste+=u;WasteSeq++;inventory["prep_pack"]=Math.Max(0,inventory["prep_pack"]-u);
  WasteLedger+=$"{WasteSeq} {TimeText} {r} {u}u ${u*0.75:0.00} prep {Prep} raw {Raw} inv_prep {Num(inventory["prep_pack"])} station {station}\n";
  Emit("waste.recorded",$"{{\"waste_id\":\"waste_{WasteSeq:000000}\",\"inventory_item_id\":\"prep_pack\",\"quantity\":{Num(u)},\"unit\":\"units\",\"reason\":\"{r}\",\"station_id\":\"{station}\"}}");
 }

 void UpdateOverload(double m){
  var was=StationOverloaded;
  var station=BottleneckStation;
  if(DelayRisk){
   over+=m;recover=0;lastOverloadMinutes=over;
   if(over>=5){StationOverloaded=true;if(!was){activeOverloadStation=station;OverloadSeq++;RecordOverload("started");}}
  }else{
   over=0;
   if(StationOverloaded){recover+=m;if(recover>=4){StationOverloaded=false;RecordOverload("recovered");}}
  }
  if(!was&&StationOverloaded)Emit("station.overloaded",StationPayload(true));
  if(was&&!StationOverloaded)Emit("station.recovered",StationPayload(false));
 }
 void UpdateTemperatures(){CoolerTemp=38+(Scenario=="equipment_failure"?4:0)+Math.Min(3,PrepAge/20);HotHoldTemp=145-(Scenario=="equipment_failure"?9:0)-(StationOverloaded?3:0)-Math.Min(6,PrepAge/10);}
 string StationPayload(bool overloaded){
  var station=overloaded?activeOverloadStation:activeOverloadStation;
  if(overloaded)return $"{{\"station_id\":\"{station}\",\"load_units\":{Num(StationLoad(station))},\"capacity_units\":{Num(StationCapacity(station))},\"duration_minutes\":{Num(Math.Max(5,lastOverloadMinutes))},\"primary_cause\":\"{OverloadCause}\"}}";
  return $"{{\"station_id\":\"{station}\",\"load_units\":{Num(StationLoad(station))},\"capacity_units\":{Num(StationCapacity(station))},\"recovery_duration_minutes\":{Num(recover)},\"recovery_reason\":\"queue_cleared\"}}";
 }
 void RecordOverload(string state){
  var station=activeOverloadStation;
  OverloadLedger+=$"{OverloadSeq} {TimeText} {state} station {station} equipment {BottleneckEquipment} cause {OverloadCause} load {Num(StationLoad(station))} cap {Num(StationCapacity(station))} backlog {Num(StationBacklog(station))}m tickets {Tickets} fryer {FryerLoad}/{Num(FryerCapacity)} grill {GrillLoad}/{Num(GrillCapacity)} assembly {AssemblyLoad}/{Num(AssemblyCapacity)} expo {ExpoLoad}/{Num(ExpoCapacity+BeverageCapacity)} coverage {CoverageUsed}/{CoveragePool} labor_pct {Num(LaborPercent)} equipment_summary {EquipmentSummary}\n";
  Trace($"overload_{state}");
 }

 void EnsureShiftStarted(){
  if(ShiftStarted)return;
  EnsureEquipment();
  ShiftStarted=true;
  Emit("shift.started",$"{{\"shift_id\":\"shift_{Seed}\",\"manager_role_ref\":\"manager_on_duty\",\"opening_inventory_snapshot_id\":\"inv_open_{Seed}\",\"scheduled_role_count\":{TotalOnClock}}}");
 }
 void EndShift(){
  if(ShiftEnded)return;
  ShiftEnded=true;
  RefreshValidation("shift_end");
  Emit("shift.ended",$"{{\"shift_id\":\"shift_{Seed}\",\"closing_inventory_snapshot_id\":\"inv_close_{Seed}\",\"orders_total\":{Orders},\"waste_events_total\":{WasteSeq},\"overload_events_total\":{OverloadSeq}}}");
  Running=false;
 }
 void Emit(string t,string p){
  if(!ShiftStarted&&t!="shift.started")EnsureShiftStarted();
  if(ShiftEnded&&t!="shift.ended")return;
  var e=new SimEvent(++EventSeq,TimeText,t,Scenario,Seed,Daypart,p);
  AllJsonl+=e.Jsonl+"\n";
  RecentEvents=e.Text+"\n"+RecentEvents;
  RecentJsonl=e.Jsonl+"\n"+RecentJsonl;
  if(RecentEvents.Length>500)RecentEvents=RecentEvents[..500];
  if(RecentJsonl.Length>1000)RecentJsonl=RecentJsonl[..1000];
  Trace("event:"+t);
 }

 void Trace(string reason){
  EnsureEquipment();
  TraceSeq++;
  TraceLedger+=$"{{\"trace_id\":\"trace_{TraceSeq:000000}\",\"time\":\"{TimeText}\",\"minute\":{Num(Minute)},\"reason\":\"{Json(reason)}\",\"event_seq\":{EventSeq},\"scenario\":\"{Scenario}\",\"daypart\":\"{Daypart}\",\"speed\":{Num(TimeScale)},\"orders\":{Orders},\"completed\":{CompletedTickets},\"active_tickets\":{Tickets},\"channels\":{{\"drive_thru\":{DriveThru},\"lobby\":{FrontCounter},\"delivery\":{Delivery},\"mobile\":{Mobile}}},\"sales_total\":{Num(Sales)},\"labor_cost\":{Num(LaborCost)},\"labor_percent\":{Num(LaborPercent)},\"projected_sales_30\":{Num(ProjectedSalesThis30)},\"projected_labor_percent_30\":{Num(ProjectedLaborPercentThis30)},\"labor_target_percent\":{Num(LaborTargetPercent*100)},\"allowed_labor_dollars_30\":{Num(AllowedLaborDollarsThis30)},\"labor_dollars_variance_30\":{Num(LaborDollarsVarianceThis30)},\"allowed_labor_hours_30\":{Num(AllowedLaborHoursThis30)},\"scheduled_labor_hours_30\":{Num(ScheduledLaborHoursThis30)},\"labor_hours_variance_30\":{Num(LaborHoursVarianceThis30)},\"crew\":{Crew},\"effective_crew\":{EffectiveCrew},\"coverage_used\":{CoverageUsed},\"coverage_pool\":{CoveragePool},\"bottleneck_station\":\"{BottleneckStation}\",\"bottleneck_equipment\":\"{BottleneckEquipment}\",\"overload_cause\":\"{OverloadCause}\",\"station_overloaded\":{JsonBool(StationOverloaded)},\"loads\":{{\"fryer\":{FryerLoad},\"grill\":{GrillLoad},\"assembly\":{AssemblyLoad},\"expo\":{ExpoLoad}}},\"capacities\":{{\"fryer\":{Num(FryerCapacity)},\"grill\":{Num(GrillCapacity)},\"assembly\":{Num(AssemblyCapacity)},\"expo\":{Num(ExpoCapacity+BeverageCapacity)},\"total\":{StaffCapacity}}},\"equipment_summary\":\"{Json(EquipmentSummary)}\",\"backlog_minutes\":{{\"fryer\":{Num(FryerBacklogMinutes)},\"grill\":{Num(GrillBacklogMinutes)},\"assembly\":{Num(AssemblyBacklogMinutes)},\"expo\":{Num(ExpoBacklogMinutes)},\"total\":{Num(KitchenBacklogMinutes)}}},\"inventory\":{{\"raw\":{Raw},\"prep\":{Prep},\"waste\":{Waste},\"main_protein\":{Num(inventory["main_protein"])},\"side_base\":{Num(inventory["side_base"])},\"drink_mix\":{Num(inventory["drink_mix"])},\"prep_pack\":{Num(inventory["prep_pack"])}}},\"temps\":{{\"cooler\":{Num(CoolerTemp)},\"hot_hold\":{Num(HotHoldTemp)}}},\"validation_status\":\"{ValidationStatus}\"}}\n";
 }

 public void RefreshValidation(string context="manual"){
  var issues=ValidationIssues();
  ValidationStatus=issues.Count==0?"OK":$"{issues.Count} issue(s)";
  var key=string.Join("|",issues);
  if(key==lastValidationKey)return;
  lastValidationKey=key;
  ValidationSeq++;
  ValidationLedger+=$"{ValidationSeq} {TimeText} {context} {ValidationStatus}";
  if(issues.Count>0)ValidationLedger+=" | "+key;
  ValidationLedger+="\n";
 }
 List<string> ValidationIssues(){
  EnsureEquipment();
  var issues=new List<string>();
  if(OrderChannelTotal!=Orders)issues.Add($"orders/channel mismatch orders={Orders} channels={OrderChannelTotal}");
  if(Tickets+CompletedTickets!=Orders)issues.Add($"ticket reconciliation mismatch orders={Orders} active={Tickets} completed={CompletedTickets}");
  if(CoverageUsed>CoveragePool)issues.Add($"coverage exceeds pool used={CoverageUsed} pool={CoveragePool}");
  if(Math.Abs(Sales-SalesBucketTotal)>.01)issues.Add($"sales total mismatch total={Num(Sales)} buckets={Num(SalesBucketTotal)}");
  if(Sales>0&&Math.Abs(LaborPercent-(LaborCost/Sales*100.0))>.01)issues.Add("labor percent formula mismatch");
  foreach(var kv in inventory)if(kv.Value<0)issues.Add($"negative inventory {kv.Key}={Num(kv.Value)}");
  foreach(var e in equipment)if(e.Load<-.01)issues.Add($"negative equipment load {e.Id}={Num(e.Load)}");
  if(EquipmentCount<10)issues.Add($"equipment model incomplete count={EquipmentCount}");
  if(StationOverloaded&&OverloadSeq<=0)issues.Add("station overloaded without overload ledger entry");
  return issues;
 }
 public string BuildValidationReport(){
  RefreshValidation("export");
  return $"validation_status={ValidationStatus}\nscenario={Scenario}\nseed={Seed}\nevents={EventSeq}\norders={Orders}\nchannel_total={OrderChannelTotal}\ncompleted_tickets={CompletedTickets}\nactive_tickets={Tickets}\nsales_total={Num(Sales)}\nsales_bucket_total={Num(SalesBucketTotal)}\nlabor_cost={Num(LaborCost)}\nlabor_percent={Num(LaborPercent)}\nprojected_labor_percent_30={Num(ProjectedLaborPercentThis30)}\nlabor_allowance_percent={Num(LaborTargetPercent*100)}\nallowed_labor_dollars_30={Num(AllowedLaborDollarsThis30)}\nprojected_labor_cost_30={Num(ProjectedLaborCostThis30)}\nlabor_dollars_variance_30={Num(LaborDollarsVarianceThis30)}\nallowed_labor_hours_30={Num(AllowedLaborHoursThis30)}\nscheduled_labor_hours_30={Num(ScheduledLaborHoursThis30)}\nlabor_hours_variance_30={Num(LaborHoursVarianceThis30)}\noverload_events={OverloadSeq}\nactive_overload={StationOverloaded}\nbottleneck_station={BottleneckStation}\nbottleneck_equipment={BottleneckEquipment}\nbottleneck_backlog_minutes={Num(BottleneckBacklogMinutes)}\nequipment_count={EquipmentCount}\nequipment_summary={EquipmentSummary}\nvalidation_ledger:\n{ValidationLedger}";
 }

 void EnsureEquipment(){
  if(equipmentReady)return;
  equipmentReady=true;
  AddEquipment("fryer_fries_1","fryer","fries",70);AddEquipment("fryer_fries_2","fryer","fries",70);AddEquipment("fryer_main_1","fryer","fried_main",70);AddEquipment("fryer_main_2","fryer","fried_main",70);
  AddEquipment("burger_press_1","grill","grilled_main",95);
  AddEquipment("soda_1","beverage","beverage",45);AddEquipment("soda_2","beverage","beverage",45);AddEquipment("soda_3","beverage","beverage",45);AddEquipment("soda_4","beverage","beverage",45);
  AddEquipment("assembly_rail_1","assembly","assembly",60);AddEquipment("assembly_rail_2","assembly","assembly",60);AddEquipment("expo_lane_1","expo","expo",65);AddEquipment("expo_lane_2","expo","expo",65);
  items["fried_main"]=new ItemSpec{Id="fried_main",Family="fried_main",Station="fryer",CookSeconds=330,HoldMinutes=20,AssemblySeconds=60,ExpoSeconds=25,Price=6.25};
  items["grilled_main"]=new ItemSpec{Id="grilled_main",Family="grilled_main",Station="grill",CookSeconds=150,HoldMinutes=15,AssemblySeconds=55,ExpoSeconds=25,Price=6.35};
  items["side"]=new ItemSpec{Id="side",Family="fries",Station="fryer",CookSeconds=180,HoldMinutes=7,AssemblySeconds=15,ExpoSeconds=5,Price=2.75};
  items["beverage"]=new ItemSpec{Id="beverage",Family="beverage",Station="beverage",CookSeconds=22,HoldMinutes=0,AssemblySeconds=0,ExpoSeconds=5,Price=2.35};
 }
 void AddEquipment(string id,string station,string family,double cap){equipment.Add(new EquipmentUnit{Id=id,Station=station,Family=family,BaseCapacity=cap});}

 double RatePerSimMinute(){if(Minute<360)return 0;var daily=Scenario=="slow_day"?620:Scenario=="rush_day"?1180:Scenario=="weather_disruption"?760:Scenario=="local_event_surge"?1050:Scenario=="school_event_surge"?980:Scenario=="holiday_pattern"?840:Scenario=="multi_rush_condition"?1240:920;return daily*DaypartShare()/DaypartMinutes()*Curve()*ScenarioMultiplier();}
 double ScenarioMultiplier()=>Scenario=="rush_day"?1.10:Scenario=="weather_disruption"?.86:Scenario=="equipment_failure"?.95:Scenario=="staffing_call_off"?.97:Scenario=="multi_rush_condition"?1.18:1.0;
 double DaypartShare()=>Daypart=="breakfast"?.18:Daypart=="mid_morning"?.07:Daypart=="lunch"?.30:Daypart=="afternoon"?.10:Daypart=="dinner"?.30:.05;
 double DaypartMinutes()=>Daypart=="breakfast"?240:Daypart=="mid_morning"?90:Daypart=="lunch"?150:Daypart=="afternoon"?150:Daypart=="dinner"?240:210;
 double Curve(){var peak=Daypart=="breakfast"?480:Daypart=="mid_morning"?615:Daypart=="lunch"?750:Daypart=="afternoon"?930:Daypart=="dinner"?1095:1260;var start=Daypart=="breakfast"?360:Daypart=="mid_morning"?600:Daypart=="lunch"?690:Daypart=="afternoon"?840:Daypart=="dinner"?990:1230;var end=Daypart=="breakfast"?600:Daypart=="mid_morning"?690:Daypart=="lunch"?840:Daypart=="afternoon"?990:Daypart=="dinner"?1230:1440;var half=Math.Max(1,Math.Max(peak-start,end-peak));var shape=1-Math.Min(1,Math.Abs(Minute-peak)/half);return .75+.5*shape;}
 double Sum(Func<Ticket,double> f){double n=0;foreach(var t in activeTickets)n+=Math.Max(0,f(t));return n;}
 int CountActive(string ch){var n=0;foreach(var t in activeTickets)if(t.Channel==ch)n++;return n;}
 double SumArray(double[] xs){double n=0;foreach(var x in xs)n+=x;return n;}
 double StaffScenarioMultiplier=>Scenario=="staffing_call_off"?.74:Scenario=="multi_rush_condition"?.78:Scenario=="holiday_pattern"?.92:1.0;
 double CoverageFactor(string station)=>station=="fryer"?(FryerCoverage<=0?0:Math.Min(1.25,.35+.325*FryerCoverage)):station=="grill"?(KitchenCoverage<=0?0:Math.Min(1.2,.50+.35*KitchenCoverage)):station=="beverage"?(DriveCoverage+CounterCoverage<=0?0:Math.Min(1.25,.25+.20*(DriveCoverage+CounterCoverage))):station=="assembly"?(KitchenCoverage+CounterCoverage<=0?0:Math.Min(1.2,.30+.30*(KitchenCoverage+CounterCoverage))):Math.Min(1.2,.25+.20*(DriveCoverage+CounterCoverage+KitchenCoverage));
 bool EquipmentAvailable(EquipmentUnit e)=>!(Scenario=="equipment_failure"&&(e.Id=="fryer_main_2"||e.Id=="soda_4"));
 double EquipmentCapacity(EquipmentUnit e)=>EquipmentAvailable(e)?e.BaseCapacity*CoverageFactor(e.Station)*StaffScenarioMultiplier:0;
 double EquipmentStationCapacity(string station){EnsureEquipment();double n=0;foreach(var e in equipment)if(e.Station==station)n+=EquipmentCapacity(e);return n;}
 double EquipmentLoadByFamily(string family){EnsureEquipment();double n=0;foreach(var e in equipment)if(e.Family==family)n+=Math.Max(0,e.Load);return n;}
 public double GrillCapacity=>EquipmentStationCapacity("grill");
 public double FryerCapacity=>EquipmentStationCapacity("fryer");
 public double AssemblyCapacity=>EquipmentStationCapacity("assembly");
 public double BeverageCapacity=>EquipmentStationCapacity("beverage");
 public double ExpoCapacity=>EquipmentStationCapacity("expo");

 public double Sales=>SalesTotal;public double SalesBucketTotal=>SumArray(sales30);public double WasteCost=>Waste*0.75;public double FoodCostPercent=>Sales<=0?0:WasteCost/Sales*100;
 public double LaborHourly=>Crew*16+Lead*18+ShiftMgr*22+AsstMgr*28+RestMgr*35;public double LaborPercent=>Sales<=0?0:LaborCost/Sales*100;
 public int TotalOnClock=>Crew+Lead+ShiftMgr+AsstMgr+RestMgr;public int EffectiveCrew=>Math.Max(0,Crew-CrewOnBreak);public int CoveragePool=>Math.Max(0,TotalOnClock-CrewOnBreak);public int CoverageUsed=>KitchenCoverage+FryerCoverage+DriveCoverage+CounterCoverage+PrepCoverage;public int CoverageOpen=>Math.Max(0,CoveragePool-CoverageUsed);public int AssignableLabor=>CoveragePool;public int StationAssigned=>CoverageUsed;public int AvailableLabor=>CoverageOpen;public int OrderChannelTotal=>DriveThru+FrontCounter+Delivery+Mobile;
 public int PaidHeadcount=>TotalOnClock;public double AverageLaborRate=>PaidHeadcount<=0?16.0:LaborHourly/PaidHeadcount;public double LaborTargetPercent=>ProjectedSalesThis30<150?0.35:ProjectedSalesThis30<300?0.32:ProjectedSalesThis30<600?0.30:0.28;public double HalfHourElapsedMinutes=>Math.Max(1.0,Minute%30.0);public double HalfHourProgress=>Math.Min(1.0,HalfHourElapsedMinutes/30.0);public double ProjectedSalesThis30=>SalesThis30/HalfHourProgress;public double ProjectedLaborCostThis30=>LaborHourly*0.5;public double ProjectedLaborPercentThis30=>ProjectedSalesThis30<=0?0:ProjectedLaborCostThis30/ProjectedSalesThis30*100.0;public double AllowedLaborDollarsThis30=>ProjectedSalesThis30*LaborTargetPercent;public double LaborDollarsVarianceThis30=>AllowedLaborDollarsThis30-ProjectedLaborCostThis30;public double AllowedLaborHoursThis30=>AverageLaborRate<=0?0:AllowedLaborDollarsThis30/AverageLaborRate;public double ScheduledLaborHoursThis30=>PaidHeadcount*0.5;public double LaborHoursVarianceThis30=>AllowedLaborHoursThis30-ScheduledLaborHoursThis30;
 public bool BreakDue=>ShiftMinutes>240&&BreaksTaken==0;public int StaffCapacity=>(int)(GrillCapacity+FryerCapacity+AssemblyCapacity+BeverageCapacity+ExpoCapacity);public int NetKitchenLoad=>KitchenLoad-StaffCapacity;public double KitchenBacklogMinutes=>StaffCapacity<=0?(Tickets>0?999:0):KitchenLoad/(double)StaffCapacity;public double FryerBacklogMinutes=>FryerCapacity<=0?(FryerLoad>0?999:0):FryerLoad/FryerCapacity;public double GrillBacklogMinutes=>GrillCapacity<=0?(GrillLoad>0?999:0):GrillLoad/GrillCapacity;public double AssemblyBacklogMinutes=>AssemblyCapacity<=0?(AssemblyLoad>0?999:0):AssemblyLoad/AssemblyCapacity;public double ExpoBacklogMinutes=>(ExpoCapacity+BeverageCapacity)<=0?(ExpoLoad>0?999:0):ExpoLoad/(ExpoCapacity+BeverageCapacity);
 public int PrepQuality=>PrepAge>=30?0:100-(int)(PrepAge*3);public int Tickets=>activeTickets.Count;public int FryerLoad=>(int)Math.Ceiling(Sum(t=>t.Fryer));public int GrillLoad=>(int)Math.Ceiling(Sum(t=>t.Grill));public int AssemblyLoad=>(int)Math.Ceiling(Sum(t=>t.Assembly));public int ExpoLoad=>(int)Math.Ceiling(Sum(t=>t.Expo)+Sum(t=>t.Beverage));public int KitchenLoad=>FryerLoad+GrillLoad+AssemblyLoad+ExpoLoad;public bool DelayRisk=>Tickets>30||(StaffCapacity<=0&&Tickets>0)||KitchenBacklogMinutes>10||FryerBacklogMinutes>8||GrillBacklogMinutes>8||AssemblyBacklogMinutes>7||ExpoBacklogMinutes>7;
 public string BottleneckStation{get{var station="fryer";var best=FryerBacklogMinutes;if(GrillBacklogMinutes>best){station="grill";best=GrillBacklogMinutes;}if(AssemblyBacklogMinutes>best){station="assembly";best=AssemblyBacklogMinutes;}if(ExpoBacklogMinutes>best){station="expo";best=ExpoBacklogMinutes;}return station;}}
 public double BottleneckBacklogMinutes=>StationBacklog(BottleneckStation);
 public string BottleneckEquipment{get{EnsureEquipment();string id="none";var best=-1.0;foreach(var e in equipment){var cap=Math.Max(1,EquipmentCapacity(e));var ratio=e.Load/cap;if(ratio>best){best=ratio;id=e.Id;}}return id;}}
 public int EquipmentCount{get{EnsureEquipment();return equipment.Count;}}
 public string EquipmentSummary{get{EnsureEquipment();var parts=new List<string>();foreach(var e in equipment)parts.Add($"{e.Id}:{Num(e.Load)}/{Num(EquipmentCapacity(e))}");return string.Join(";",parts);}}
 public string ItemCatalogJson=>"[{\"item_id\":\"fried_main\",\"equipment\":\"fryer_main_1|fryer_main_2\",\"cook_seconds\":330,\"hold_minutes\":20},{\"item_id\":\"grilled_main\",\"equipment\":\"burger_press_1\",\"cook_seconds\":150,\"hold_minutes\":15},{\"item_id\":\"side\",\"equipment\":\"fryer_fries_1|fryer_fries_2\",\"cook_seconds\":180,\"hold_minutes\":7},{\"item_id\":\"beverage\",\"equipment\":\"soda_1|soda_2|soda_3|soda_4\",\"cook_seconds\":22,\"hold_minutes\":0}]";
 double StationLoad(string station)=>station=="fryer"?FryerLoad:station=="grill"?GrillLoad:station=="assembly"?AssemblyLoad:ExpoLoad;
 double StationCapacity(string station)=>station=="fryer"?FryerCapacity:station=="grill"?GrillCapacity:station=="assembly"?AssemblyCapacity:ExpoCapacity+BeverageCapacity;
 double StationBacklog(string station){var cap=StationCapacity(station);var load=StationLoad(station);return cap<=0?(load>0?999:0):load/cap;}
 public bool SanitizerDue=>SanitizerAge>=120;public bool TempCheckDue=>TempCheckAge>=120;public bool TempOutOfRange=>CoolerTemp>41||HotHoldTemp<135;public string AlertText=>StationOverloaded?$"ALERT: {activeOverloadStation} overloaded | {OverloadCause} | {StationBacklog(activeOverloadStation):0.0}m backlog | equipment {BottleneckEquipment}":TempOutOfRange?"ALERT: temperature out of range":TempCheckDue?"Warning: temperature check due":SanitizerDue?"Warning: sanitizer change due":BreakDue?"Warning: break due":DelayRisk?$"Warning: {BottleneckStation} delay risk | {BottleneckBacklogMinutes:0.0}m backlog | equipment {BottleneckEquipment}":PrepQuality<50?"Warning: prep quality low":Prep<80?"Warning: prep low":"Alerts: none";public int DtSos=>(int)Math.Min(900,258+CountActive("drive_thru")*18+(DriveCoverage<=0?120:0)+(StationOverloaded?90:0));public int FcSos=>(int)Math.Min(720,180+CountActive("lobby")*20+(CounterCoverage<=0?120:0)+(StationOverloaded?75:0));public int DelSos=>(int)Math.Min(2100,420+CountActive("delivery")*35+(StationOverloaded?180:0));public int TicketsThis30=>tickets30[(int)(Minute/30)%48];public int TicketsThis60=>tickets30[(int)(Minute/30)%48]+tickets30[((int)(Minute/30)+47)%48];public double SalesThis30=>sales30[(int)(Minute/30)%48];public double SalesThis60=>sales30[(int)(Minute/30)%48]+sales30[((int)(Minute/30)+47)%48];public string Daypart=>Minute<360?"late_night":Minute<600?"breakfast":Minute<690?"mid_morning":Minute<840?"lunch":Minute<990?"afternoon":Minute<1230?"dinner":"late_night";public string TimeText=>$"{(int)(Minute/60):00}:{(int)(Minute%60):00}";

 string Channel(){var x=Roll(20);return x<600?"drive_thru":x<760?"lobby":x<860?"delivery":"mobile";}
 int Roll(int salt){var v=(long)Seed*1103515245L+(long)(Orders+1)*12345L+(long)salt*9973L+(long)((int)Minute)*31L;return (int)(Math.Abs(v)%1000);}
 double Range(double min,double target,double max,int salt){var r=Roll(salt)/999.0;return r<.5?min+(target-min)*(r*2):target+(max-target)*((r-.5)*2);}
 double CheckAmount(int items)=>10.0+(Roll(30)%201)/100.0+Math.Max(0,items-2)*1.25;
 int ExpectedTicketSeconds(string channel,int items)=>150+items*35+(channel=="delivery"?160:channel=="mobile"?45:channel=="lobby"?25:0);
 string CustomerSegment=>Daypart=="breakfast"?"commuter_breakfast":Daypart=="dinner"?"family_dinner":Daypart=="late_night"?"late_night_guest":"general_guest";
 string OverloadCause=>Scenario=="equipment_failure"?"equipment_constraint":Scenario=="staffing_call_off"||CoverageOpen<=0&&CoverageUsed>=CoveragePool?"staffing_gap":Scenario=="multi_rush_condition"?"multi_rush":Scenario=="rush_day"||Scenario=="local_event_surge"||Scenario=="school_event_surge"?"rush_demand":BottleneckStation=="fryer"?"item_cook_time_mix":"menu_mix";
 string StaffReason(string reason)=>reason=="call_off"?"call_off":reason=="break_coverage"?"break_coverage":reason=="manager_adjustment"?"manager_adjustment":"rush_support";
 string JsonStringOrNull(string? v)=>v==null?"null":$"\"{v}\"";
 string Num(double n)=>n.ToString("0.##",CultureInfo.InvariantCulture);
 string DrawJson(Dictionary<string,double> draw){var parts=new List<string>();foreach(var kv in draw)parts.Add($"\"{kv.Key}\":{Num(kv.Value)}");return "{"+string.Join(",",parts)+"}";}
 string Json(string s)=>s.Replace("\\","\\\\").Replace("\"","\\\"");
 string JsonBool(bool b)=>b?"true":"false";
}
