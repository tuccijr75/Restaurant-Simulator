from __future__ import annotations

import hashlib
import json
import tempfile
import unittest
from pathlib import Path

from restaurant_simulator.core import SCENARIOS, build_simulation, run_to_path


EXPECTED_OUTPUT_FILES = {
    "event_stream.jsonl",
    "inventory_ledger.json",
    "staffing_ledger.json",
    "recommendation_validation_dataset.json",
    "alert_validation_dataset.json",
    "end_of_shift_summary.json",
    "run_receipt.json",
    "hashes.json",
}

PROHIBITED_MARKERS = [
    "employee_score",
    "disciplinary",
    "ranking",
    "real_customer",
    "payment_card",
    "phone_number",
    "home_address",
]


def stable_json(obj: object) -> str:
    return json.dumps(obj, sort_keys=True, separators=(",", ":"), ensure_ascii=False)


def sha_text(text: str) -> str:
    return hashlib.sha256(text.encode()).hexdigest()


class ValidationGateTests(unittest.TestCase):
    def test_all_scenarios_are_deterministic_for_fixed_seed(self) -> None:
        for scenario in SCENARIOS:
            with self.subTest(scenario=scenario):
                first = stable_json(build_simulation(scenario, 20260115))
                second = stable_json(build_simulation(scenario, 20260115))
                self.assertEqual(first, second)

    def test_all_scenarios_pass_internal_validation(self) -> None:
        for scenario in SCENARIOS:
            with self.subTest(scenario=scenario):
                result = build_simulation(scenario, 20260115)
                self.assertEqual(result["validation"]["status"], "passed")
                self.assertTrue(result["validation"]["schema_valid"])
                self.assertTrue(result["validation"]["security_valid"])
                self.assertTrue(result["validation"]["deterministic_replay_valid"])
                self.assertTrue(result["validation"]["ledgers_reconcile"])

    def test_event_stream_excludes_deprecated_item_sold(self) -> None:
        for scenario in SCENARIOS:
            with self.subTest(scenario=scenario):
                result = build_simulation(scenario, 20260115)
                event_types = {event["event_type"] for event in result["events"]}
                self.assertNotIn("item.sold", event_types)
                self.assertIn("item.taken", event_types)
                self.assertIn("item.completed", event_types)

    def test_generated_outputs_remain_synthetic_and_governance_safe(self) -> None:
        for scenario in SCENARIOS:
            with self.subTest(scenario=scenario):
                result = build_simulation(scenario, 20260115)
                serialized = json.dumps(result, sort_keys=True).lower()
                self.assertIn('"synthetic_data": true', serialized)
                for marker in PROHIBITED_MARKERS:
                    self.assertNotIn(marker, serialized)

    def test_run_to_path_output_bundle_and_receipt_hashes(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp)
            receipt = run_to_path("multi_rush_condition", 20260115, tmp)
            self.assertEqual(EXPECTED_OUTPUT_FILES, {p.name for p in out.iterdir()})
            self.assertEqual(receipt["status"], "completed")
            self.assertTrue(receipt["deterministic_replay_valid"])

            receipt_path = out / "run_receipt.json"
            hashes_path = out / "hashes.json"
            saved_receipt = json.loads(receipt_path.read_text(encoding="utf-8"))
            saved_hashes = json.loads(hashes_path.read_text(encoding="utf-8"))
            self.assertEqual(receipt["hashes"], saved_hashes)
            self.assertEqual(saved_receipt["hashes"], saved_hashes)

            event_text = (out / "event_stream.jsonl").read_text(encoding="utf-8")
            self.assertEqual(saved_hashes["event_stream"], sha_text(event_text))
            for artifact_name, hash_key in [
                ("inventory_ledger.json", "inventory_ledger"),
                ("staffing_ledger.json", "staffing_ledger"),
                ("recommendation_validation_dataset.json", "recommendation_validation_dataset"),
                ("alert_validation_dataset.json", "alert_validation_dataset"),
                ("end_of_shift_summary.json", "end_of_shift_summary"),
            ]:
                artifact = json.loads((out / artifact_name).read_text(encoding="utf-8"))
                canonical = json.dumps(artifact, indent=2, sort_keys=True, ensure_ascii=False) + "\n"
                self.assertEqual(saved_hashes[hash_key], sha_text(canonical))

    def test_output_files_are_parseable(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp)
            run_to_path("normal_day", 13579, tmp)
            for path in out.iterdir():
                if path.suffix == ".json":
                    json.loads(path.read_text(encoding="utf-8"))
                elif path.suffix == ".jsonl":
                    for line in path.read_text(encoding="utf-8").splitlines():
                        json.loads(line)


if __name__ == "__main__":
    unittest.main()
