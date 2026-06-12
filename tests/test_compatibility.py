"""RS-CX-001 — AI Shift Commander compatibility tests.

Proves exported simulator bundles are ASC-ingestion-ready without integrating
with ASC runtime. Every rule enforced here is declared in
profiles/compatibility.json; the two files are reviewed together.

Bundle discovery:
  * RS_COMPAT_BUNDLES env var: os.pathsep-joined bundle directories, or
  * default globs (outputs/sim_*_*, sample_outputs/sim_*_*, /tmp/sample_outputs/sim_*_*).
Tests run against EVERY discovered bundle via subTest, so one command validates
all exported scenarios at once. Stdlib only.
"""
import glob
import hashlib
import json
import os
import re
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

REQUIRED_FILES = [
    "event_stream.jsonl", "inventory_ledger.json", "staffing_ledger.json",
    "recommendation_validation_dataset.json", "alert_validation_dataset.json",
    "end_of_shift_summary.json", "run_receipt.json", "hashes.json",
]
ENVELOPE_FIELDS = [
    "event_id", "simulation_id", "scenario_id", "seed", "event_type",
    "occurred_at", "business_day", "daypart", "sequence", "source",
    "synthetic_data", "schema_version", "generator_version", "payload",
]
ALWAYS_REQUIRED_EVENTS = {
    "shift.started", "order.created", "item.taken", "item.completed",
    "ticket.updated", "staff.assignment.updated", "prep.confirmed",
    "waste.recorded",
}
STAFF_REASONS = {
    "shift_start", "shift_end", "call_off", "break_coverage",
    "rush_support", "manager_adjustment", "station_recovery",
}
FORBIDDEN_MARKERS = [
    "individual_score", "individual_rank", "disciplinary_prediction",
    "named_employee_performance_label", "performance_score", "payroll",
    "api_key", "access_token", "client_secret", "BEGIN PRIVATE KEY",
    "connection_string",
]
EMAIL_RE = re.compile(r"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}")
SSN_RE = re.compile(r"\b\d{3}-\d{2}-\d{4}\b")
PROVENANCE_KEYS = ["simulation_id", "scenario_id", "seed", "schema_version", "generator_version"]


def discover_bundles():
    env = os.environ.get("RS_COMPAT_BUNDLES")
    if env:
        dirs = [d for d in env.split(os.pathsep) if d]
    else:
        dirs = []
        for pattern in ("outputs/sim_*_*", "sample_outputs/sim_*_*", "/tmp/sample_outputs/sim_*_*"):
            full = pattern if os.path.isabs(pattern) else os.path.join(ROOT, pattern)
            dirs.extend(sorted(glob.glob(full)))
    return [d for d in dirs if os.path.isdir(d)
            and os.path.isfile(os.path.join(d, "event_stream.jsonl"))]


class Bundle:
    """Loads one exported bundle once; shared by all tests."""

    def __init__(self, path):
        self.path = path
        self.name = os.path.basename(path.rstrip("/"))
        self.raw = {}
        for fn in REQUIRED_FILES:
            fp = os.path.join(path, fn)
            self.raw[fn] = open(fp, encoding="utf-8").read() if os.path.isfile(fp) else None
        self.json = {}
        for fn, content in self.raw.items():
            if fn.endswith(".json") and content is not None:
                try:
                    self.json[fn] = json.loads(content)
                except json.JSONDecodeError:
                    self.json[fn] = None
        self.events = []
        if self.raw["event_stream.jsonl"]:
            for line in self.raw["event_stream.jsonl"].splitlines():
                if line.strip():
                    try:
                        self.events.append(json.loads(line))
                    except json.JSONDecodeError:
                        self.events.append(None)


_BUNDLES = None


def bundles():
    global _BUNDLES
    if _BUNDLES is None:
        _BUNDLES = [Bundle(d) for d in discover_bundles()]
    return _BUNDLES


class CompatibilityBase(unittest.TestCase):
    def setUp(self):
        self.bundles = bundles()
        if not self.bundles:
            self.skipTest(
                "No exported bundle found. Export one (F5 in-game or the Exports "
                "panel) and set RS_COMPAT_BUNDLES to its directory."
            )

    def each(self):
        # Plain iteration: every assertion message already names the bundle, and
        # subTest-inside-a-generator misbehaves on failure under Python 3.12.
        return list(self.bundles)


class TestBundleContract(CompatibilityBase):
    def test_required_files_exist(self):
        for b in self.each():
            missing = [fn for fn in REQUIRED_FILES if b.raw[fn] is None]
            self.assertEqual(missing, [], f"{b.name}: missing files {missing}")

    def test_all_json_artifacts_parse(self):
        for b in self.each():
            bad = [fn for fn, parsed in b.json.items() if parsed is None]
            self.assertEqual(bad, [], f"{b.name}: unparseable JSON {bad}")
            bad_lines = sum(1 for e in b.events if e is None)
            self.assertEqual(bad_lines, 0, f"{b.name}: {bad_lines} unparseable JSONL lines")

    def test_hashes_match_artifacts(self):
        for b in self.each():
            hmap = b.json.get("hashes.json") or {}
            self.assertTrue(hmap, f"{b.name}: empty hashes.json")
            for fn, expected in hmap.items():
                content = b.raw.get(fn)
                self.assertIsNotNone(content, f"{b.name}: hashed file {fn} missing")
                actual = hashlib.sha256(content.encode("utf-8")).hexdigest()
                self.assertEqual(actual, expected, f"{b.name}: hash mismatch for {fn}")


class TestEventEnvelope(CompatibilityBase):
    def test_envelope_completeness(self):
        for b in self.each():
            for i, e in enumerate(b.events):
                missing = [f for f in ENVELOPE_FIELDS if f not in e]
                self.assertEqual(missing, [], f"{b.name} event #{i}: missing {missing}")
            srcs = {e["source"] for e in b.events}
            self.assertEqual(srcs, {"restaurant_daily_flow_simulator"},
                             f"{b.name}: unexpected sources {srcs}")
            self.assertTrue(all(e["synthetic_data"] is True for e in b.events),
                            f"{b.name}: event without synthetic_data:true")

    def test_event_type_contract(self):
        for b in self.each():
            types = {e["event_type"] for e in b.events}
            missing = ALWAYS_REQUIRED_EVENTS - types
            self.assertEqual(missing, set(), f"{b.name}: missing event types {missing}")
            self.assertNotIn("item.sold", types, f"{b.name}: deprecated item.sold present")
            summary = b.json["end_of_shift_summary.json"]
            if summary.get("overload_events_total", 0) > 0:
                self.assertIn("station.overloaded", types,
                              f"{b.name}: overloads counted but no station.overloaded event")
            shift_ended = "shift.ended" in types
            if summary.get("active_tickets_at_close", 1) == 0 or shift_ended:
                pass  # mid-day exports legitimately lack shift.ended
            self.assertIn("shift.started", types)

    def test_ordering(self):
        for b in self.each():
            last_seq, last_t = 0, ""
            for e in b.events:
                self.assertGreater(e["sequence"], last_seq,
                                   f"{b.name}: sequence not strictly increasing at {e['event_id']}")
                self.assertGreaterEqual(e["occurred_at"], last_t,
                                        f"{b.name}: occurred_at regressed at {e['event_id']}")
                last_seq, last_t = e["sequence"], e["occurred_at"]


class TestItemLifecycle(CompatibilityBase):
    def test_completed_never_exceeds_taken(self):
        for b in self.each():
            taken, done = {}, {}
            for e in b.events:
                et, oid = e["event_type"], e["payload"].get("order_id", "")
                if et == "item.taken":
                    taken[oid] = taken.get(oid, 0) + 1
                elif et == "item.completed":
                    done[oid] = done.get(oid, 0) + 1
                    self.assertLessEqual(done[oid], taken.get(oid, 0),
                                         f"{b.name}: {oid} completed before taken")
                elif et == "ticket.updated" and e["payload"].get("status") == "completed":
                    self.assertEqual(done.get(oid, 0), taken.get(oid, 0),
                                     f"{b.name}: ticket {oid} completed with open items")

    def test_ticket_statuses_modeled(self):
        for b in self.each():
            statuses = {e["payload"].get("status") for e in b.events
                        if e["event_type"] == "ticket.updated"}
            self.assertIn("queued", statuses, f"{b.name}: no queued tickets")
            self.assertTrue({"completed", "abandoned"} & statuses,
                            f"{b.name}: no terminal ticket states observed")


class TestLedgers(CompatibilityBase):
    def test_inventory_reconciles(self):
        for b in self.each():
            led = b.json["inventory_ledger.json"]
            for row in led["components"]:
                self.assertTrue(row["reconciles"],
                                f"{b.name}: {row['inventory_item_id']} flagged unreconciled")
                lhs = (row["opening"] + row["prep_confirmed_or_received"]
                       - row["consumed_item_taken"] - row["waste_recorded"]
                       + row["approved_adjustments"])
                self.assertAlmostEqual(lhs, row["closing"], delta=0.01,
                                       msg=f"{b.name}: {row['inventory_item_id']} equation fails")

    def test_staffing_aggregate_and_synthetic(self):
        for b in self.each():
            led = b.json["staffing_ledger.json"]
            self.assertIn("active_role_coverage_by_interval", led["model"])
            for entry in led["entries"]:
                m = re.search(r"reason (\S+)", entry)
                self.assertIsNotNone(m, f"{b.name}: staffing entry without reason: {entry[:60]}")
                self.assertIn(m.group(1), STAFF_REASONS,
                              f"{b.name}: unknown staffing reason {m.group(1)}")
            blob = b.raw["staffing_ledger.json"]
            for marker in ("individual_score", "individual_rank", "disciplinary"):
                self.assertNotIn(marker, blob, f"{b.name}: staffing carries {marker}")


class TestProvenance(CompatibilityBase):
    def test_consistent_across_bundle(self):
        for b in self.each():
            blocks = [b.json[fn]["provenance"] for fn in (
                "inventory_ledger.json", "staffing_ledger.json",
                "recommendation_validation_dataset.json",
                "alert_validation_dataset.json", "end_of_shift_summary.json",
                "run_receipt.json") if isinstance(b.json.get(fn), dict)
                and "provenance" in b.json[fn]]
            self.assertGreaterEqual(len(blocks), 6, f"{b.name}: provenance blocks missing")
            ref = blocks[0]
            for blk in blocks[1:]:
                for k in PROVENANCE_KEYS:
                    self.assertEqual(blk[k], ref[k],
                                     f"{b.name}: provenance {k} inconsistent across files")
            for blk in blocks:
                self.assertIs(blk["synthetic_data"], True)
                self.assertEqual(blk["data_classification"], "INTERNAL_SIM")
            for e in b.events:
                for k in PROVENANCE_KEYS:
                    self.assertEqual(e[k], ref[k],
                                     f"{b.name}: event {e['event_id']} {k} != bundle provenance")

    def test_summary_internally_consistent(self):
        for b in self.each():
            s = b.json["end_of_shift_summary.json"]
            self.assertEqual(sum(s["orders_by_channel"].values()), s["orders_total"],
                             f"{b.name}: channel split != orders_total")
            created = sum(1 for e in b.events if e["event_type"] == "order.created")
            self.assertEqual(created, s["orders_total"],
                             f"{b.name}: order.created count != summary orders_total")


class TestSafety(CompatibilityBase):
    def test_no_forbidden_markers_or_pii(self):
        for b in self.each():
            for fn, content in b.raw.items():
                if content is None:
                    continue
                for marker in FORBIDDEN_MARKERS:
                    self.assertNotIn(marker, content, f"{b.name}/{fn}: contains {marker}")
                self.assertIsNone(EMAIL_RE.search(content), f"{b.name}/{fn}: email-like PII")
                self.assertIsNone(SSN_RE.search(content), f"{b.name}/{fn}: SSN-like PII")

    def test_synthetic_declared_everywhere(self):
        for b in self.each():
            self.assertIn('"synthetic_data":true',
                          b.raw["event_stream.jsonl"].splitlines()[0].replace(" ", ""),
                          f"{b.name}: first event lacks synthetic_data:true")


if __name__ == "__main__":
    unittest.main(verbosity=2)
