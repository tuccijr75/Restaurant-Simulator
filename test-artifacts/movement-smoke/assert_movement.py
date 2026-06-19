#!/usr/bin/env python3
import json
import sys
from pathlib import Path

OVERLAP_DISTANCE = 0.45
OVERLAP_SECONDS = 2.0
STUCK_SECONDS = 4.0
STUCK_TARGET_DISTANCE = 0.75
OUTSIDE_FOOD_SECONDS = 5.0
ORDER_COUNTER_SECONDS = 12.0
ORDER_KIOSK_SECONDS = 18.0
PICKUP_SECONDS = 20.0
WAITING_SECONDS = 180.0


def fail(failures, code, msg):
    failures.append({"code": code, "message": msg})


def main():
    if len(sys.argv) != 2:
        print("usage: assert_movement.py <movement-smoke-output-dir>", file=sys.stderr)
        return 2

    out_dir = Path(sys.argv[1])
    sample_path = out_dir / "movement_samples.jsonl"
    if not sample_path.exists():
        print(json.dumps({"status": "fail", "failures": [{"code": "missing_telemetry", "message": str(sample_path)}]}))
        return 1

    failures = []
    overlap_time = {}
    outside_food_time = {}
    prev_time = None
    samples = 0
    agents_seen = 0

    for line in sample_path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        row = json.loads(line)
        samples += 1
        now = float(row["time_sec"])
        dt = 0.0 if prev_time is None else max(0.0, now - prev_time)
        prev_time = now

        active_slots = {}
        for agent in row.get("agents", []):
            agents_seen += 1
            agent_id = agent["agent_id"]
            slot_id = agent.get("slot_id") or ""
            if slot_id:
                if slot_id in active_slots:
                    fail(failures, "duplicate_agent_slot", f"{slot_id} used by {active_slots[slot_id]} and {agent_id} at {now:.2f}s")
                active_slots[slot_id] = agent_id

            phase = agent.get("phase", "")
            phase_seconds = float(agent.get("phase_seconds", 0.0))
            distance = float(agent.get("distance_to_target_m", 0.0))
            stuck = float(agent.get("stuck_seconds", 0.0))
            ticket_complete = bool(agent.get("ticket_complete", False))
            carrying = bool(agent.get("carrying_food", False))
            outside = bool(agent.get("outside_store", False))

            if stuck > STUCK_SECONDS and distance > STUCK_TARGET_DISTANCE:
                fail(failures, "stuck", f"{agent_id} stuck {stuck:.2f}s at {distance:.2f}m from target")

            if phase == "Ordering":
                limit = ORDER_KIOSK_SECONDS if slot_id.startswith("kiosk_") else ORDER_COUNTER_SECONDS
                if phase_seconds > limit:
                    fail(failures, "ordering_timeout", f"{agent_id} ordering {phase_seconds:.2f}s limit {limit:.2f}s")

            if phase == "ToPickup" and ticket_complete and phase_seconds > PICKUP_SECONDS:
                fail(failures, "pickup_timeout", f"{agent_id} pickup {phase_seconds:.2f}s after ticket complete")

            if phase == "Waiting" and ticket_complete and phase_seconds > WAITING_SECONDS:
                fail(failures, "waiting_timeout", f"{agent_id} waiting {phase_seconds:.2f}s with complete ticket")

            if phase == "Leave" and carrying and outside:
                outside_food_time[agent_id] = outside_food_time.get(agent_id, 0.0) + dt
                if outside_food_time[agent_id] > OUTSIDE_FOOD_SECONDS:
                    fail(failures, "outside_with_food", f"{agent_id} outside with food {outside_food_time[agent_id]:.2f}s")
            else:
                outside_food_time[agent_id] = 0.0

        for pair in row.get("pairs", []):
            key = tuple(sorted((pair["a"], pair["b"])))
            if pair.get("exempt", False):
                overlap_time[key] = 0.0
                continue
            if float(pair["distance_m"]) < OVERLAP_DISTANCE:
                overlap_time[key] = overlap_time.get(key, 0.0) + dt
                if overlap_time[key] > OVERLAP_SECONDS:
                    fail(failures, "overlap", f"{key[0]} and {key[1]} overlapped {overlap_time[key]:.2f}s")
            else:
                overlap_time[key] = 0.0

    if samples == 0:
        fail(failures, "no_samples", "movement_samples.jsonl was empty")
    if agents_seen == 0:
        fail(failures, "no_agents", "no agents were sampled")

    summary = {
        "status": "pass" if not failures else "fail",
        "samples": samples,
        "agents_seen": agents_seen,
        "failures": failures,
        "thresholds": {
            "overlap_distance_m": OVERLAP_DISTANCE,
            "overlap_seconds": OVERLAP_SECONDS,
            "stuck_seconds": STUCK_SECONDS,
            "stuck_target_distance_m": STUCK_TARGET_DISTANCE,
            "outside_food_seconds": OUTSIDE_FOOD_SECONDS,
            "ordering_counter_seconds": ORDER_COUNTER_SECONDS,
            "ordering_kiosk_seconds": ORDER_KIOSK_SECONDS,
            "pickup_seconds": PICKUP_SECONDS,
            "waiting_seconds": WAITING_SECONDS,
        },
    }
    (out_dir / "movement_summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary))
    return 0 if not failures else 1


if __name__ == "__main__":
    raise SystemExit(main())
