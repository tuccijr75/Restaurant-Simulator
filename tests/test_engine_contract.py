from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from restaurant_simulator.core import DAYPARTS, EVENT_TYPES, SCENARIOS, build_simulation, run_to_path


class EngineContractTests(unittest.TestCase):
    def test_all_scenarios_generate_required_outputs(self) -> None:
        for scenario in SCENARIOS:
            with self.subTest(scenario=scenario):
                result = build_simulation(scenario, 12345)
                self.assertIn("events", result)
                self.assertIn("inventory_ledger", result)
                self.assertIn("staffing_ledger", result)
                self.assertIn("recommendation_validation_dataset", result)
                self.assertIn("alert_validation_dataset", result)
                self.assertIn("end_of_shift_summary", result)
                self.assertEqual(result["validation"]["status"], "passed")

    def test_event_contract_basics(self) -> None:
        result = build_simulation("multi_rush_condition", 777)
        event_types = {event["event_type"] for event in result["events"]}
        for event_type in EVENT_TYPES:
            self.assertIn(event_type, event_types)
        dayparts = {event["daypart"] for event in result["events"]}
        for daypart, *_ in DAYPARTS:
            self.assertIn(daypart, dayparts)
        for event in result["events"]:
            self.assertEqual(event["source"], "restaurant_daily_flow_simulator")
            self.assertTrue(event["synthetic_data"])
            self.assertIn("business_day", event)
            self.assertRegex(event["occurred_at"], r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:00Z$")

    def test_inventory_reconciliation_and_security(self) -> None:
        result = build_simulation("staffing_call_off", 2468)
        self.assertEqual(result["inventory_ledger"]["reconciliation"]["status"], "passed")
        serialized = json.dumps(result, sort_keys=True).lower()
        forbidden = ["employee_score", "disciplinary", "ranking", "real_customer", "api_key", "token="]
        for term in forbidden:
            self.assertNotIn(term, serialized)

    def test_deterministic_replay(self) -> None:
        first = json.dumps(build_simulation("rush_day", 999), sort_keys=True)
        second = json.dumps(build_simulation("rush_day", 999), sort_keys=True)
        self.assertEqual(first, second)

    def test_run_to_path_creates_outputs(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            receipt = run_to_path("normal_day", 13579, tmp)
            self.assertEqual(receipt["status"], "completed")
            expected = {
                "event_stream.jsonl",
                "inventory_ledger.json",
                "staffing_ledger.json",
                "recommendation_validation_dataset.json",
                "alert_validation_dataset.json",
                "end_of_shift_summary.json",
                "run_receipt.json",
                "hashes.json",
            }
            self.assertEqual(expected, {p.name for p in Path(tmp).iterdir()})


if __name__ == "__main__":
    unittest.main()
