using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RestaurantSimulator;

public class SimRunState{
 public event Action<string,string>? OrderCreatedEvt;    // (channel, order_id)
 public event Action<string,string>? TicketCompletedEvt; // (channel, order_id)
 class Ticket{
  public int Id;
  public string Channel="drive_thru",OrderId="";
  public double Created;
  public double Total;                 // RS-HQ-001: reversible on abandonment
  public double MinQuality=100;        // worst freshness drawn from hold
  public bool Remade,HandoffQueued,Abandoned;
  public int IntakeTaskId;             // RS-FE-001: kitchen waits for order intake
  public List<EquipTask> Tasks=new();
  public List<PendingItem> Pending=new(); // items waiting on hold stock (item.taken deferred)
  public int TakenCount; public bool Packaged;   // RS-IM-003 order-level paper goods
 }
 class PendingItem{ public ItemSpec Spec=null!; public Dictionary<string,double> Draw=new(); }
 // RS-HQ-001: cook-to-hold buffer. Crew cooks full batch cycles into the pan;
 // orders draw FIFO in seconds; aged product expires to waste.
 class HoldPan{
  public string Family="",Station="",Cooked="";
  public (string C,double Q)[] Raw=System.Array.Empty<(string,double)>();
  public double CycleSeconds,BatchQty,Capacity;
  public int LimitMin;
  public int InFlight;
  public double InFlightQty;
  public readonly List<(double Qty,double At)> Batches=new();
  public double Level{get{double n=0;foreach(var b in Batches)n+=b.Qty;return n;}}
 }
 class EquipmentUnit{
  public string Id="",Station="",Family="";
  public double BaseCapacity,Assigned,Processed;
  public double MaintBase,MaintUntil=-1;   // RS-IN-001 wear baseline + downtime
  public bool FailedDown;
  public int Tasks;
  public EquipTask? Active;
  public List<EquipTask> Queue=new();
 }
 class EquipTask{
  public double BatchUnits;   // RS-HQ-001: partial hold batches off-peak
  public int Id,TicketId,DependencyId;
  public string OrderId="",ItemId="",Phase="",Station="",Family="",EquipmentId="";
  public double WorkSeconds,Remaining;
  public bool Queued,Started,Done;
 }
 class ItemSpec{
  public string Id="",Family="",Station="";
  public double CookSeconds,HoldMinutes,AssemblySeconds,ExpoSeconds,Price;
  public double BatchSize=1;
  public string? HoldFamily;  // RS-HQ-001: draws cooked product from this pan
 }
 // RS-GP-001: interrupt the player must answer (auto-resolves with the default).
 public class Decision{
  public int Id; public string Kind="",Title="",OptionA="",OptionB="";
  public double DeadlineMinute; public bool DefaultA=true;
 }

 readonly List<Ticket> activeTickets=new();
 readonly List<EquipmentUnit> equipment=new();
 readonly Dictionary<string,ItemSpec> items=new();
 readonly int[] tickets30=new int[48];
 readonly double[] sales30=new double[48];
 readonly Dictionary<string,double> inventory=new(){["main_protein"]=980,["side_base"]=520,["drink_mix"]=160,["prep_pack"]=260};
 double acc,over,recover,nextTraceMinute=360,lastOverloadMinutes;
 bool equipmentReady;
 string activeOverloadStation="assembly",lastValidationKey="";

 public int Seed=12345,Orders,DriveThru,FrontCounter,Delivery,Mobile,EventSeq,WasteSeq,StaffingSeq,TraceSeq,ValidationSeq,OverloadSeq,ItemSeq,TaskSeq,Raw=500,Waste,Crew=4,Lead=1,ShiftMgr=1,AsstMgr=0,RestMgr=0,CrewOnBreak,BreaksTaken,CallOffs,SanitationTasks,CompletedTickets,FriesSold,DrinksSold,MainsSold;
 public int KitchenCoverage=1,FryerCoverage=1,DriveCoverage=2,CounterCoverage=1,PrepCoverage=1;
 // RS-HQ-001 follow-up: prep is FIFO batches with per-batch 30-min life — the
 // old single-clock pool expired a quarter of everything every 30 minutes and
 // let one Discard click dump the whole pool.
 readonly List<(double Qty,double At)> prepBatches=new();
 public int Prep{get{double n=0;foreach(var b in prepBatches)n+=b.Qty;return (int)Math.Round(n);}}
 public double PrepAge=>prepBatches.Count==0?0:Math.Max(0,Minute-prepBatches[0].At);
 void ConsumePrep(double n){
  while(n>0&&prepBatches.Count>0){
   var b=prepBatches[0];var take=Math.Min(b.Qty,n);n-=take;
   if(b.Qty-take<=.0001)prepBatches.RemoveAt(0);else prepBatches[0]=(b.Qty-take,b.At);
  }
 }
 public double Minute=360,TimeScale=1.0,LaborCost,ShiftMinutes,BreakTimer,SanitizerAge,TempCheckAge,CoolerTemp=38,HotHoldTemp=145,SalesTotal;
 public string Scenario="normal_day",RecentEvents="",RecentJsonl="",ValidationStatus="OK",RecommendationRows="",AlertRows="";
 readonly StringBuilder allJsonl=new(),wasteSb=new(),staffingSb=new(),traceSb=new(),validationSb=new(),overloadSb=new(),equipmentSb=new(),itemSb=new(),taskSb=new();
 string wasteRecent="",staffingRecent="";
 public string AllJsonl=>allJsonl.ToString();
 public string WasteLedger=>wasteRecent;            // capped view for per-frame UI
 public string WasteLedgerFull=>wasteSb.ToString();
 public string StaffingLedger=>staffingRecent;      // capped view for per-frame UI
 public string StaffingLedgerFull=>staffingSb.ToString();
 public string TraceLedger=>traceSb.ToString();
 public string ValidationLedger=>validationSb.ToString();
 public string OverloadLedger=>overloadSb.ToString();
 public string EquipmentLedger=>equipmentSb.ToString();
 public string ItemLedger=>itemSb.ToString();
 public string TaskLedger=>taskSb.ToString();
 public bool Running,StationOverloaded,ShiftStarted,ShiftEnded;

 // Fixed timestep (audit F-01): one tick = one simulated second. TimeScale is
 // sim-seconds per real second; it changes how many ticks run per frame, never
 // what a tick does, so (scenario, seed) replays to an identical event stream.
 double secAcc;
 bool truckReceived;
 // RS-ST-001 scheduled staffing curve state
 public bool AutoSchedule=true;       // manual staffing actions switch this off
 int lastScheduleMinute=-1,schedSeq,breakReturnMinute=-1,callOffDeficitUntil=-1;
 bool callOffFired,replacementCalled;
 public bool ExternallyDriven; // set by 3D Main so dashboard panels don't double-step
 // RS-RM-001: store reputation carryover from career mode. Set once before the run
 // from CareerState.DemandMultiplier (bounded 0.85-1.05); 1.0 = neutral single-day
 // behavior, so every existing (scenario, seed) replay is byte-identical. The value
 // is a deterministic input like config: same (career state, scenario, seed) =>
 // same event stream. Store-level only — never an individual signal.
 public double ReputationDemandMultiplier=1.0;
 public void Step(double d){
  EnsureEquipment();
  if(!Running)return;
  secAcc+=Math.Max(0,d)*TimeScale;
  var n=(int)secAcc;
  if(n>1800)n=1800; // cap per-frame catch-up
  secAcc-=n;
  for(var i=0;i<n&&!ShiftEnded;i++)StepOneSimSecond();
 }
 void StepOneSimSecond(){
  EnsureShiftStarted();
  if(ShiftEnded)return;
  ClampCoverage();
  var sm=1.0/60.0;
  var finalStep=false;
  if(Minute+sm>=1439){sm=Math.Max(0,1439-Minute);finalStep=true;}
  Minute+=sm;
  ShiftMinutes+=sm;
  LaborCost+=LaborHourly*sm/60;
  if(CrewOnBreak>0)BreakTimer+=sm;
  SanitizerAge+=sm;
  TempCheckAge+=sm;
  UpdateTemperatures();
  ApplySchedule();
  HqSystems();
  ReceiveTruckIfDue();
  ProcessEquipment(sm);
  CompleteReadyTickets();
  acc+=RatePerSimMinute()*sm;
  while(acc>=1){AddOrder();acc-=1;}
  if(PrepAge>=30)ExpirePrep();
  if(ingredients!=null&&Math.Floor(Minute)>lastIngredientTick){lastIngredientTick=Math.Floor(Minute);ingredients.Tick(Minute);ingredients.AuditTemps(CoolerTemp,HotHoldTemp,-5);}
  if(!ManagerMode&&Prep<80&&PrepCoverage>0)DoPrep("auto_threshold");
  UpdateOverload(sm);
  if(Minute>=nextTraceMinute||finalStep){Trace("interval");CaptureValidationRow();nextTraceMinute=Math.Floor(Minute/15.0)*15.0+15.0;}
  RefreshValidation("step");
  if(finalStep)EndShift();
 }

 void AddOrder(){
  var ch=Channel();
  // RS-HQ-001: excessive visible/off-premise queue depth turns demand away before
  // it becomes a ticket. This keeps rushes painful without accepting impossible work.
  if(ShouldBalk(ch)){BalkedCars++;return;}
  Orders++;
  if(ch=="drive_thru")DriveThru++;else if(ch=="lobby")FrontCounter++;else if(ch=="delivery")Delivery++;else Mobile++;
  var oid=$"ord_{Orders:000000}";
  var fried=Roll(1)<500;
  var hasFries=Roll(2)<650;
  var hasDrink=Roll(3)<600;
  var secondMain=Roll(4)<200;
  var count=1+(secondMain?1:0)+(hasFries?1:0)+(hasDrink?1:0);
  var orderTotal=(fried?items["fried_main"].Price:items["grilled_main"].Price)
   +(secondMain?(Roll(5)<500?items["fried_main"].Price:items["grilled_main"].Price):0)
   +(hasFries?items["side"].Price:0)+(hasDrink?items["beverage"].Price:0);
  SalesTotal+=orderTotal;
  var b=(int)(Minute/30)%48;
  tickets30[b]++;
  sales30[b]+=orderTotal;
  Emit("order.created",$"{{\"order_id\":\"{oid}\",\"customer_segment\":\"{CustomerSegment}\",\"channel\":\"{ch}\",\"estimated_items\":{count},\"expected_ticket_seconds\":{ExpectedTicketSeconds(ch,count)}}}");
  OrderCreatedEvt?.Invoke(ch,oid);
  var t=new Ticket{Id=Orders,OrderId=oid,Channel=ch,Created=Minute,Total=orderTotal};
  activeTickets.Add(t);
  // RS-FE-001: order intake occupies window/counter labor; kitchen waits on it.
  // Mobile/delivery were ordered remotely — no intake labor.
  if(ch=="drive_thru")t.IntakeTaskId=CreateTask(t,"order_intake","service","drive_thru","dt_service",SimConfig.DtIntakeSec,0).Id;
  else if(ch=="lobby")t.IntakeTaskId=CreateTask(t,"order_intake","service","lobby","counter_service",SimConfig.FcIntakeSec,0).Id;
  if(fried){MainsSold++;RegisterItem(t,items["fried_main"],new Dictionary<string,double>{{"cooked_fried_main",1},{"prep_pack",.5}});}else{MainsSold++;RegisterItem(t,items["grilled_main"],new Dictionary<string,double>{{"cooked_grilled_main",1},{"prep_pack",.5}});}
  if(secondMain){MainsSold++;var sm2=Roll(5)<500;RegisterItem(t,sm2?items["fried_main"]:items["grilled_main"],new Dictionary<string,double>{{sm2?"cooked_fried_main":"cooked_grilled_main",1},{"prep_pack",.5}});}
  if(hasFries){FriesSold++;RegisterItem(t,items["side"],new Dictionary<string,double>{{"cooked_fries",1}});}
  if(hasDrink){DrinksSold++;RegisterItem(t,items["beverage"],new Dictionary<string,double>{{"drink_mix",.25}});}
  TryTakeItems(t);
  QueueReadyTasks(t);
  Emit("ticket.updated",$"{{\"ticket_id\":\"tkt_{t.Id:000000}\",\"order_id\":\"{oid}\",\"status\":\"queued\",\"queue_seconds\":0,\"station_id\":\"{FirstOpenStation(t)}\"}}");
 }

 void RegisterItem(Ticket t,ItemSpec spec,Dictionary<string,double> draw){
  t.Pending.Add(new PendingItem{Spec=spec,Draw=draw});
 }
 // RS-HQ-001: item.taken fires only once the hold pan can actually supply the
 // item — a stockout defers the take (and the lifecycle) while the guest waits.
 void TryTakeItems(Ticket t){
  for(var i=t.Pending.Count-1;i>=0;i--){
   var pi=t.Pending[i];
   var ok=true;
   foreach(var kv in pi.Draw)if(inventory.GetValueOrDefault(kv.Key)<kv.Value-.0001){
    ok=false;
    if(coockedToPan.TryGetValue(kv.Key,out var pf2)){
     var p2=pans[pf2];
     if(p2.InFlight==0&&RawAvailable(p2))QueueBatch(p2,Math.Max(2,p2.BatchQty*0.5));  // cook-to-order
    }
    break;
   }
   if(!ok)continue;
   foreach(var kv in pi.Draw){
    if(coockedToPan.TryGetValue(kv.Key,out var pf)){t.MinQuality=Math.Min(t.MinQuality,DrawCooked(pans[pf],kv.Value));continue;}
    if(kv.Key=="prep_pack")ConsumePrep(kv.Value);   // FIFO age tracking; inventory book below
    var have=inventory.GetValueOrDefault(kv.Key);
    var take=Math.Min(have,kv.Value);
    inventory[kv.Key]=have-take;
    invConsumed[kv.Key]=invConsumed.GetValueOrDefault(kv.Key)+take;
   }
   var spec=pi.Spec;
   var dep=t.IntakeTaskId;
   if(spec.CookSeconds>0)dep=CreateTask(t,spec.Id,"cook",spec.Station,spec.Family,spec.CookSeconds,dep).Id;
   if(spec.AssemblySeconds>0)dep=CreateTask(t,spec.Id,"assembly","assembly","assembly",spec.AssemblySeconds,dep).Id;
   if(spec.ExpoSeconds>0)CreateTask(t,spec.Id,"expo","expo","expo",spec.ExpoSeconds,dep);
   ItemSeq++;
   itemSb.Append($"{ItemSeq} {TimeText} taken order {t.OrderId} item {spec.Id} hold {spec.HoldFamily??"none"} quality {Num(t.MinQuality)} assembly {Num(spec.AssemblySeconds)}s expo {Num(spec.ExpoSeconds)}s price ${spec.Price:0.00}\n");
   Emit("item.taken",$"{{\"order_id\":\"{t.OrderId}\",\"item_id\":\"{spec.Id}\",\"quantity\":1,\"station_ids\":[\"{spec.Station}\"],\"inventory_draw\":{DrawJson(pi.Draw)},\"status\":\"taken\"}}");
   if(ingredients!=null&&abstractToMenu.TryGetValue(spec.Id,out var mi))ingredients.ConsumeMenuItem(mi,Minute);
   t.TakenCount++;
   t.Pending.RemoveAt(i);
  }
  if(t.Pending.Count==0){
   if(ingredients!=null&&!t.Packaged){ingredients.ConsumeOrderPackaging(t.TakenCount,Minute);t.Packaged=true;}  // RS-IM-003
   QueueReadyTasks(t);
  }
 }
 static readonly Dictionary<string,string> coockedToPan=new(){{"cooked_fried_main","fried_main"},{"cooked_grilled_main","grilled_main"},{"cooked_fries","fries"}};
 double DrawCooked(HoldPan pan,double qty){
  inventory[pan.Cooked]=Math.Max(0,inventory.GetValueOrDefault(pan.Cooked)-qty);
  invConsumed[pan.Cooked]=invConsumed.GetValueOrDefault(pan.Cooked)+qty;
  double quality=100;
  while(qty>0&&pan.Batches.Count>0){
   var bch=pan.Batches[0];
   var age=Math.Max(0,Minute-bch.At);
   quality=Math.Min(quality,100-40*Math.Min(1,age/Math.Max(1,pan.LimitMin)));
   var take=Math.Min(bch.Qty,qty);
   qty-=take;
   if(bch.Qty-take<=.0001)pan.Batches.RemoveAt(0);
   else pan.Batches[0]=(bch.Qty-take,bch.At);
  }
  return quality;
 }

 EquipTask CreateTask(Ticket t,string item,string phase,string station,string family,double work,int dependency){
  var task=new EquipTask{Id=++TaskSeq,TicketId=t.Id,OrderId=t.OrderId,ItemId=item,Phase=phase,Station=station,Family=family,WorkSeconds=work,Remaining=work,DependencyId=dependency};
  t.Tasks.Add(task);
  taskSb.Append($"{TimeText} created task_{task.Id:000000} order {task.OrderId} item {item} phase {phase} station {station} family {family} dependency {(dependency==0?"none":$"task_{dependency:000000}")} work {Num(work)}s\n");
  return task;
 }

 void QueueReadyTasks(Ticket t){
  foreach(var task in t.Tasks){
   if(task.Done||task.Queued||!DependencyDone(t,task))continue;
   var unit=SelectEquipment(task.Family);
   if(unit==null)continue;
   task.Queued=true;task.EquipmentId=unit.Id;
   unit.Queue.Add(task);unit.Assigned+=task.WorkSeconds;unit.Tasks++;
   taskSb.Append($"{TimeText} queued task_{task.Id:000000} order {task.OrderId} item {task.ItemId} phase {task.Phase} equipment {unit.Id} load {Num(EquipmentLoad(unit))} cap_min {Num(EquipmentCapacity(unit))}\n");
   equipmentSb.Append($"{TimeText} assign order {task.OrderId} item {task.ItemId} phase {task.Phase} equipment {unit.Id} family {task.Family} work {Num(task.WorkSeconds)}s load {Num(EquipmentLoad(unit))} cap_min {Num(EquipmentCapacity(unit))}\n");
  }
 }
 bool DependencyDone(Ticket t,EquipTask task){if(task.DependencyId==0)return true;foreach(var x in t.Tasks)if(x.Id==task.DependencyId)return x.Done;return false;}
 Ticket? FindTicket(int id){foreach(var t in activeTickets)if(t.Id==id)return t;return null;}

 EquipmentUnit? SelectEquipment(string family){
  EnsureEquipment();
  EquipmentUnit? best=null;var bestRatio=double.MaxValue;
  foreach(var e in equipment){
   if(e.Family!=family||!EquipmentAvailable(e))continue;
   var ratio=EquipmentLoad(e)/Math.Max(1,EquipmentCapacity(e));
   if(ratio<bestRatio){best=e;bestRatio=ratio;}
  }
  if(best!=null)return best;
  foreach(var e in equipment)if(e.Family==family)return e;
  return null;
 }

 void ProcessEquipment(double sm){
  foreach(var e in equipment){
   var cap=Math.Max(0,EquipmentCapacity(e)*sm);
   while(cap>0){
    if(e.Active==null)StartNext(e);
    if(e.Active==null)break;
    var done=Math.Min(e.Active.Remaining,cap);
    e.Active.Remaining-=done;cap-=done;e.Processed+=done;
    if(e.Active.Remaining<=.001)CompleteTask(e);
   }
  }
 }
 void StartNext(EquipmentUnit e){
  while(e.Queue.Count>0&&e.Queue[0].TicketId!=0&&abandonedTicketIds.Contains(e.Queue[0].TicketId)){
   var sk=e.Queue[0];e.Queue.RemoveAt(0);sk.Done=true;sk.Remaining=0;   // guest left; drop the work
  }
  if(e.Queue.Count<=0)return;
  e.Active=e.Queue[0];e.Queue.RemoveAt(0);e.Active.Started=true;
  taskSb.Append($"{TimeText} started task_{e.Active.Id:000000} order {e.Active.OrderId} item {e.Active.ItemId} phase {e.Active.Phase} equipment {e.Id}\n");
 }
 void CompleteTask(EquipmentUnit e){
  if(e.Active==null)return;
  var task=e.Active;task.Done=true;task.Remaining=0;
  taskSb.Append($"{TimeText} completed task_{task.Id:000000} order {task.OrderId} item {task.ItemId} phase {task.Phase} equipment {e.Id}\n");
  e.Active=null;
  if(task.Phase=="batch_cook"){BatchComplete(task);return;}
  var t=FindTicket(task.TicketId);
  if(t==null)return;
  if(task.Phase=="expo"){
   EmitItemCompleted(t,task);
   // RS-HQ-001: 1% production error caught at expo -> remake chain (accuracy hit;
   // the guest waits for the redo, satisfaction reflects it).
   if(Roll(31+task.Id%97)<10){
    t.Remade=true;
    RecordWaste("production_error",1,"assembly");
    ingredients?.WastePackaging("sandwich_wrap",1,Minute);   // RS-IM-003 remade item spoils its wrap
    var rd=CreateTask(t,task.ItemId,"assembly_redo","assembly","assembly",20,0).Id;
    CreateTask(t,task.ItemId,"expo_redo","expo","expo",10,rd);
   }
  }
  QueueReadyTasks(t);
  // RS-FE-001: hand-off labor at the window/counter/shelf once everything is plated.
  if(!t.HandoffQueued&&t.Pending.Count==0){
   var all=true;foreach(var x in t.Tasks)if(x.Phase!="handoff"&&!x.Done){all=false;break;}
   if(all){
    t.HandoffQueued=true;
    var (hst,hfam,hsec)=HandoffFor(t.Channel);
    CreateTask(t,"order_handoff","handoff",hst,hfam,hsec,0);
    QueueReadyTasks(t);
   }
  }
 }
 (string,string,double) HandoffFor(string ch)=>ch switch{
  "drive_thru"=>("drive_thru","dt_service",(double)SimConfig.DtHandoffSec),
  "lobby"=>("lobby","counter_service",(double)SimConfig.FcHandoffSec),
  "mobile"=>("pickup","pickup_service",(double)SimConfig.MobHandoffSec),
  _=>("pickup","pickup_service",(double)SimConfig.DelHandoffSec)};
 void BatchComplete(EquipTask task){
  var pan=pans[task.Family];
  pan.InFlight=Math.Max(0,pan.InFlight-1);
  pan.InFlightQty=Math.Max(0,pan.InFlightQty-(task.BatchUnits>0?task.BatchUnits:pan.BatchQty));
  var space=Math.Max(0,pan.Capacity-pan.Level);
  var rawAvail=double.MaxValue;
  foreach(var (c,q) in pan.Raw)rawAvail=Math.Min(rawAvail,inventory.GetValueOrDefault(c)/Math.Max(.0001,q));
  var qty=Math.Floor(Math.Min(task.BatchUnits>0?task.BatchUnits:pan.BatchQty,Math.Min(space,rawAvail)));
  if(qty<=0)return;
  foreach(var (c,q) in pan.Raw){
   var need=qty*q;var take=Math.Min(inventory.GetValueOrDefault(c),need);
   inventory[c]=inventory.GetValueOrDefault(c)-take;
   invConsumed[c]=invConsumed.GetValueOrDefault(c)+take;
  }
  inventory[pan.Cooked]=inventory.GetValueOrDefault(pan.Cooked)+qty;
  invReceived[pan.Cooked]=invReceived.GetValueOrDefault(pan.Cooked)+qty;
  pan.Batches.Add((qty,Minute));
  Emit("prep.confirmed",$"{{\"prep_batch_id\":\"batch_{task.Id:000000}\",\"inventory_item_id\":\"{pan.Cooked}\",\"quantity\":{Num(qty)},\"unit\":\"units\",\"station_id\":\"{pan.Station}\",\"confirmed_by_role\":\"cook\"}}");
 }
 void EmitItemCompleted(Ticket t,EquipTask task){
  var sec=(int)((Minute-t.Created+1440)%1440*60);
  itemSb.Append($"{ItemSeq} {TimeText} completed order {t.OrderId} item {task.ItemId} final_equipment {task.EquipmentId} elapsed {sec}s\n");
  Emit("item.completed",$"{{\"order_id\":\"{t.OrderId}\",\"item_id\":\"{task.ItemId}\",\"quantity\":1,\"station_ids\":[\"{task.EquipmentId}\"],\"completed_station_id\":\"{task.Station}\",\"elapsed_seconds\":{sec},\"status\":\"completed\"}}");
 }
 readonly List<(string Ch,int Sec,double At)> sosSamples=new();
 readonly Dictionary<string,HoldPan> pans=new();
 readonly HashSet<int> abandonedTicketIds=new();
 public readonly List<Decision> Decisions=new();
 int decisionSeq;
 double satSum;int satCount;
 public int AbandonedTickets,BalkedCars,ComplaintsComped;
 public double LostSales,OvertimePremium,CompCost,MaintSpend;
 public double InspectionScore=-1;public string InspectionNotes="";
 public bool ManagerMode;          // RS-GP-001: false = full-auto (ASC/headless unchanged semantics)
 public bool AutoHold=true;        // auto par-level batch drops
 int inspectionMinute=-1;bool inspectionDone,truckDecisionRaised;
 void CompleteReadyTickets(){
  for(var i=activeTickets.Count-1;i>=0;i--){
   var t=activeTickets[i];if(!TicketDone(t))continue;
   CompletedTickets++;
   var sec=(int)((Minute-t.Created+1440)%1440*60);
   // RS-HQ-001: satisfaction from speed vs channel target, hold freshness, accuracy.
   var target=t.Channel=="drive_thru"?SimConfig.DtSosTarget:t.Channel=="lobby"?SimConfig.FcSosTarget:t.Channel=="mobile"?SimConfig.MobileTarget:SimConfig.DelTarget;
   var sat=Math.Clamp(100-Math.Max(0,(sec-target)/8.0)-(100-t.MinQuality)*0.4-(t.Remade?15:0),0,100);
   satSum+=sat;satCount++;
   if(sat<40&&Roll(9+t.Id%53)<350)
    RaiseDecision("complaint",$"Guest complaint on {t.OrderId} ({sec}s wait)","Apologize","Comp the meal ($8, goodwill)",defaultA:true);
   sosSamples.Add((t.Channel,sec,Minute));
   if(sosSamples.Count>4000)sosSamples.RemoveRange(0,1000);
   Emit("ticket.updated",$"{{\"ticket_id\":\"tkt_{t.Id:000000}\",\"order_id\":\"{t.OrderId}\",\"status\":\"completed\",\"queue_seconds\":{sec},\"station_id\":\"expo\"}}");
   TicketCompletedEvt?.Invoke(t.Channel,t.OrderId);
   activeTickets.RemoveAt(i);
  }
 }
 public double MeasuredSos(string ch,double windowMin=30){
  double sum=0;var n=0;
  for(var i=sosSamples.Count-1;i>=0;i--){var x=sosSamples[i];if(Minute-x.At>windowMin)break;if(x.Ch==ch){sum+=x.Sec;n++;}}
  return n==0?-1:sum/n;
 }
 public double MeasuredSosAllDay(string ch){double sum=0;var n=0;foreach(var x in sosSamples)if(x.Ch==ch){sum+=x.Sec;n++;}return n==0?0:sum/n;}
 public string PosTicketSummary(int max=5){
  if(activeTickets.Count==0)return "Active tickets: none";
  var sb=new StringBuilder();
  var shown=0;
  foreach(var t in activeTickets){
   if(shown>=max)break;
   var done=0;foreach(var task in t.Tasks)if(task.Done)done++;
   var station=FirstOpenStation(t);
   var elapsed=(int)((Minute-t.Created+1440)%1440*60);
   if(shown>0)sb.Append('\n');
   sb.Append(t.OrderId).Append("  ").Append(ChannelLabel(t.Channel))
    .Append("  ").Append(station).Append("  ").Append(elapsed).Append("s")
    .Append("  items ").Append(t.TakenCount).Append('/').Append(t.TakenCount+t.Pending.Count)
    .Append("  tasks ").Append(done).Append('/').Append(t.Tasks.Count);
   if(t.Pending.Count>0)sb.Append("  waiting ").Append(PendingSummary(t));
   shown++;
  }
  if(activeTickets.Count>shown)sb.Append("\n+").Append(activeTickets.Count-shown).Append(" more active");
  return sb.ToString();
 }
 bool TicketDone(Ticket t){if(t.Pending.Count>0)return false;if(t.Tasks.Count==0)return false;if(!t.HandoffQueued)return false;foreach(var task in t.Tasks)if(!task.Done)return false;return true;}
 string FirstOpenStation(Ticket t){foreach(var task in t.Tasks)if(!task.Done)return task.Station;return "expo";}
 static string ChannelLabel(string ch)=>ch switch{"drive_thru"=>"DT","lobby"=>"FC","mobile"=>"MOB","delivery"=>"DEL",_=>ch};
 static string PendingSummary(Ticket t){
  var names=new List<string>();
  foreach(var p in t.Pending){
   if(names.Count>=2)break;
   names.Add(p.Spec.Id);
  }
  var text=string.Join(",",names);
  if(t.Pending.Count>names.Count)text+="+"+(t.Pending.Count-names.Count);
  return text;
 }

 public void AddKitchenCoverage(){AddCov(ref KitchenCoverage,"grill");}public void RemoveKitchenCoverage(){RemoveCov(ref KitchenCoverage,"grill");}
 public void AddFryerCoverage(){AddCov(ref FryerCoverage,"fryer");}public void RemoveFryerCoverage(){RemoveCov(ref FryerCoverage,"fryer");}
 public void AddDriveCoverage(){AddCov(ref DriveCoverage,"drive_thru");}public void RemoveDriveCoverage(){RemoveCov(ref DriveCoverage,"drive_thru");}
 public void AddCounterCoverage(){AddCov(ref CounterCoverage,"lobby");}public void RemoveCounterCoverage(){RemoveCov(ref CounterCoverage,"lobby");}
 public void AddPrepCoverage(){AddCov(ref PrepCoverage,"prep");}public void RemovePrepCoverage(){RemoveCov(ref PrepCoverage,"prep");}
 void AddCov(ref int v,string station){AutoSchedule=false;if(CoverageOpen<=0)return;v++;RecordStaffing("coverage_unit","coverage_unit",null,station,"manager_adjustment");}
 void RemoveCov(ref int v,string station){AutoSchedule=false;if(v<=0)return;v--;RecordStaffing("coverage_unit","coverage_unit",station,null,"manager_adjustment");}
 void ClampCoverage(){while(CoverageUsed>CoveragePool){if(PrepCoverage>0)PrepCoverage--;else if(CounterCoverage>0)CounterCoverage--;else if(DriveCoverage>0)DriveCoverage--;else if(KitchenCoverage>0)KitchenCoverage--;else if(FryerCoverage>0)FryerCoverage--;else break;}}

 public void ManualPrep(){if(PrepCoverage>0)DoPrep("manager_adjustment");}
 /// Discards only the OLDEST prep batch (the quality action targets aged product).
 public void ManualDiscard(){if(prepBatches.Count==0)return;var w=prepBatches[0].Qty;prepBatches.RemoveAt(0);RecordWaste("quality_discard",(int)Math.Round(w),"assembly");}
 public void ChangeSanitizer(){SanitizerAge=0;SanitationTasks++;Trace("sanitizer_changed");}
 public void CheckTemps(){TempCheckAge=0;UpdateTemperatures();Trace("temperature_checked");}
 public void AddCrew(){AutoSchedule=false;Crew++;RecordStaffing("crew_member","crew_shift_pool",null,"grill","manager_adjustment");}
 public void CutCrew(){AutoSchedule=false;if(Crew<=0)return;Crew--;ClampCoverage();RecordStaffing("crew_member","crew_shift_pool","grill",null,"manager_adjustment");}
 public void AddLead(){AutoSchedule=false;Lead++;RecordStaffing("team_leader","crew_shift_lead",null,"floor","manager_adjustment");}
 public void CutLead(){AutoSchedule=false;if(Lead<=0)return;Lead--;ClampCoverage();RecordStaffing("team_leader","crew_shift_lead","floor",null,"manager_adjustment");}
 public void AddShiftMgr(){AutoSchedule=false;ShiftMgr++;RecordStaffing("shift_manager","manager_shift",null,"floor","manager_adjustment");}
 public void CutShiftMgr(){AutoSchedule=false;if(ShiftMgr<=0)return;ShiftMgr--;ClampCoverage();RecordStaffing("shift_manager","manager_shift","floor",null,"manager_adjustment");}
 public void AddAsstMgr(){AutoSchedule=false;AsstMgr++;RecordStaffing("assistant_manager","manager_assistant",null,"floor","manager_adjustment");}
 public void CutAsstMgr(){AutoSchedule=false;if(AsstMgr<=0)return;AsstMgr--;ClampCoverage();RecordStaffing("assistant_manager","manager_assistant","floor",null,"manager_adjustment");}
 public void AddRestMgr(){AutoSchedule=false;RestMgr++;RecordStaffing("restaurant_manager","manager_restaurant",null,"floor","manager_adjustment");}
 public void CutRestMgr(){AutoSchedule=false;if(RestMgr<=0)return;RestMgr--;ClampCoverage();RecordStaffing("restaurant_manager","manager_restaurant","floor",null,"manager_adjustment");}
 public void StartBreak(){AutoSchedule=false;if(CrewOnBreak>=Crew)return;CrewOnBreak++;BreakTimer=0;ClampCoverage();RecordStaffing("crew_member","crew_shift_break","grill",null,"break_coverage");}
 public void EndBreak(){AutoSchedule=false;if(CrewOnBreak<=0)return;CrewOnBreak--;BreaksTaken++;BreakTimer=0;RecordStaffing("crew_member","crew_shift_break",null,"grill","break_coverage");}
 public void CallOff(){AutoSchedule=false;if(Crew<=0)return;Crew--;CallOffs++;if(CrewOnBreak>Crew)CrewOnBreak=Crew;ClampCoverage();RecordStaffing("cook","crew_shift_calloff","fryer",null,"call_off");}

 void RecordStaffing(string role,string worker,string? from,string? to,string reason){
  EnsureShiftStarted();StaffingSeq++;var src=$"evt_{EventSeq+1:000000}";
  {var sl=$"{StaffingSeq} {TimeText} {role} {worker} {from ?? "none"}->{to ?? "none"} eff {EffectiveCrew} cap {StaffCapacity} coverage {CoverageUsed}/{CoveragePool} labor_pct {Num(LaborPercent)} projected_labor_pct {Num(ProjectedLaborPercentThis30)} allowed_hrs {Num(AllowedLaborHoursThis30)} variance_hrs {Num(LaborHoursVarianceThis30)} reason {reason} src {src}\n";staffingSb.Append(sl);staffingRecent=sl+staffingRecent;if(staffingRecent.Length>900)staffingRecent=staffingRecent[..900];}
  Emit("staff.assignment.updated",$"{{\"assignment_id\":\"asg_{StaffingSeq:000000}\",\"synthetic_worker_ref\":\"{worker}\",\"role_id\":\"{role}\",\"from_station_id\":{JsonStringOrNull(from)},\"to_station_id\":{JsonStringOrNull(to)},\"reason\":\"{StaffReason(reason)}\"}}");
 }
 double PrepBurnPerMin=>RatePerSimMinute()*0.62;
 double PrepPar=>Math.Clamp(PrepBurnPerMin*18,8,120);
 void DoPrep(string reason){
  var par=Math.Clamp(RatePerSimMinute()*0.62*21,10,160);
  var n=(int)Math.Floor(Math.Min(Raw,Math.Min(par,Math.Max(0,par*2-Prep))));
  if(n<=0)return;Raw-=n;prepBatches.Add((n,Minute));inventory["prep_pack"]+=n;invReceived["prep_pack"]=invReceived.GetValueOrDefault("prep_pack")+n;Trace($"prep_{reason}");Emit("prep.confirmed",$"{{\"prep_batch_id\":\"prep_{EventSeq+1:000000}\",\"inventory_item_id\":\"prep_pack\",\"quantity\":{Num(n)},\"unit\":\"units\",\"station_id\":\"prep\",\"confirmed_by_role\":\"cook\"}}");}
 void ExpirePrep(){
  while(prepBatches.Count>0&&Minute-prepBatches[0].At>30){
   var w=(int)Math.Round(prepBatches[0].Qty);prepBatches.RemoveAt(0);
   if(w>0)RecordWaste("holding_time_exceeded",w,"assembly");
  }
 }
 void RecordWaste(string r,int u,string station){Waste+=u;WasteSeq++;var wTake=Math.Min(inventory["prep_pack"],u);inventory["prep_pack"]-=wTake;invWasted["prep_pack"]=invWasted.GetValueOrDefault("prep_pack")+wTake;{var wl=$"{WasteSeq} {TimeText} {r} {u}u ${u*0.75:0.00} prep {Prep} raw {Raw} inv_prep {Num(inventory["prep_pack"])} station {station}\n";wasteSb.Append(wl);wasteRecent=wl+wasteRecent;if(wasteRecent.Length>900)wasteRecent=wasteRecent[..900];}Emit("waste.recorded",$"{{\"waste_id\":\"waste_{WasteSeq:000000}\",\"inventory_item_id\":\"prep_pack\",\"quantity\":{Num(u)},\"unit\":\"units\",\"reason\":\"{r}\",\"station_id\":\"{station}\"}}");}

 void UpdateOverload(double m){var was=StationOverloaded;var station=BottleneckStation;if(DelayRisk){over+=m;recover=0;lastOverloadMinutes=over;if(over>=5){StationOverloaded=true;if(!was){activeOverloadStation=station;OverloadSeq++;RecordOverload("started");}}}else{over=0;if(StationOverloaded){recover+=m;if(recover>=4){StationOverloaded=false;RecordOverload("recovered");}}}if(!was&&StationOverloaded)Emit("station.overloaded",StationPayload(true));if(was&&!StationOverloaded)Emit("station.recovered",StationPayload(false));}
 void UpdateTemperatures(){CoolerTemp=38+(Scenario=="equipment_failure"?4:0)+Math.Min(3,PrepAge/20);HotHoldTemp=145-(Scenario=="equipment_failure"?9:0)-(StationOverloaded?3:0)-Math.Min(6,PrepAge/10);}
 string StationPayload(bool overloaded){var station=activeOverloadStation;if(overloaded)return $"{{\"station_id\":\"{station}\",\"load_units\":{Num(StationLoad(station))},\"capacity_units\":{Num(StationCapacity(station))},\"duration_minutes\":{Num(Math.Max(5,lastOverloadMinutes))},\"primary_cause\":\"{OverloadCause}\"}}";return $"{{\"station_id\":\"{station}\",\"load_units\":{Num(StationLoad(station))},\"capacity_units\":{Num(StationCapacity(station))},\"recovery_duration_minutes\":{Num(recover)},\"recovery_reason\":\"{RecoveryReason}\"}}";}
 void RecordOverload(string state){var station=activeOverloadStation;overloadSb.Append($"{OverloadSeq} {TimeText} {state} station {station} equipment {BottleneckEquipment} cause {OverloadCause} load {Num(StationLoad(station))} cap {Num(StationCapacity(station))} backlog {Num(StationBacklog(station))}m tickets {Tickets} fryer {FryerLoad}/{Num(FryerCapacity)} grill {GrillLoad}/{Num(GrillCapacity)} assembly {AssemblyLoad}/{Num(AssemblyCapacity)} expo {ExpoLoad}/{Num(ExpoCapacity+BeverageCapacity)} coverage {CoverageUsed}/{CoveragePool} labor_pct {Num(LaborPercent)} equipment_summary {EquipmentSummary}\n");Trace($"overload_{state}");}

 void EnsureShiftStarted(){if(ShiftStarted)return;EnsureEquipment();EnsureIngredientLedger();foreach(var kv in inventory)invOpening[kv.Key]=kv.Value;ShiftStarted=true;Emit("shift.started",$"{{\"shift_id\":\"shift_{Seed}\",\"manager_role_ref\":\"manager_on_duty\",\"opening_inventory_snapshot_id\":\"inv_open_{Seed}\",\"scheduled_role_count\":{TotalOnClock}}}");
  // Opening roster events (RS-ST-001): every on-clock head is an explicit shift_start assignment.
  for(var i=0;i<Crew;i++)RecordStaffing("crew_member",$"crew_sched_{++schedSeq:00}",null,"kitchen","shift_start");
  if(Lead>0)RecordStaffing("team_leader","lead_sched_01",null,"floor","shift_start");
  if(ShiftMgr>0)RecordStaffing("shift_manager","mgr_sched_01",null,"floor","shift_start");}
 void EndShift(){if(ShiftEnded)return;CloseOpenTickets();ShiftEnded=true;RefreshValidation("shift_end");Emit("shift.ended",$"{{\"shift_id\":\"shift_{Seed}\",\"closing_inventory_snapshot_id\":\"inv_close_{Seed}\",\"orders_total\":{Orders},\"waste_events_total\":{WasteSeq},\"overload_events_total\":{OverloadSeq}}}");Running=false;}
 void CloseOpenTickets(){
  for(var i=activeTickets.Count-1;i>=0;i--){
   var t=activeTickets[i];
   var sec=(int)((Minute-t.Created+1440)%1440*60);
   AbandonedTickets++;LostSales+=t.Total;
   SalesTotal-=t.Total;sales30[(int)(Minute/30)%48]-=t.Total;
   abandonedTicketIds.Add(t.Id);t.Abandoned=true;
   Emit("ticket.updated",$"{{\"ticket_id\":\"tkt_{t.Id:000000}\",\"order_id\":\"{t.OrderId}\",\"status\":\"abandoned\",\"queue_seconds\":{sec},\"station_id\":\"{FirstOpenStation(t)}\",\"reason\":\"shift_closeout\"}}");
   TicketAbandonedEvt?.Invoke(t.Channel,t.OrderId);
   activeTickets.RemoveAt(i);
  }
 }
 void Emit(string t,string p){if(!ShiftStarted&&t!="shift.started")EnsureShiftStarted();if(ShiftEnded&&t!="shift.ended")return;var e=new SimEvent(++EventSeq,TimeText,t,Scenario,Seed,Daypart,p);allJsonl.Append(e.Jsonl).Append('\n');RecentEvents=e.Text+"\n"+RecentEvents;RecentJsonl=e.Jsonl+"\n"+RecentJsonl;if(RecentEvents.Length>500)RecentEvents=RecentEvents[..500];if(RecentJsonl.Length>1000)RecentJsonl=RecentJsonl[..1000];Trace("event:"+t);}

 void CaptureValidationRow(){
  string rec="none",why="all systems within thresholds";
  if(TempOutOfRange){rec="check_temperature";why="holding temperature outside FDA Food Code band";}
  else if(StationOverloaded&&OverloadCause=="equipment_constraint"){rec="address_equipment_issue";why=$"{ActiveOverloadStation} constrained by equipment outage";}
  else if(StationOverloaded&&OverloadCause=="staffing_gap"){rec="shift_staff";why=$"{ActiveOverloadStation} overloaded with open coverage gap";}
  else if(Prep<40&&!EnableRealIngredients){rec="prep_more";why="prep pool below half threshold";}
  else if(SanitizerDue){rec="change_sanitizer";why="sanitizer beyond change interval";}
  else if(StationOverloaded||DelayRisk){rec="monitor_queue";why=$"{BottleneckStation} backlog {Num(BottleneckBacklogMinutes)}m";}
  if(RecommendationRows.Length>0)RecommendationRows+=",";
  RecommendationRows+=$"{{\"checkpoint_time\":\"{SimEvent.BusinessDay}T{TimeText}:00Z\",\"daypart\":\"{Daypart}\",\"features\":{{\"open_tickets\":{Tickets},\"effective_crew\":{EffectiveCrew},\"coverage_used\":{CoverageUsed},\"prep\":{Prep},\"bottleneck_station\":\"{BottleneckStation}\",\"bottleneck_backlog_minutes\":{Num(BottleneckBacklogMinutes)},\"station_overloaded\":{JsonBool(StationOverloaded)},\"cooler_f\":{Num(CoolerTemp)},\"hot_hold_f\":{Num(HotHoldTemp)}}},\"expected_recommendation\":\"{rec}\",\"rationale\":\"{Json(why)}\"}}";
  if(AlertRows.Length>0)AlertRows+=",";
  var alert=AlertText;
  AlertRows+=$"{{\"checkpoint_time\":\"{SimEvent.BusinessDay}T{TimeText}:00Z\",\"daypart\":\"{Daypart}\",\"alert_text\":\"{Json(alert)}\",\"expected_alert\":{JsonBool(alert.StartsWith("ALERT"))},\"expected_warning\":{JsonBool(alert.StartsWith("Warning"))}}}";
 }
 void Trace(string reason){EnsureEquipment();TraceSeq++;traceSb.Append($"{{\"trace_id\":\"trace_{TraceSeq:000000}\",\"time\":\"{TimeText}\",\"minute\":{Num(Minute)},\"reason\":\"{Json(reason)}\",\"event_seq\":{EventSeq},\"scenario\":\"{Scenario}\",\"daypart\":\"{Daypart}\",\"speed\":{Num(TimeScale)},\"orders\":{Orders},\"completed\":{CompletedTickets},\"active_tickets\":{Tickets},\"channels\":{{\"drive_thru\":{DriveThru},\"lobby\":{FrontCounter},\"delivery\":{Delivery},\"mobile\":{Mobile}}},\"sales_total\":{Num(Sales)},\"labor_cost\":{Num(LaborCost)},\"labor_percent\":{Num(LaborPercent)},\"projected_sales_30\":{Num(ProjectedSalesThis30)},\"projected_labor_percent_30\":{Num(ProjectedLaborPercentThis30)},\"labor_target_percent\":{Num(LaborTargetPercent*100)},\"allowed_labor_dollars_30\":{Num(AllowedLaborDollarsThis30)},\"labor_dollars_variance_30\":{Num(LaborDollarsVarianceThis30)},\"allowed_labor_hours_30\":{Num(AllowedLaborHoursThis30)},\"scheduled_labor_hours_30\":{Num(ScheduledLaborHoursThis30)},\"labor_hours_variance_30\":{Num(LaborHoursVarianceThis30)},\"crew\":{Crew},\"effective_crew\":{EffectiveCrew},\"coverage_used\":{CoverageUsed},\"coverage_pool\":{CoveragePool},\"bottleneck_station\":\"{BottleneckStation}\",\"bottleneck_equipment\":\"{BottleneckEquipment}\",\"overload_cause\":\"{OverloadCause}\",\"station_overloaded\":{JsonBool(StationOverloaded)},\"open_tasks\":{OpenTaskCount},\"loads\":{{\"fryer\":{FryerLoad},\"grill\":{GrillLoad},\"assembly\":{AssemblyLoad},\"expo\":{ExpoLoad}}},\"capacities\":{{\"fryer\":{Num(FryerCapacity)},\"grill\":{Num(GrillCapacity)},\"assembly\":{Num(AssemblyCapacity)},\"expo\":{Num(ExpoCapacity+BeverageCapacity)},\"total\":{StaffCapacity}}},\"equipment_summary\":\"{Json(EquipmentSummary)}\",\"backlog_minutes\":{{\"fryer\":{Num(FryerBacklogMinutes)},\"grill\":{Num(GrillBacklogMinutes)},\"assembly\":{Num(AssemblyBacklogMinutes)},\"expo\":{Num(ExpoBacklogMinutes)},\"total\":{Num(KitchenBacklogMinutes)}}},\"inventory\":{{\"raw\":{Raw},\"prep\":{Prep},\"waste\":{Waste},\"main_protein\":{Num(inventory["main_protein"])},\"side_base\":{Num(inventory["side_base"])},\"drink_mix\":{Num(inventory["drink_mix"])},\"prep_pack\":{Num(inventory["prep_pack"])}}},\"temps\":{{\"cooler\":{Num(CoolerTemp)},\"hot_hold\":{Num(HotHoldTemp)}}},\"validation_status\":\"{ValidationStatus}\"}}\n");}

 public void RefreshValidation(string context="manual"){var issues=ValidationIssues();ValidationStatus=issues.Count==0?"OK":$"{issues.Count} issue(s)";var key=string.Join("|",issues);if(key==lastValidationKey)return;lastValidationKey=key;ValidationSeq++;validationSb.Append($"{ValidationSeq} {TimeText} {context} {ValidationStatus}");if(issues.Count>0)validationSb.Append(" | "+key);validationSb.Append('\n');}
 List<string> ValidationIssues(){EnsureEquipment();var issues=new List<string>();if(OrderChannelTotal!=Orders)issues.Add($"orders/channel mismatch orders={Orders} channels={OrderChannelTotal}");if(Tickets+CompletedTickets+AbandonedTickets!=Orders)issues.Add($"ticket reconciliation mismatch orders={Orders} active={Tickets} completed={CompletedTickets} abandoned={AbandonedTickets}");if(CoverageUsed>CoveragePool)issues.Add($"coverage exceeds pool used={CoverageUsed} pool={CoveragePool}");if(Math.Abs(Sales-SalesBucketTotal)>.01)issues.Add($"sales total mismatch total={Num(Sales)} buckets={Num(SalesBucketTotal)}");if(Sales>0&&Math.Abs(LaborPercent-(LaborCost/Sales*100.0))>.01)issues.Add("labor percent formula mismatch");foreach(var kv in inventory)if(kv.Value<0)issues.Add($"negative inventory {kv.Key}={Num(kv.Value)}");foreach(var e in equipment)if(EquipmentLoad(e)<-.01)issues.Add($"negative equipment load {e.Id}={Num(EquipmentLoad(e))}");if(EquipmentCount<10)issues.Add($"equipment model incomplete count={EquipmentCount}");if(StationOverloaded&&OverloadSeq<=0)issues.Add("station overloaded without overload ledger entry");return issues;}
 public string BuildValidationReport(){RefreshValidation("export");return $"validation_status={ValidationStatus}\nscenario={Scenario}\nseed={Seed}\nevents={EventSeq}\norders={Orders}\nchannel_total={OrderChannelTotal}\ncompleted_tickets={CompletedTickets}\nactive_tickets={Tickets}\nopen_tasks={OpenTaskCount}\nsales_total={Num(Sales)}\nsales_bucket_total={Num(SalesBucketTotal)}\nlabor_cost={Num(LaborCost)}\nlabor_percent={Num(LaborPercent)}\nprojected_labor_percent_30={Num(ProjectedLaborPercentThis30)}\nlabor_allowance_percent={Num(LaborTargetPercent*100)}\nallowed_labor_dollars_30={Num(AllowedLaborDollarsThis30)}\nprojected_labor_cost_30={Num(ProjectedLaborCostThis30)}\nlabor_dollars_variance_30={Num(LaborDollarsVarianceThis30)}\nallowed_labor_hours_30={Num(AllowedLaborHoursThis30)}\nscheduled_labor_hours_30={Num(ScheduledLaborHoursThis30)}\nlabor_hours_variance_30={Num(LaborHoursVarianceThis30)}\noverload_events={OverloadSeq}\nactive_overload={StationOverloaded}\nbottleneck_station={BottleneckStation}\nbottleneck_equipment={BottleneckEquipment}\nbottleneck_backlog_minutes={Num(BottleneckBacklogMinutes)}\nequipment_count={EquipmentCount}\nequipment_summary={EquipmentSummary}\nvalidation_ledger:\n{ValidationLedger}";}

 readonly Dictionary<string,double> invOpening=new(),invReceived=new(),invConsumed=new(),invWasted=new();
 // RS-IM-001: real per-ingredient ledger (opt-in). Off by default so every existing
 // (scenario,seed) replay stays byte-identical; Godot/harness set EnableRealIngredients
 // before the run when config/ingredients.json is available.
 public bool EnableRealIngredients;
 public IngredientCatalog? Catalog;
 IngredientLedger? ingredients;
 double lastIngredientTick=360;
 static readonly Dictionary<string,string> abstractToMenu=new(){
  {"fried_main","crispy_chicken_sandwich"},{"grilled_main","classic_burger"},{"side","fries"},{"beverage","fountain_drink"}};
 void EnsureIngredientLedger(){
  if(ingredients!=null||!EnableRealIngredients)return;
  ingredients=new IngredientLedger(Catalog??IngredientCatalog.Default());
  ingredients.OpenDay(Minute);
 }
 public bool RealIngredientsActive=>ingredients is { Active:true };
 public string IngredientLedgerJson=>ingredients?.ToLedgerJson(Exports.Provenance(this))??"{}";
 public double IngredientWasteCostUsd=>ingredients?.WasteCostUsd??0;
 public double IngredientWasteUnits=>ingredients?.WasteUnits??0;
 public string IngredientWasteByItemJson=>ingredients?.WasteByItemJson()??"{}";
 // RS-ST-001: daypart-shaped scheduled crew curve (derived from docs/06 daypart
 // ticket shares; exact heads operator_calibration_required). Every change is an
 // explicit staff.assignment.updated event so the staffing ledger reconciles:
 // scheduled - call_offs + replacements + breaks/returns = active coverage.
 public static int ScheduledCrewAt(int m){
  if(m<360)return 0;
  if(m<630)return 4;    // breakfast
  if(m<660)return 3;    // mid-morning trough
  if(m<705)return 6;    // lunch build
  if(m<855)return 7;    // lunch peak
  if(m<975)return 5;    // afternoon
  if(m<1245)return 7;   // dinner
  if(m<1320)return 5;   // dinner taper
  return 4;             // late night
 }
 bool InBreakWindow(int m)=>(m>=615&&m<680)||(m>=870&&m<960);
 string AddReason(int m)=>m<=361?"shift_start":(m>=660&&m<860)||(m>=975&&m<1050)?"rush_support":"manager_adjustment";
 string DropReason(int m)=>m>=1430?"shift_end":"manager_adjustment";
 void ApplySchedule(){
  var m=(int)Minute;
  if(m==lastScheduleMinute)return;
  lastScheduleMinute=m;
  if(!AutoSchedule)return;
  // Break return
  if(breakReturnMinute>=0&&m>=breakReturnMinute){
   if(CrewOnBreak>0){CrewOnBreak--;BreaksTaken++;RecordStaffing("crew_member",$"crew_sched_break_{BreaksTaken:00}","break_room","kitchen","break_coverage");}
   breakReturnMinute=-1;
  }
  // staffing_call_off: the first lunch-build add no-shows; a replacement is reached for 14:00.
  if(Scenario=="staffing_call_off"&&!callOffFired&&m>=660){
   callOffFired=true;callOffDeficitUntil=840;CallOffs++;
   RecordStaffing("crew_member",$"crew_sched_{++schedSeq:00}","kitchen",null,"call_off");
   RaiseDecision("calloff","Lunch crew member called off","Run short through lunch","Offer overtime cover (+$33 premium)",defaultA:true);
  }
  var target=ScheduledCrewAt(m);
  if(callOffDeficitUntil>0&&m<callOffDeficitUntil)target=Math.Max(1,target-1);
  if(callOffDeficitUntil>0&&m>=callOffDeficitUntil&&!replacementCalled&&Crew<ScheduledCrewAt(m)){
   replacementCalled=true;Crew++;
   RecordStaffing("crew_member",$"crew_sched_{++schedSeq:00}",null,"kitchen","rush_support");
  }
  while(Crew<target){Crew++;RecordStaffing("crew_member",$"crew_sched_{++schedSeq:00}",null,"kitchen",AddReason(m));}
  while(Crew>target&&EffectiveCrew>1){Crew--;if(CrewOnBreak>Crew)CrewOnBreak=Crew;RecordStaffing("crew_member",$"crew_sched_{++schedSeq:00}","kitchen",null,DropReason(m));}
  // Staggered off-peak breaks (break compliance is a pass/fail dimension).
  if(InBreakWindow(m)&&CrewOnBreak==0&&BreaksTaken<4&&m%35==20&&EffectiveCrew>3){
   CrewOnBreak++;breakReturnMinute=m+30;
   RecordStaffing("crew_member",$"crew_sched_break_{BreaksTaken+1:00}","kitchen","break_room","break_coverage");
  }
  AutoPlanCoverage();
 }
 // Demand-weighted coverage from the live pool. Emits one assignment event per
 // changed unit (a handful of transitions per day, not per-minute spam).
 void AutoPlanCoverage(){
  var pool=CoveragePool;
  var plan=new int[5]; // kitchen, fryer, drive, counter, prep
  string[] names={"grill","fryer","drive_thru","lobby","prep"};
  int[] firstPass={2,1,0,3,4};            // drive, fryer, kitchen, counter, prep
  int[] cycle={2,0,1,3,2,0,4,3};          // extras favor drive/kitchen
  var n=pool;var i=0;
  while(n>0&&i<firstPass.Length){plan[firstPass[i]]++;n--;i++;}
  i=0;while(n>0){plan[cycle[i%cycle.Length]]++;n--;i++;}
  ApplyCoverage(ref KitchenCoverage,plan[0],names[0]);
  ApplyCoverage(ref FryerCoverage,plan[1],names[1]);
  ApplyCoverage(ref DriveCoverage,plan[2],names[2]);
  ApplyCoverage(ref CounterCoverage,plan[3],names[3]);
  ApplyCoverage(ref PrepCoverage,plan[4],names[4]);
 }
 void ApplyCoverage(ref int field,int want,string station){
  while(field<want){field++;RecordStaffing("coverage_unit","coverage_sched",null,station,"manager_adjustment");}
  while(field>want){field--;RecordStaffing("coverage_unit","coverage_sched",station,null,"manager_adjustment");}
 }
 public void EnableAutoSchedule(){AutoSchedule=true;lastScheduleMinute=-1;}

 // ===================== RS-HQ-001 / RS-IN-001 / RS-GP-001 =====================
 int hqTick,lastSysMinute=-1;double truckDeferUntil;
 void HqSystems(){
  hqTick++;
  if(hqTick%5==0)foreach(var t in activeTickets)if(t.Pending.Count>0)TryTakeItems(t);
  if(hqTick%15==0){ManagePans();AbandonScan();}
  var m=(int)Minute;
  if(m!=lastSysMinute){lastSysMinute=m;AgePans();WearAndFailures();InspectionTick(m);ExpireDecisions();AutoCompliance();SupplyWatch(m);}
 }
 // Routine compliance is standard crew ops when running full-auto; in Manager
 // Mode these are the player's responsibility (and the inspector will notice).
 void AutoCompliance(){
  if(ManagerMode)return;
  if(TempCheckAge>=55)CheckTemps();
  if(SanitizerAge>=110)ChangeSanitizer();
 }
 // RS-GP-001: an unforecast surge can outrun the day's supply order. A competent
 // manager reacts with a costed emergency run (sister store / cash-and-carry).
 int supplyRunsUsed;double supplyArrivalMin=-1;bool supplyDecisionOpen;
 public double SupplyRunCost;
 void SupplyWatch(int m){
  if(supplyArrivalMin>0&&Minute>=supplyArrivalMin){
   supplyArrivalMin=-1;
   Receive("main_protein",300);Receive("side_base",150);Receive("drink_mix",40);
   Raw+=200;   // prep raw material — the thing surge days actually exhaust first
   Trace("emergency_supply_arrived");
  }
  if(supplyRunsUsed>=2||supplyDecisionOpen||supplyArrivalMin>0||m>1350)return;
  if(inventory.GetValueOrDefault("main_protein")<80||inventory.GetValueOrDefault("side_base")<50||Raw<60){
   supplyDecisionOpen=true;
   RaiseDecision("supply","Product running out before close","Emergency supply run ($120, arrives in 45 min)","Ride it out",defaultA:true);
  }
 }
 double FamilyPerOrder(string family)=>family=="fries"?0.65:0.6; // mains 1.2/order split 50/50
 void ManagePans(){
  if(!AutoHold)return;
  foreach(var pan in pans.Values){
   var demand=RatePerSimMinute()*FamilyPerOrder(pan.Family);
   var par=Math.Min(pan.Capacity,demand*pan.LimitMin*0.7);   // ORIG
   if(par<1.5)continue;                                      // near-zero demand: cook to order
   var want=par-pan.Level-pan.InFlightQty;
   // Batch size floor scales with demand x cycle time: a 2u load still costs a
   // full cycle, so tiny batches at peak quarter the station's throughput and
   // spiral into permanent stockout. Off-peak the floor stays at 2u (low waste);
   // at peak it reaches the full batch (full throughput).
   var minQ=Math.Min(pan.BatchQty,Math.Max(2,demand*(pan.CycleSeconds/60.0)*1.2));
   while(want>0.5&&RawAvailable(pan)){
    var q=Math.Clamp(Math.Max(want,minQ),minQ,pan.BatchQty);
    QueueBatch(pan,q);want-=q;
   }
  }
 }
 bool RawAvailable(HoldPan pan){foreach(var (c,q) in pan.Raw)if(inventory.GetValueOrDefault(c)<q*pan.BatchQty)return false;return true;}
 void QueueBatch(HoldPan pan,double qty=0){
  if(qty<=0)qty=pan.BatchQty;
  var task=new EquipTask{Id=++TaskSeq,TicketId=0,OrderId="hold",ItemId=pan.Family,Phase="batch_cook",Station=pan.Station,Family=pan.Family,WorkSeconds=pan.CycleSeconds,Remaining=pan.CycleSeconds,DependencyId=0,BatchUnits=qty};
  var unit=SelectEquipment(pan.Family);
  if(unit==null)return;
  task.Queued=true;task.EquipmentId=unit.Id;
  unit.Queue.Add(task);unit.Assigned+=task.WorkSeconds;unit.Tasks++;
  pan.InFlight++;pan.InFlightQty+=qty;
  taskSb.Append($"{TimeText} queued task_{task.Id:000000} order hold item {pan.Family} phase batch_cook equipment {unit.Id}\n");
 }
 public void DropBatch(string family){if(pans.TryGetValue(family,out var pan)&&RawAvailable(pan)&&pan.Level+pan.InFlight*pan.BatchQty<pan.Capacity)QueueBatch(pan);}
 void AgePans(){
  foreach(var pan in pans.Values){
   while(pan.Batches.Count>0&&Minute-pan.Batches[0].At>pan.LimitMin){
    var q=pan.Batches[0].Qty;pan.Batches.RemoveAt(0);
    RecordWasteComponent(pan.Cooked,q,"holding_time_exceeded",pan.Station);
    lastHoldWasteMinute=(int)Minute;
   }
  }
 }
 int lastHoldWasteMinute=-999;
 void RecordWasteComponent(string comp,double u,string reason,string station){
  Waste+=(int)Math.Round(u);WasteSeq++;
  u=Math.Min(u,inventory.GetValueOrDefault(comp));   // waste cannot exceed stock
  inventory[comp]=inventory.GetValueOrDefault(comp)-u;
  invWasted[comp]=invWasted.GetValueOrDefault(comp)+u;
  var wl=$"{WasteSeq} {TimeText} {reason} {Num(u)}u {comp} station {station}\n";
  wasteSb.Append(wl);wasteRecent=wl+wasteRecent;if(wasteRecent.Length>900)wasteRecent=wasteRecent[..900];
  Emit("waste.recorded",$"{{\"waste_id\":\"waste_{WasteSeq:000000}\",\"inventory_item_id\":\"{comp}\",\"quantity\":{Num(u)},\"unit\":\"units\",\"reason\":\"{reason}\",\"station_id\":\"{station}\"}}");
 }
 string PatienceChannel(string ch)=>ch;
 int PatienceFor(string ch)=>ch=="drive_thru"?SimConfig.PatienceDtSec:ch=="lobby"?SimConfig.PatienceLobbySec:ch=="mobile"?SimConfig.PatienceMobileSec:SimConfig.PatienceDeliverySec;
 public event Action<string,string>? TicketAbandonedEvt;
 void AbandonScan(){
  for(var i=activeTickets.Count-1;i>=0;i--){
   var t=activeTickets[i];
   var sec=(int)((Minute-t.Created+1440)%1440*60);
   if(sec<=PatienceFor(t.Channel))continue;
   AbandonedTickets++;LostSales+=t.Total;
   SalesTotal-=t.Total;sales30[(int)(Minute/30)%48]-=t.Total;
   abandonedTicketIds.Add(t.Id);t.Abandoned=true;
   Emit("ticket.updated",$"{{\"ticket_id\":\"tkt_{t.Id:000000}\",\"order_id\":\"{t.OrderId}\",\"status\":\"abandoned\",\"queue_seconds\":{sec},\"station_id\":\"{FirstOpenStation(t)}\"}}");
   TicketAbandonedEvt?.Invoke(t.Channel,t.OrderId);
   activeTickets.RemoveAt(i);
  }
 }
 // ---- RS-IN-001: equipment wear, failures, maintenance, health inspection ----
 double WearK(string family)=>family=="fried_main"||family=="fries"?SimConfig.WearKFryer:family=="grilled_main"?SimConfig.WearKGrill:SimConfig.WearKOther;
 double Condition(EquipmentUnit e)=>Math.Max(0,100-(e.Processed-e.MaintBase)/WearK(e.Family));
 void WearAndFailures(){
  foreach(var e in equipment){
   if(e.FailedDown||Condition(e)>10)continue;
   e.FailedDown=true;
   RecordWasteComponent("prep_pack",2,"equipment_issue",e.Station);
   RaiseDecision("breakdown",$"{e.Id} just failed (worn out)","Call tech ($150, back in 30 min)","Limp without it",defaultA:true,context:e.Id);
  }
 }
 public void MaintainWorstEquipment(){
  EquipmentUnit? worst=null;var wc=101.0;
  foreach(var e in equipment){var c=Condition(e);if(c<wc){wc=c;worst=e;}}
  if(worst==null)return;
  MaintSpend+=SimConfig.MaintenanceCost;
  worst.MaintBase=worst.Processed;worst.MaintUntil=Minute+10;worst.FailedDown=false;
  Trace($"maintenance_{worst.Id}");
 }
 public string WorstEquipment{get{EquipmentUnit? w=null;var wc=101.0;foreach(var e in equipment){var c=Condition(e);if(c<wc){wc=c;w=e;}}return w==null?"-":$"{w.Id} {Num(wc)}%";}}
 public double WorstEquipmentCondition{get{var wc=100.0;foreach(var e in equipment)wc=Math.Min(wc,Condition(e));return wc;}}
 void InspectionTick(int m){
  if(inspectionMinute<0)inspectionMinute=630+(int)(((long)Seed*131)%360);
  if(inspectionDone||m<inspectionMinute)return;
  inspectionDone=true;
  var score=100.0;var notes=new List<string>();
  if(TempOutOfRange){score-=25;notes.Add("holding temps out of range");}
  if(TempCheckAge>120){score-=15;notes.Add("temp log stale");}
  if(SanitizerAge>120){score-=15;notes.Add("sanitizer overdue");}
  if(SanitationTasks==0&&m>700){score-=10;notes.Add("no sanitation tasks logged");}
  if(m-lastHoldWasteMinute<30&&!EnableRealIngredients){score-=10;notes.Add("expired hold product observed");}
  if(StationOverloaded){score-=5;notes.Add("line out of control");}
  InspectionScore=Math.Max(0,score);
  InspectionNotes=notes.Count==0?"clean inspection":string.Join("; ",notes);
  Trace("health_inspection");
 }
 public bool InspectorIncoming=>!inspectionDone&&inspectionMinute>0&&Minute>=inspectionMinute-15;
 // ---- RS-GP-001: decision interrupts (auto modes resolve with the default) ----
 void RaiseDecision(string kind,string title,string a,string b,bool defaultA,string context=""){
  var d=new Decision{Id=++decisionSeq,Kind=kind,Title=title,OptionA=a,OptionB=b,DefaultA=defaultA,DeadlineMinute=Minute+3};
  Decisions.Add(d);
  Trace($"decision_raised_{kind}");
  decisionContext[d.Id]=context;
  if(!ManagerMode)ResolveDecision(d.Id,defaultA);
 }
 readonly Dictionary<int,string> decisionContext=new();
 void ExpireDecisions(){
  for(var i=Decisions.Count-1;i>=0;i--)if(Minute>=Decisions[i].DeadlineMinute)ResolveDecision(Decisions[i].Id,Decisions[i].DefaultA);
 }
 public void ResolveDecision(int id,bool optionA){
  Decision? d=null;foreach(var x in Decisions)if(x.Id==id){d=x;break;}
  if(d==null)return;
  Decisions.Remove(d);
  decisionContext.TryGetValue(d.Id,out var ctx);decisionContext.Remove(d.Id);
  switch(d.Kind){
   case "truck":
    if(optionA)DoTruck();else truckDeferUntil=Minute+60;
    break;
   case "calloff":
    if(!optionA){Crew++;OvertimePremium+=4*16*0.5;callOffDeficitUntil=-1;RecordStaffing("crew_member","crew_overtime",null,"kitchen","rush_support");}
    break;
   case "breakdown":
    foreach(var e in equipment)if(e.Id==ctx){
     if(optionA){MaintSpend+=SimConfig.TechCalloutCost;e.MaintBase=e.Processed;e.MaintUntil=Minute+30;e.FailedDown=false;}
     break;
    }
    break;
   case "complaint":
    if(!optionA){CompCost+=8;ComplaintsComped++;satSum+=25;}
    break;
   case "supply":
    supplyDecisionOpen=false;
    if(optionA){supplyRunsUsed++;SupplyRunCost+=120;supplyArrivalMin=Minute+45;Trace("emergency_supply_ordered");}
    break;
  }
  Trace($"decision_resolved_{d.Kind}_{(optionA?"a":"b")}");
 }
 public void ToggleManagerMode(){ManagerMode=!ManagerMode;AutoHold=!ManagerMode;}
 public double InventoryLevel(string c)=>inventory.GetValueOrDefault(c);
 public double HoldLevel(string f)=>pans.TryGetValue(f,out var p)?p.Level:0;
 public double HoldCapacity(string f)=>pans.TryGetValue(f,out var p)?p.Capacity:0;
 public int HoldInFlight(string f)=>pans.TryGetValue(f,out var p)?p.InFlight:0;
 public double HoldOldestAgeMin(string f)=>pans.TryGetValue(f,out var p)&&p.Batches.Count>0?Math.Max(0,Minute-p.Batches[0].At):0;
 public double Csat=>satCount==0?100:Math.Clamp(satSum/satCount,0,100);
 public double SalesTargetToday=>10100*(Scenario=="slow_day"?0.75:Scenario=="weather_disruption"?0.65:Scenario=="rush_day"?1.35:Scenario=="multi_rush_condition"?1.2:Scenario=="holiday_pattern"?1.1:1.0);
 public double SalesPacePercent{get{var span=Math.Max(1,Minute-360);var expected=SalesTargetToday*span/1080.0;return expected<=0?100:Sales/expected*100;}}

 void ReceiveTruckIfDue(){
  if(truckReceived||Minute<Math.Max(840,truckDeferUntil))return; // 14:00 scheduled delivery
  if(ManagerMode&&!truckDecisionRaised){
   truckDecisionRaised=true;
   RaiseDecision("truck","Supply truck is here","Receive now (prep tied up 20 min)","Defer 60 min",defaultA:true);
   return;
  }
  if(!truckDecisionRaised)DoTruck();
 }
 void DoTruck(){
  if(truckReceived)return;
  truckReceived=true;
  Raw+=420;
  Receive("main_protein",480);Receive("side_base",300);Receive("drink_mix",80);
  Trace("inventory_truck_received");
 }
 void Receive(string k,double q){inventory[k]=inventory.GetValueOrDefault(k)+q;invReceived[k]=invReceived.GetValueOrDefault(k)+q;}
 string RecoveryReason=>Scenario=="equipment_failure"&&Minute>=810&&Minute<900?"equipment_restored":Minute>=1380?"end_of_shift_queue_clear":"queue_cleared";
 public string InventoryLedgerJson{get{
  var rows=new List<string>();
  foreach(var kv in inventory){
   var k=kv.Key;
   var opening=invOpening.GetValueOrDefault(k);
   var received=invReceived.GetValueOrDefault(k);
   var consumed=invConsumed.GetValueOrDefault(k);
   var wasted=invWasted.GetValueOrDefault(k);
   var closing=kv.Value;
   var ok=Math.Abs(opening+received-consumed-wasted-closing)<0.01;
   rows.Add($"{{\"inventory_item_id\":\"{k}\",\"unit\":\"units\",\"opening\":{Num(opening)},\"prep_confirmed_or_received\":{Num(received)},\"consumed_item_taken\":{Num(consumed)},\"waste_recorded\":{Num(wasted)},\"approved_adjustments\":0,\"closing\":{Num(closing)},\"reconciles\":{JsonBool(ok)}}}");
  }
  return "["+string.Join(",",rows)+"]";
 }}
 public string ActiveOverloadStation=>activeOverloadStation;
 void EnsureEquipment(){if(equipmentReady)return;equipmentReady=true;AddEquipment("fryer_fries_1","fryer","fries",70);AddEquipment("fryer_fries_2","fryer","fries",70);AddEquipment("fryer_main_1","fryer","fried_main",70);AddEquipment("fryer_main_2","fryer","fried_main",70);AddEquipment("burger_press_1","grill","grilled_main",95);AddEquipment("soda_1","beverage","beverage",45);AddEquipment("soda_2","beverage","beverage",45);AddEquipment("soda_3","beverage","beverage",45);AddEquipment("soda_4","beverage","beverage",45);AddEquipment("assembly_rail_1","assembly","assembly",60);AddEquipment("assembly_rail_2","assembly","assembly",60);AddEquipment("expo_lane_1","expo","expo",65);AddEquipment("expo_lane_2","expo","expo",65);AddEquipment("dt_window_1","drive_thru","dt_service",60);AddEquipment("dt_window_2","drive_thru","dt_service",60);AddEquipment("dt_window_3","drive_thru","dt_service",60);AddEquipment("counter_pos_1","lobby","counter_service",60);AddEquipment("counter_pos_2","lobby","counter_service",60);AddEquipment("counter_pos_3","lobby","counter_service",60);AddEquipment("pickup_shelf_1","pickup","pickup_service",80);// RS-HQ-001: mains/fries are cook-to-hold (full-cycle latency lives in the pan,
  // draws take seconds). Fries keep a 15 s scoop at the fryer; beverage stays made-to-order.
  items["fried_main"]=new ItemSpec{Id="fried_main",Family="fried_main",Station="fryer",CookSeconds=0,HoldFamily="fried_main",HoldMinutes=SimConfig.HoldLimitFriedMin,AssemblySeconds=44,ExpoSeconds=20,Price=6.25};
  items["grilled_main"]=new ItemSpec{Id="grilled_main",Family="grilled_main",Station="grill",CookSeconds=0,HoldFamily="grilled_main",HoldMinutes=SimConfig.HoldLimitGrilledMin,AssemblySeconds=44,ExpoSeconds=20,Price=6.35};
  items["side"]=new ItemSpec{Id="side",Family="fries",Station="fryer",CookSeconds=0,HoldFamily="fries",HoldMinutes=SimConfig.HoldLimitFriesMin,AssemblySeconds=18,ExpoSeconds=7,Price=2.75};  // bag at assembly: scoops must not queue behind 180s vat cycles
  items["beverage"]=new ItemSpec{Id="beverage",Family="beverage",Station="beverage",CookSeconds=25,HoldMinutes=0,AssemblySeconds=0,ExpoSeconds=6,Price=2.35};
  pans["fried_main"]=new HoldPan{Family="fried_main",Station="fryer",Cooked="cooked_fried_main",Raw=new[]{("main_protein",1.0)},CycleSeconds=330,BatchQty=8,Capacity=16,LimitMin=SimConfig.HoldLimitFriedMin};
  pans["grilled_main"]=new HoldPan{Family="grilled_main",Station="grill",Cooked="cooked_grilled_main",Raw=new[]{("main_protein",1.0)},CycleSeconds=150,BatchQty=6,Capacity=12,LimitMin=SimConfig.HoldLimitGrilledMin};
  pans["fries"]=new HoldPan{Family="fries",Station="fryer",Cooked="cooked_fries",Raw=new[]{("side_base",1.0)},CycleSeconds=180,BatchQty=4,Capacity=12,LimitMin=SimConfig.HoldLimitFriesMin};
  foreach(var pan in pans.Values){
   inventory[pan.Cooked]=pan.BatchQty;            // opening hold = one fresh batch
   pan.Batches.Add((pan.BatchQty,360));
  }
  prepBatches.Add((260,360));                      // opening prep batch == opening inventory
 }
 void AddEquipment(string id,string station,string family,double cap){equipment.Add(new EquipmentUnit{Id=id,Station=station,Family=family,BaseCapacity=cap});}

 double RatePerSimMinute(){if(Minute<360||Minute>=SimConfig.LastOrderMinute)return 0;var daily=Scenario=="slow_day"?620:Scenario=="rush_day"?1180:Scenario=="weather_disruption"?760:Scenario=="local_event_surge"?1050:Scenario=="school_event_surge"?980:Scenario=="holiday_pattern"?840:Scenario=="multi_rush_condition"?1120:920;return daily*DaypartShare()/DaypartMinutes()*Curve()*ScenarioMultiplier()*ReputationDemandMultiplier;}
 double ScenarioMultiplier()=>Scenario=="rush_day"?1.10:Scenario=="weather_disruption"?.86:Scenario=="equipment_failure"?.95:Scenario=="staffing_call_off"?.97:Scenario=="multi_rush_condition"?1.08:1.0;
 double DaypartShare()=>Daypart=="breakfast"?.18:Daypart=="mid_morning"?.07:Daypart=="lunch"?.30:Daypart=="afternoon"?.10:Daypart=="dinner"?.30:.05;
 double DaypartMinutes()=>Daypart=="breakfast"?240:Daypart=="mid_morning"?90:Daypart=="lunch"?150:Daypart=="afternoon"?150:Daypart=="dinner"?240:210;
 double Curve(){var peak=Daypart=="breakfast"?480:Daypart=="mid_morning"?615:Daypart=="lunch"?750:Daypart=="afternoon"?930:Daypart=="dinner"?1095:1260;var start=Daypart=="breakfast"?360:Daypart=="mid_morning"?600:Daypart=="lunch"?690:Daypart=="afternoon"?840:Daypart=="dinner"?990:1230;var end=Daypart=="breakfast"?600:Daypart=="mid_morning"?690:Daypart=="lunch"?840:Daypart=="afternoon"?990:Daypart=="dinner"?1230:1440;var half=Math.Max(1,Math.Max(peak-start,end-peak));var shape=1-Math.Min(1,Math.Abs(Minute-peak)/half);return .75+.5*shape;}
 double SumArray(double[] xs){double n=0;foreach(var x in xs)n+=x;return n;}
 double StaffScenarioMultiplier=>Scenario=="multi_rush_condition"?.88:Scenario=="holiday_pattern"?.92:1.0; // call_off is causal via the schedule now (RS-ST-001)
 double CoverageFactor(string station)=>station=="fryer"?(FryerCoverage<=0?0:Math.Min(1.25,.35+.325*FryerCoverage)):station=="grill"?(KitchenCoverage<=0?0:Math.Min(1.2,.50+.35*KitchenCoverage)):station=="beverage"?(DriveCoverage+CounterCoverage<=0?0:Math.Min(1.25,.25+.20*(DriveCoverage+CounterCoverage))):station=="assembly"?(KitchenCoverage+CounterCoverage<=0?0:Math.Min(1.2,.30+.30*(KitchenCoverage+CounterCoverage))):station=="drive_thru"?(DriveCoverage<=0?0:Math.Min(1.2,.45+.40*DriveCoverage)):station=="lobby"?(CounterCoverage<=0?0:Math.Min(1.2,.45+.40*CounterCoverage)):station=="pickup"?Math.Min(1.0,.40+.20*(DriveCoverage+CounterCoverage)):Math.Min(1.2,.25+.20*(DriveCoverage+CounterCoverage+KitchenCoverage));
 bool EquipmentAvailable(EquipmentUnit e)=>!e.FailedDown&&Minute>=e.MaintUntil&&!(Scenario=="equipment_failure"&&Minute>=660&&Minute<810&&(e.Id=="fryer_main_2"||e.Id=="soda_4"));
 // RS-CF-001: aggregate behavior-profile pace + late-shift fatigue; RS-IN-001: worn
 // equipment loses a fifth of its throughput until maintained.
 double EquipmentCapacity(EquipmentUnit e)=>EquipmentAvailable(e)?e.BaseCapacity*CoverageFactor(e.Station)*StaffScenarioMultiplier*SimConfig.CrewPace(Seed,e.Station)*SimConfig.Fatigue(ShiftMinutes)*(Condition(e)<40?0.8:1.0):0;
 double EquipmentLoad(EquipmentUnit e){double n=e.Active==null?0:Math.Max(0,e.Active.Remaining);foreach(var t in e.Queue)n+=Math.Max(0,t.Remaining);return n;}
 double EquipmentStationCapacity(string station){EnsureEquipment();double n=0;foreach(var e in equipment)if(e.Station==station)n+=EquipmentCapacity(e);return n;}
 double EquipmentStationLoad(string station){EnsureEquipment();double n=0;foreach(var e in equipment)if(e.Station==station)n+=EquipmentLoad(e);return n;}
 public double GrillCapacity=>EquipmentStationCapacity("grill");public double FryerCapacity=>EquipmentStationCapacity("fryer");public double AssemblyCapacity=>EquipmentStationCapacity("assembly");public double BeverageCapacity=>EquipmentStationCapacity("beverage");public double ExpoCapacity=>EquipmentStationCapacity("expo");
 public double Sales=>SalesTotal;public double SalesBucketTotal=>SumArray(sales30);public double DisplayWasteUnits=>RealIngredientsActive?IngredientWasteUnits:Waste;public double DisplayWasteCost=>RealIngredientsActive?IngredientWasteCostUsd:WasteCost;public double WasteCost=>Waste*0.75;public double FoodCostPercent=>Sales<=0?0:DisplayWasteCost/Sales*100;
 public double LaborHourly=>Crew*16+Lead*18+ShiftMgr*22+AsstMgr*28+RestMgr*35;public double LaborPercent=>Sales<=0?0:LaborCost/Sales*100;
 public int TotalOnClock=>Crew+Lead+ShiftMgr+AsstMgr+RestMgr;public int EffectiveCrew=>Math.Max(0,Crew-CrewOnBreak);public int CoveragePool=>Math.Max(0,TotalOnClock-CrewOnBreak);public int CoverageUsed=>KitchenCoverage+FryerCoverage+DriveCoverage+CounterCoverage+PrepCoverage;public int CoverageOpen=>Math.Max(0,CoveragePool-CoverageUsed);public int AssignableLabor=>CoveragePool;public int StationAssigned=>CoverageUsed;public int AvailableLabor=>CoverageOpen;public int OrderChannelTotal=>DriveThru+FrontCounter+Delivery+Mobile;
 public int PaidHeadcount=>TotalOnClock;public double AverageLaborRate=>PaidHeadcount<=0?16.0:LaborHourly/PaidHeadcount;public double LaborTargetPercent=>ProjectedSalesThis30<150?0.35:ProjectedSalesThis30<300?0.32:ProjectedSalesThis30<600?0.30:0.28;public double HalfHourElapsedMinutes=>Math.Max(1.0,Minute%30.0);public double HalfHourProgress=>Math.Min(1.0,HalfHourElapsedMinutes/30.0);public double ProjectedSalesThis30=>SalesThis30/HalfHourProgress;public double ProjectedLaborCostThis30=>LaborHourly*0.5;public double ProjectedLaborPercentThis30=>ProjectedSalesThis30<=0?0:ProjectedLaborCostThis30/ProjectedSalesThis30*100.0;public double AllowedLaborDollarsThis30=>ProjectedSalesThis30*LaborTargetPercent;public double LaborDollarsVarianceThis30=>AllowedLaborDollarsThis30-ProjectedLaborCostThis30;public double AllowedLaborHoursThis30=>AverageLaborRate<=0?0:AllowedLaborDollarsThis30/AverageLaborRate;public double ScheduledLaborHoursThis30=>PaidHeadcount*0.5;public double LaborHoursVarianceThis30=>AllowedLaborHoursThis30-ScheduledLaborHoursThis30;
 public bool BreakDue=>ShiftMinutes>240&&BreaksTaken==0;public int StaffCapacity=>(int)(GrillCapacity+FryerCapacity+AssemblyCapacity+BeverageCapacity+ExpoCapacity);public int NetKitchenLoad=>KitchenLoad-StaffCapacity;public double KitchenBacklogMinutes=>StaffCapacity<=0?(Tickets>0?999:0):KitchenLoad/(double)StaffCapacity;public double FryerBacklogMinutes=>FryerCapacity<=0?(FryerLoad>0?999:0):FryerLoad/FryerCapacity;public double GrillBacklogMinutes=>GrillCapacity<=0?(GrillLoad>0?999:0):GrillLoad/GrillCapacity;public double AssemblyBacklogMinutes=>AssemblyCapacity<=0?(AssemblyLoad>0?999:0):AssemblyLoad/AssemblyCapacity;public double ExpoBacklogMinutes=>(ExpoCapacity+BeverageCapacity)<=0?(ExpoLoad>0?999:0):ExpoLoad/(ExpoCapacity+BeverageCapacity);
 public int PrepQuality=>PrepAge>=30?0:100-(int)(PrepAge*3);public int Tickets=>activeTickets.Count;public int FryerLoad=>(int)Math.Ceiling(EquipmentStationLoad("fryer"));public int GrillLoad=>(int)Math.Ceiling(EquipmentStationLoad("grill"));public int AssemblyLoad=>(int)Math.Ceiling(EquipmentStationLoad("assembly"));public int ExpoLoad=>(int)Math.Ceiling(EquipmentStationLoad("expo")+EquipmentStationLoad("beverage"));public int KitchenLoad=>FryerLoad+GrillLoad+AssemblyLoad+ExpoLoad;public bool DelayRisk=>Tickets>30||(StaffCapacity<=0&&Tickets>0)||KitchenBacklogMinutes>10||FryerBacklogMinutes>8||GrillBacklogMinutes>8||AssemblyBacklogMinutes>7||ExpoBacklogMinutes>7;
 public string BottleneckStation{get{var station="fryer";var best=FryerBacklogMinutes;if(GrillBacklogMinutes>best){station="grill";best=GrillBacklogMinutes;}if(AssemblyBacklogMinutes>best){station="assembly";best=AssemblyBacklogMinutes;}if(ExpoBacklogMinutes>best){station="expo";best=ExpoBacklogMinutes;}return station;}}
 public double BottleneckBacklogMinutes=>StationBacklog(BottleneckStation);public string BottleneckEquipment{get{EnsureEquipment();string id="none";var best=-1.0;foreach(var e in equipment){var ratio=EquipmentLoad(e)/Math.Max(1,EquipmentCapacity(e));if(ratio>best){best=ratio;id=e.Id;}}return id;}}
 public int EquipmentCount{get{EnsureEquipment();return equipment.Count;}}public int OpenTaskCount{get{var n=0;foreach(var t in activeTickets)foreach(var task in t.Tasks)if(!task.Done)n++;return n;}}
 public string EquipmentSummary{get{EnsureEquipment();var parts=new List<string>();foreach(var e in equipment)parts.Add($"{e.Id}:{Num(EquipmentLoad(e))}/{Num(EquipmentCapacity(e))}");return string.Join(";",parts);}}
 public string ItemCatalogJson=>"[{\"item_id\":\"fried_main\",\"equipment\":\"fryer_main_1|fryer_main_2\",\"cook_seconds\":330,\"hold_minutes\":20,\"dependency\":\"cook->assembly->expo\",\"events\":\"item.taken->item.completed\"},{\"item_id\":\"grilled_main\",\"equipment\":\"burger_press_1\",\"cook_seconds\":150,\"hold_minutes\":15,\"dependency\":\"cook->assembly->expo\",\"events\":\"item.taken->item.completed\"},{\"item_id\":\"side\",\"equipment\":\"fryer_fries_1|fryer_fries_2\",\"cook_seconds\":180,\"hold_minutes\":7,\"dependency\":\"cook->assembly->expo\",\"events\":\"item.taken->item.completed\"},{\"item_id\":\"beverage\",\"equipment\":\"soda_1|soda_2|soda_3|soda_4\",\"cook_seconds\":22,\"hold_minutes\":0,\"dependency\":\"pour->expo\",\"events\":\"item.taken->item.completed\"}]";
 double StationLoad(string station)=>station=="fryer"?FryerLoad:station=="grill"?GrillLoad:station=="assembly"?AssemblyLoad:ExpoLoad;double StationCapacity(string station)=>station=="fryer"?FryerCapacity:station=="grill"?GrillCapacity:station=="assembly"?AssemblyCapacity:ExpoCapacity+BeverageCapacity;double StationBacklog(string station){var cap=StationCapacity(station);var load=StationLoad(station);return cap<=0?(load>0?999:0):load/cap;}
 public bool SanitizerDue=>SanitizerAge>=120;public bool TempCheckDue=>TempCheckAge>=120;public bool TempOutOfRange=>CoolerTemp>41||HotHoldTemp<135;public string HoldAlert{get{foreach(var pan in pans.Values)if(pan.Level<=0&&pan.InFlight==0)return $"ALERT: {pan.Family} hold empty";return "";}}
 public string AlertText=>ShiftEnded?"Shift closed":Decisions.Count>0?$"DECISION NEEDED: {Decisions[0].Title}":WorstEquipmentCondition<=10?$"ALERT: equipment down ({WorstEquipment})":HoldAlert!=""?HoldAlert:InspectorIncoming?"Warning: health inspector arriving":StationOverloaded?$"ALERT: {activeOverloadStation} overloaded | {OverloadCause} | {StationBacklog(activeOverloadStation):0.0}m backlog | equipment {BottleneckEquipment}":TempOutOfRange?"ALERT: temperature out of range":TempCheckDue?"Warning: temperature check due":SanitizerDue?"Warning: sanitizer change due":BreakDue?"Warning: break due":DelayRisk?$"Warning: {BottleneckStation} delay risk | {BottleneckBacklogMinutes:0.0}m backlog | equipment {BottleneckEquipment}":PrepQuality<50&&!EnableRealIngredients?"Warning: prep quality low":Prep<80&&!EnableRealIngredients?"Warning: prep low":"Alerts: none";public int DtSos{get{var m=MeasuredSos("drive_thru");return m>=0?(int)m:(int)Math.Min(900,SimConfig.DtSosFloor+CountActive("drive_thru")*18+(DriveCoverage<=0?120:0)+(StationOverloaded?90:0));}}public int FcSos{get{var m=MeasuredSos("lobby");return m>=0?(int)m:(int)Math.Min(720,SimConfig.FcSosFloor+CountActive("lobby")*20+(CounterCoverage<=0?120:0)+(StationOverloaded?75:0));}}public int DelSos{get{var m=MeasuredSos("delivery");return m>=0?(int)m:(int)Math.Min(2100,SimConfig.DelSosFloor+CountActive("delivery")*35+(StationOverloaded?180:0));}}public int TicketsThis30=>tickets30[(int)(Minute/30)%48];public int TicketsThis60=>tickets30[(int)(Minute/30)%48]+tickets30[((int)(Minute/30)+47)%48];public double SalesThis30=>sales30[(int)(Minute/30)%48];public double SalesThis60=>sales30[(int)(Minute/30)%48]+sales30[((int)(Minute/30)+47)%48];public string Daypart=>Minute<360?"late_night":Minute<600?"breakfast":Minute<690?"mid_morning":Minute<840?"lunch":Minute<990?"afternoon":Minute<1230?"dinner":"late_night";public string TimeText=>$"{(int)(Minute/60):00}:{(int)(Minute%60):00}";
 string Channel(){var x=Roll(20);return x<600?"drive_thru":x<760?"lobby":x<860?"delivery":"mobile";}int CountActive(string ch){var n=0;foreach(var t in activeTickets)if(t.Channel==ch)n++;return n;}int Roll(int salt){var v=(long)Seed*1103515245L+(long)(Orders+1)*12345L+(long)salt*9973L+(long)((int)Minute)*31L;return (int)(Math.Abs(v)%1000);}int ExpectedTicketSeconds(string channel,int items)=>150+items*35+(channel=="drive_thru"?SimConfig.DtIntakeSec+SimConfig.DtHandoffSec:channel=="lobby"?SimConfig.FcIntakeSec+SimConfig.FcHandoffSec+25:channel=="mobile"?SimConfig.MobHandoffSec+45:SimConfig.DelHandoffSec+160);string CustomerSegment=>Daypart=="breakfast"?"commuter_breakfast":Daypart=="dinner"?"family_dinner":Daypart=="late_night"?"late_night_guest":"general_guest";string OverloadCause=>Scenario=="equipment_failure"?"equipment_constraint":Scenario=="staffing_call_off"||CoverageOpen<=0&&CoverageUsed>=CoveragePool?"staffing_gap":Scenario=="multi_rush_condition"?"multi_rush":Scenario=="rush_day"||Scenario=="local_event_surge"||Scenario=="school_event_surge"?"rush_demand":BottleneckStation=="fryer"?"item_cook_time_mix":"menu_mix";string StaffReason(string reason)=>reason switch{"call_off"or"break_coverage"or"manager_adjustment"or"shift_start"or"shift_end"or"rush_support"or"station_recovery"=>reason,_=>"manager_adjustment"};string JsonStringOrNull(string? v)=>v==null?"null":$"\"{v}\"";string Num(double n)=>n.ToString("0.##",CultureInfo.InvariantCulture);string DrawJson(Dictionary<string,double> draw){var parts=new List<string>();foreach(var kv in draw)parts.Add($"\"{kv.Key}\":{Num(kv.Value)}");return "{"+string.Join(",",parts)+"}";}string Json(string s)=>s.Replace("\\","\\\\").Replace("\"","\\\"");string JsonBool(bool b)=>b?"true":"false";
 bool ShouldBalk(string ch)=>ch=="drive_thru"?CountActive(ch)>=SimConfig.DtBalkQueueDepth:ch=="lobby"?CountActive(ch)>=SimConfig.LobbyBalkQueueDepth:ch=="mobile"?CountActive(ch)>=SimConfig.MobileThrottleQueueDepth:CountActive(ch)>=SimConfig.DeliveryThrottleQueueDepth;
}
