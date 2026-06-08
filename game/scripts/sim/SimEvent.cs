namespace RestaurantSimulator;
public class SimEvent{
 public int Seq,Seed; public string Time,Type,Scenario,Daypart;
 public SimEvent(int seq,string time,string type,string scenario,int seed,string daypart){Seq=seq;Time=time;Type=type;Scenario=scenario;Seed=seed;Daypart=daypart;}
 public string Text=>$"{Seq} {Time} {Type}";
 public string Jsonl=>$"{{\"event_id\":\"evt_{Seq:000000}\",\"simulation_id\":\"sim_{Scenario}_{Seed}\",\"scenario_id\":\"scn_{Scenario}\",\"seed\":{Seed},\"event_type\":\"{Type}\",\"occurred_at\":\"{Time}\",\"daypart\":\"{Daypart}\",\"sequence\":{Seq},\"source\":\"restaurant_simulator_game\",\"synthetic_data\":true,\"schema_version\":\"1.0.0\",\"generator_version\":\"game-0.1.0\"}}";
}
