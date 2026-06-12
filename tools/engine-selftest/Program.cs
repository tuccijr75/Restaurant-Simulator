using RestaurantSimulator;
foreach (var sc in new[]{"normal_day","slow_day","rush_day","weather_disruption","staffing_call_off",
                          "equipment_failure","local_event_surge","school_event_surge","holiday_pattern","multi_rush_condition"})
{
    System.Console.WriteLine(SelfTest.Run(sc, 12345));
    System.Console.WriteLine(new string('-', 60));
}
