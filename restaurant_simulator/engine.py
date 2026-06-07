import json
from pathlib import Path
SCENARIOS='normal_day slow_day rush_day weather_disruption staffing_call_off equipment_failure local_event_surge school_event_surge holiday_pattern multi_rush_condition'.split()
def run_to_path(scenario_type='normal_day',seed=12345,out='outputs/run'):
    if scenario_type not in SCENARIOS: raise ValueError(scenario_type)
    p=Path(out); p.mkdir(par