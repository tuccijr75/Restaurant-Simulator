import json,hashlib
from pathlib import Path
SCENARIOS='normal_day slow_day rush_day weather_disruption staffing_call_off equipment_failure local_event_surge school_event_surge holiday_pattern multi_rush_condition'.split()
EVENT_TYPES=set('shift.started order.created item.sold ticket.updated staff.assignment.updated prep.confirmed waste.recorded station.overloaded station.recovered shift.ended'.split())
def scenario_config(scenario_type='normal_day',seed=12345):
    if scenario_type not in SCENARIOS: raise ValueError(scenario_type