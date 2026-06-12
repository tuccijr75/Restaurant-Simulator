from __future__ import annotations

import json
import unittest
from pathlib import Path

from restaurant_simulator.core import CHANNELS, DAYPARTS, SCENARIOS, build_simulation, scenario_config


PROFILE_PATH = Path(__file__).resolve().parents[1] / "profiles" / "plausibility.json"


def load_profile() -> dict:
    return json.loads(PROFILE_PATH.read_text(encoding="utf-8"))


def orders_total(result: dict) -> int:
    return result["end_of_shift_summary"]["demand_summary"]["metrics"]["orders_total"]


def completion_ratio(result: dict) -> float:
    metrics = result["end_of_shift_summary"]["demand_summary"]["metrics"]
    orders = metrics["orders_total"] or 1
    return metrics["completed_tickets"] / orders


class PlausibilityProfileTests(unittest.TestCase):
    def test_profile_declares_required_operational_categories(self) -> None:
        profile = load_profile()
        self.assertEqual(set(profile["required_scenarios"]), set(SCENARIOS))
        self.assertEqual(set(profile["required_dayparts"]), {d[0] for d in DAYPARTS})
        self.assertEqual(set(profile["required_channels"]), set(CHANNELS))

        required = {
            "daypart_demand_shape",
            "customer_arrivals",
            "channel_mix",
            "menu_mix",
            "station_capacity",
            "staffing_coverage",
            "prep_inventory",
            "waste",
            "equipment_constraints",
            "weather_effects",
            "traffic_effects",
            "local_events",
            "school_events",
            "holiday_pattern",
            "item_lifecycle",
            "overload_and_recovery",
            "multi_rush_stacking",
            "recommendation_dataset",
            "alert_dataset",
            "synthetic_security",
        }
        self.assertTrue(required.issubset(set(profile["required_realism_categories"])))

    def test_daypart_demand_shape_is_distinct_and_ordered(self) -> None:
        profile = load_profile()
        cfg = scenario_config("normal_day", 12345)
        dayparts = cfg["dayparts"]
        rates = {row["daypart"]: row["arrival_curve"]["base_rate_per_15_min"] for row in dayparts}

        self.assertEqual(set(rates), set(profile["required_dayparts"]))
        self.assertGreaterEqual(
            len({round(value, 2) for value in rates.values()}),
            profile["thresholds"]["minimum_distinct_daypart_rates"],
        )
        self.assertGreater(rates["lunch"], rates["mid_morning"])
        self.assertGreater(rates["dinner"], rates["afternoon"])
        self.assertGreater(rates["breakfast"], rates["late_night"])

    def test_channel_and_menu_mix_cover_required_service_modes(self) -> None:
        profile = load_profile()
        cfg = scenario_config("normal_day", 12345)
        required_channels = set(profile["required_channels"])

        for daypart in cfg["dayparts"]:
            with self.subTest(daypart=daypart["daypart"]):
                channel_mix = daypart["channel_mix"]
                menu_mix = daypart["menu_mix"]
                self.assertEqual(set(channel_mix), required_channels)
                self.assertAlmostEqual(sum(channel_mix.values()), 1.0, places=6)
                self.assertGreaterEqual(sum(1 for value in channel_mix.values() if value > 0), profile["thresholds"]["minimum_channel_coverage"])
                self.assertGreaterEqual(len(menu_mix), 3)
                self.assertAlmostEqual(sum(menu_mix.values()), 1.0, places=6)

        breakfast_mix = next(row["channel_mix"] for row in cfg["dayparts"] if row["daypart"] == "breakfast")
        self.assertGreater(breakfast_mix["drive_thru"], breakfast_mix["lobby"])

    def test_scenario_modifiers_have_directional_effects(self) -> None:
        normal = build_simulation("normal_day", 20260115)
        slow = build_simulation("slow_day", 20260115)
        rush = build_simulation("rush_day", 20260115)
        multi = build_simulation("multi_rush_condition", 20260115)

        self.assertLess(orders_total(slow), orders_total(normal))
        self.assertGreater(orders_total(rush), orders_total(normal))
        self.assertGreater(orders_total(multi), orders_total(rush))

        normal_weather = scenario_config("normal_day", 20260115)["weather_profile"]
        storm_weather = scenario_config("weather_disruption", 20260115)["weather_profile"]
        self.assertEqual(normal_weather["condition"], "clear")
        self.assertNotEqual(storm_weather["condition"], "clear")
        self.assertGreater(storm_weather["channel_shift"].get("delivery", 0), 0)

    def test_stress_scenarios_surface_causal_evidence(self) -> None:
        staffing = build_simulation("staffing_call_off", 20260115)
        staffing_events = [event for event in staffing["events"] if event["event_type"] == "staff.assignment.updated"]
        self.assertTrue(any(event["payload"].get("reason") == "call_off" for event in staffing_events))
        self.assertTrue(any(row["movement_type"] == "call_off" for row in staffing["staffing_ledger"]["entries"]))

        equipment_cfg = scenario_config("equipment_failure", 20260115)
        self.assertTrue(equipment_cfg["equipment_profile"]["constraints"])

        for scenario in ["local_event_surge", "school_event_surge", "holiday_pattern"]:
            with self.subTest(scenario=scenario):
                cfg = scenario_config(scenario, 20260115)
                self.assertTrue(cfg["local_event_profile"]["events"] or cfg["operating_date_profile"]["holiday_flag"])

        multi = build_simulation("multi_rush_condition", 20260115)
        event_types = [event["event_type"] for event in multi["events"]]
        self.assertIn("station.overloaded", event_types)
        self.assertIn("station.recovered", event_types)

    def test_lifecycle_ledgers_recommendations_and_alerts_are_coherent(self) -> None:
        profile = load_profile()
        thresholds = profile["thresholds"]

        for scenario in SCENARIOS:
            with self.subTest(scenario=scenario):
                result = build_simulation(scenario, 20260115)
                event_types = {event["event_type"] for event in result["events"]}
                self.assertIn("item.taken", event_types)
                self.assertIn("item.completed", event_types)
                self.assertNotIn("item.sold", event_types)
                self.assertGreaterEqual(completion_ratio(result), thresholds["minimum_completion_ratio"])
                self.assertEqual(result["inventory_ledger"]["reconciliation"]["status"], "passed")
                self.assertEqual(result["staffing_ledger"]["reconciliation"]["status"], "passed")
                self.assertGreaterEqual(len(result["inventory_ledger"]["entries"]), thresholds["minimum_inventory_components"])
                self.assertGreaterEqual(len(result["staffing_ledger"]["entries"]), thresholds["minimum_staffing_entries"])
                self.assertGreaterEqual(len(result["recommendation_validation_dataset"]["rows"]), thresholds["minimum_recommendation_rows"])
                self.assertGreaterEqual(len(result["alert_validation_dataset"]["rows"]), thresholds["minimum_alert_rows"])

    def test_generated_outputs_remain_synthetic_and_non_punitive(self) -> None:
        profile = load_profile()
        for scenario in SCENARIOS:
            with self.subTest(scenario=scenario):
                result = build_simulation(scenario, 20260115)
                serialized = json.dumps(result, sort_keys=True).lower()
                self.assertIn('"synthetic_data": true', serialized)
                for marker in profile["security_rules"]["forbidden_markers"]:
                    self.assertNotIn(marker, serialized)


if __name__ == "__main__":
    unittest.main()
