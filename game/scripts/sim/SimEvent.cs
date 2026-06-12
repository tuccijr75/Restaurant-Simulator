namespace RestaurantSimulator;

public class SimEvent{
 public const string BusinessDay="2026-01-15";
 public int Seq,Seed; public string Time,Type,Scenario,Daypart,Payload;
 public SimEvent(int seq,string time,string type,string scenario,int seed,string daypart,string payload){Seq=seq;Time=time;Type=type;Scenario=scenario;Seed=seed;Daypart=daypart;Payload=payload;}
 public string Text=>$"{Seq} {Time} {Type}";
 public string OccurredAt=>$"{BusinessDay}T{Time}:00Z";
 public string Jsonl=>$"{{\"event_id\":\"evt_{Seq:000000}\",\"simulation_id\":\"sim_{Scenario}_{Seed}\",\"scenario_id\":\"scn_{Scenario}\",\"seed\":{Seed},\"event_type\":\"{Type}\",\"occurred_at\":\"{OccurredAt}\",\"business_day\":\"{BusinessDay}\",\"daypart\":\"{Daypart}\",\"sequence\":{Seq},\"source\":\"restaurant_daily_flow_simulator\",\"synthetic_data\":true,\"schema_version\":\"1.0.0\",\"generator_version\":\"game-0.3.0\",\"payload\":{Payload}}}";
}
