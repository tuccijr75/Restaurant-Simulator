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
ENTER_SECONDS = 20.0
DINING_SECONDS = 130.0
BUSING_SECONDS = 30.0
LEAVE_SECONDS = 25.0
ARRIVAL_RADIUS = 0.55
JITTER_WINDOW_SECONDS = 4.0
JITTER_MIN_PATH_M = 0.45
JITTER_MAX_NET_M = 0.12
WALKIN_PROXIMITY_SECONDS = 2.0
WALKIN_PROXIMITY_DISTANCE = 0.8


def fail(failures, code, msg):
    failures.append({"code": code, "message": msg})


def fail_once(failures, seen, code, msg):
    key = (code, msg)
    if key in seen:
        return
    seen.add(key)
    fail(failures, code, msg)


def fail_key(failures, seen, key, code, msg):
    if key in seen:
        return
    seen.add(key)
    fail(failures, code, msg)


def pos(agent):
    p = agent.get("position") or {}
    return float(p.get("x", 0.0)), float(p.get("z", 0.0))


def flat_dist(a, b):
    dx = a[0] - b[0]
    dz = a[1] - b[1]
    return (dx * dx + dz * dz) ** 0.5


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
    walkin_pair_time = {}
    seen_failures = set()
    jitter = {}
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
        slot_reserved = {}
        slot_occupied = {}
        for slot in row.get("slots", []):
            slot_id = slot.get("slot_id") or ""
            reserved_by = slot.get("reserved_by")
            occupied_by = slot.get("occupied_by")
            if reserved_by:
                if slot_id in slot_reserved and slot_reserved[slot_id] != reserved_by:
                    fail_once(failures, seen_failures, "duplicate_reserved_slot", f"{slot_id} reserved by {slot_reserved[slot_id]} and {reserved_by} at {now:.2f}s")
                slot_reserved[slot_id] = reserved_by
            if occupied_by:
                if slot_id in slot_occupied and slot_occupied[slot_id] != occupied_by:
                    fail_once(failures, seen_failures, "duplicate_occupied_slot", f"{slot_id} occupied by {slot_occupied[slot_id]} and {occupied_by} at {now:.2f}s")
                slot_occupied[slot_id] = occupied_by

        for agent in row.get("agents", []):
            agents_seen += 1
            agent_id = agent["agent_id"]
            slot_id = agent.get("slot_id") or ""
            if slot_id:
                if slot_id in active_slots:
                    fail_once(failures, seen_failures, "duplicate_agent_slot", f"{slot_id} used by {active_slots[slot_id]} and {agent_id} at {now:.2f}s")
                active_slots[slot_id] = agent_id

            phase = agent.get("phase", "")
            phase_seconds = float(agent.get("phase_seconds", 0.0))
            distance = float(agent.get("distance_to_target_m", 0.0))
            stuck = float(agent.get("stuck_seconds", 0.0))
            ticket_complete = bool(agent.get("ticket_complete", False))
            carrying = bool(agent.get("carrying_food", False))
            outside = bool(agent.get("outside_store", False))

            if stuck > STUCK_SECONDS and distance > STUCK_TARGET_DISTANCE:
                fail_key(failures, seen_failures, ("stuck", agent_id), "stuck", f"{agent_id} stuck {stuck:.2f}s at {distance:.2f}m from target")

            if agent.get("agent_type") == "customer":
                if phase == "Enter" and phase_seconds > ENTER_SECONDS:
                    fail_key(failures, seen_failures, ("enter_timeout", agent_id), "enter_timeout", f"{agent_id} enter {phase_seconds:.2f}s limit {ENTER_SECONDS:.2f}s")
                if phase == "Enter" and ticket_complete and phase_seconds > PICKUP_SECONDS:
                    fail_key(failures, seen_failures, ("complete_ticket_before_pickup", agent_id), "complete_ticket_before_pickup", f"{agent_id} complete ticket stuck in Enter {phase_seconds:.2f}s")
                if phase == "Dining" and phase_seconds > DINING_SECONDS:
                    fail_key(failures, seen_failures, ("dining_timeout", agent_id), "dining_timeout", f"{agent_id} dining {phase_seconds:.2f}s limit {DINING_SECONDS:.2f}s")
                if phase == "Busing" and phase_seconds > BUSING_SECONDS:
                    fail_key(failures, seen_failures, ("busing_timeout", agent_id), "busing_timeout", f"{agent_id} busing {phase_seconds:.2f}s limit {BUSING_SECONDS:.2f}s")
                if phase == "Leave" and phase_seconds > LEAVE_SECONDS:
                    fail_key(failures, seen_failures, ("leave_timeout", agent_id), "leave_timeout", f"{agent_id} leave {phase_seconds:.2f}s limit {LEAVE_SECONDS:.2f}s")

            if phase == "Ordering":
                limit = ORDER_KIOSK_SECONDS if slot_id.startswith("kiosk_") else ORDER_COUNTER_SECONDS
                if phase_seconds > limit:
                    fail_key(failures, seen_failures, ("ordering_timeout", agent_id), "ordering_timeout", f"{agent_id} ordering {phase_seconds:.2f}s limit {limit:.2f}s")

            if phase == "ToPickup" and ticket_complete and phase_seconds > PICKUP_SECONDS:
                fail_key(failures, seen_failures, ("pickup_timeout", agent_id), "pickup_timeout", f"{agent_id} pickup {phase_seconds:.2f}s after ticket complete")

            if phase == "Waiting" and ticket_complete and phase_seconds > WAITING_SECONDS:
                fail_key(failures, seen_failures, ("waiting_timeout", agent_id), "waiting_timeout", f"{agent_id} waiting {phase_seconds:.2f}s with complete ticket")

            if phase == "Leave" and carrying and outside:
                outside_food_time[agent_id] = outside_food_time.get(agent_id, 0.0) + dt
                if outside_food_time[agent_id] > OUTSIDE_FOOD_SECONDS:
                    fail_key(failures, seen_failures, ("outside_with_food", agent_id), "outside_with_food", f"{agent_id} outside with food {outside_food_time[agent_id]:.2f}s")
            else:
                outside_food_time[agent_id] = 0.0

            sample_pos = pos(agent)
            hist = jitter.setdefault(agent_id, [])
            hist.append((now, sample_pos, distance, slot_id, phase))
            while hist and now - hist[0][0] > JITTER_WINDOW_SECONDS:
                hist.pop(0)
            if slot_id and distance <= ARRIVAL_RADIUS and len(hist) >= 4:
                same_slot = all(h[3] == slot_id and h[4] == phase for h in hist)
                if same_slot:
                    path = sum(flat_dist(hist[i - 1][1], hist[i][1]) for i in range(1, len(hist)))
                    net = flat_dist(hist[0][1], hist[-1][1])
                    if path >= JITTER_MIN_PATH_M and net <= JITTER_MAX_NET_M:
                        fail_key(failures, seen_failures, ("jitter", agent_id, slot_id, phase), "jitter", f"{agent_id} jitter near {slot_id}: path {path:.2f}m net {net:.2f}m")

        for pair in row.get("pairs", []):
            key = tuple(sorted((pair["a"], pair["b"])))
            if pair.get("exempt", False):
                overlap_time[key] = 0.0
                continue
            if float(pair["distance_m"]) < OVERLAP_DISTANCE:
                overlap_time[key] = overlap_time.get(key, 0.0) + dt
                if overlap_time[key] > OVERLAP_SECONDS:
                    fail_once(failures, seen_failures, "overlap", f"{key[0]} and {key[1]} overlapped {overlap_time[key]:.2f}s")
            else:
                overlap_time[key] = 0.0

            if float(pair["distance_m"]) < WALKIN_PROXIMITY_DISTANCE:
                agents = {a["agent_id"]: a for a in row.get("agents", [])}
                a = agents.get(pair["a"], {})
                b = agents.get(pair["b"], {})
                if "Walk-in supply run" in (a.get("phase", ""), b.get("phase", "")):
                    walkin_pair_time[key] = walkin_pair_time.get(key, 0.0) + dt
                    if walkin_pair_time[key] > WALKIN_PROXIMITY_SECONDS:
                        fail_key(failures, seen_failures, ("walkin_proximity", key), "walkin_proximity", f"{key[0]} and {key[1]} near walk-in supply run {walkin_pair_time[key]:.2f}s")
            else:
                walkin_pair_time[key] = 0.0

    if samples == 0:
        fail_once(failures, seen_failures, "no_samples", "movement_samples.jsonl was empty")
    if agents_seen == 0:
        fail_once(failures, seen_failures, "no_agents", "no agents were sampled")

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
            "enter_seconds": ENTER_SECONDS,
            "dining_seconds": DINING_SECONDS,
            "busing_seconds": BUSING_SECONDS,
            "leave_seconds": LEAVE_SECONDS,
            "jitter_window_seconds": JITTER_WINDOW_SECONDS,
            "jitter_min_path_m": JITTER_MIN_PATH_M,
            "jitter_max_net_m": JITTER_MAX_NET_M,
            "walkin_proximity_seconds": WALKIN_PROXIMITY_SECONDS,
            "walkin_proximity_distance_m": WALKIN_PROXIMITY_DISTANCE,
        },
    }
    (out_dir / "movement_summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary))
    return 0 if not failures else 1


if __name__ == "__main__":
    raise SystemExit(main())
