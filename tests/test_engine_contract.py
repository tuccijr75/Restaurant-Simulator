from __future__ import annotations

import json
import tempfile
import unittest
from collections import Counter, defaultdict
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

    def test_source_pack_schema_event_enum_matches_engine_contract(self) -> None:
        schema_path = Path(__file__).resolve().parents[1] / "control-pack" / "active" / "03_schema" / "03_schema.json"
        schema = json.loads(schema_path.read_text(encoding="utf-8"))
        schema_event_types = schema["$defs"]["event_type"]["enum"]
        self.assertEqual(EVENT_TYPES, schema_event_types)
        self.assertNotIn("item.sold", schema_event_types)
        self.assertIn("item.taken", schema_event_types)
        self.assertIn("item.completed", schema_event_types)

    def test_item_lifecycle_replaces_deprecated_item_sold(self) -> None:
        result = build_simulation("multi_rush_condition", 777)
        events = result["events"]
        event_types = [event["event_type"] for event in events]
        self.assertNotIn("item.sold", event_types)
        self.assertIn("item.taken", event_types)
        self.assertIn("item.completed", event_types)

        taken_by_order: dict[str, Counter[str]] = defaultdict(Counter)
        completed_by_order: dict[str, Counter[str]] = defaultdict(Counter)
        ticket_completed_orders: set[str] = set()

        for event in events:
            event_type = event["event_type"]
            payload = event["payload"]
            if event_type == "item.taken":
                self.assertEqual(payload["status"], "taken")
                taken_by_order[payload["order_id"]][payload["item_id"]] += int(payload["quantity"])
            elif event_type == "item.completed":
                self.assertEqual(payload["status"], "completed")
                order_id = payload["order_id"]
                item_id = payload["item_id"]
                completed_by_order[order_id][item_id] += int(payload["quantity"])
                self.assertGreaterEqual(
                    taken_by_order[order_id][item_id],
                    completed_by_order[order_id][item_id],
                    f"item.completed emitted before matching item.taken for order={order_id} item={item_id}",
                )
            elif event_type == "ticket.updated" and payload["status"] == "completed":
                order_id = payload["order_id"]
                self.assertEqual(
                    taken_by_order[order_id],
                    completed_by_order[order_id],
                    f"ticket completed before all items completed for order={order_id}",
                )
                ticket_completed_orders.add(order_id)

        self.assertGreater(len(taken_by_order), 0)
        self.assertEqual(set(taken_by_order), set(completed_by_order))
        self.assertEqual(set(taken_by_order), ticket_completed_orders)
        for order_id, taken_counts in taken_by_order.items():
            self.assertEqual(taken_counts, completed_by_order[order_id])

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
