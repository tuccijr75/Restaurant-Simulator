namespace RestaurantSimulator;
public class SimEvent{
 public int Seq; public int Seed; public string Time,Type,Scenario;
 public SimEvent(int seq,string time,string type,string scenario,int seed){Seq=seq;Time=time;Type=type;Scenario=scenario;Seed=seed;}
 public string Text=>$"{Seq} {Time} {Type}";
 public string Jsonl=>$"{{\"seq\":{Seq},\"seed\":{Seed},\"time\":\"{Time}\",\"type\":\"{Type}\",\"scenario\":\"{Scenario}\"}}";
}
