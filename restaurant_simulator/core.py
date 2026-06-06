import hashlib,json,random
from datetime import datetime,timedelta,timezone
from pathlib import Path
SCHEMA_VERSION='1.0.0';GENERATOR_VERSION='simgen-0.1.0';SOURCE='restaurant_daily_flow_simulator'
SCENARIOS='normal_day slow_day rush_day weather_disruption staffing_call_off equipment_failure local_event_surge school_event_surge holiday_pattern multi_rush_condition'.split()
EVENT_TYPES={'order.created','item.sold','ticket.updated','staff.assignment.updated','prep.confirmed','waste.recorded