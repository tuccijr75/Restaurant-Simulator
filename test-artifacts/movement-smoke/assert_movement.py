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
DINING_ARRIVAL_SECONDS = 8.0
RIGHT_POS_MIN_X = 3.0
HEIGHT_TOLERANCE_M = 0.04
OFFICE_DEPARTURE_DISTANCE_M = 2.0
OFFICE_RETURN_RADIUS_M = 0.75


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


def target_pos(agent):
    p = agent.get("target") or {}
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
    last_phase = {}
    prev_time = None
    samples = 0
    agents_seen = 0
    customer_heights = []
    employee_heights = []
    office_tracks = {}

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
            phase_changed = last_phase.get(agent_id) != phase
            last_phase[agent_id] = phase
            distance = float(agent.get("distance_to_target_m", 0.0))
            stuck = float(agent.get("stuck_seconds", 0.0))
            ticket_complete = bool(agent.get("ticket_complete", False))
            carrying = bool(agent.get("carrying_food", False))
            outside = bool(agent.get("outside_store", False))

            if stuck > STUCK_SECONDS and distance > STUCK_TARGET_DISTANCE:
                fail_key(failures, seen_failures, ("stuck", agent_id), "stuck", f"{agent_id} stuck {stuck:.2f}s at {distance:.2f}m from target")

            if agent.get("agent_type") == "customer":
                h = float(agent.get("apparent_height_m", 0.0))
                if h > 0:
                    customer_heights.append(h)
                if not phase_changed and phase == "Enter" and phase_seconds > ENTER_SECONDS:
                    fail_key(failures, seen_failures, ("enter_timeout", agent_id), "enter_timeout", f"{agent_id} enter {phase_seconds:.2f}s limit {ENTER_SECONDS:.2f}s")
                if not phase_changed and phase == "Enter" and ticket_complete and phase_seconds > PICKUP_SECONDS:
                    fail_key(failures, seen_failures, ("complete_ticket_before_pickup", agent_id), "complete_ticket_before_pickup", f"{agent_id} complete ticket stuck in Enter {phase_seconds:.2f}s")
                if not phase_changed and phase == "Dining" and phase_seconds > DINING_SECONDS:
                    fail_key(failures, seen_failures, ("dining_timeout", agent_id), "dining_timeout", f"{agent_id} dining {phase_seconds:.2f}s limit {DINING_SECONDS:.2f}s")
                if not phase_changed and phase == "Dining" and phase_seconds > DINING_ARRIVAL_SECONDS and distance > ARRIVAL_RADIUS:
                    fail_key(failures, seen_failures, ("dining_seat_unreached", agent_id), "dining_seat_unreached", f"{agent_id} dining seat not reached after {phase_seconds:.2f}s distance {distance:.2f}m")
                if not phase_changed and phase == "Busing" and phase_seconds > BUSING_SECONDS:
                    fail_key(failures, seen_failures, ("busing_timeout", agent_id), "busing_timeout", f"{agent_id} busing {phase_seconds:.2f}s limit {BUSING_SECONDS:.2f}s")
                if not phase_changed and phase == "Leave" and phase_seconds > LEAVE_SECONDS:
                    fail_key(failures, seen_failures, ("leave_timeout", agent_id), "leave_timeout", f"{agent_id} leave {phase_seconds:.2f}s limit {LEAVE_SECONDS:.2f}s")

            if agent.get("agent_type") == "employee":
                h = float(agent.get("apparent_height_m", 0.0))
                if h > 0:
                    employee_heights.append(h)
                track = office_tracks.setdefault(agent_id, {
                    "home": None,
                    "departed": False,
                    "returned": False,
                    "max_distance": 0.0,
                    "last_phase": "",
                    "last_slot": "",
                })
                if slot_id.startswith("work_office_") and track["home"] is None:
                    track["home"] = sample_pos
                if track["home"] is not None:
                    office_distance = flat_dist(sample_pos, track["home"])
                    track["max_distance"] = max(track["max_distance"], office_distance)
                    if office_distance > OFFICE_DEPARTURE_DISTANCE_M:
                        track["departed"] = True
                    if track["departed"] and slot_id.startswith("work_office_") and office_distance <= OFFICE_RETURN_RADIUS_M:
                        track["returned"] = True
                    track["last_phase"] = phase
                    track["last_slot"] = slot_id

            if phase == "Ordering":
                if slot_id.startswith("pos_order_"):
                    tx, _ = target_pos(agent)
                    if tx < RIGHT_POS_MIN_X:
                        fail_key(failures, seen_failures, ("pos_not_right_end", slot_id), "pos_not_right_end", f"{slot_id} target x {tx:.2f} is not at customer-facing right end")
                limit = ORDER_KIOSK_SECONDS if slot_id.startswith("kiosk_") else ORDER_COUNTER_SECONDS
                if not phase_changed and phase_seconds > limit:
                    fail_key(failures, seen_failures, ("ordering_timeout", agent_id), "ordering_timeout", f"{agent_id} ordering {phase_seconds:.2f}s limit {limit:.2f}s")

            if not phase_changed and phase == "ToPickup" and ticket_complete and phase_seconds > PICKUP_SECONDS:
                fail_key(failures, seen_failures, ("pickup_timeout", agent_id), "pickup_timeout", f"{agent_id} pickup {phase_seconds:.2f}s after ticket complete")

            if not phase_changed and phase == "Waiting" and ticket_complete and phase_seconds > WAITING_SECONDS:
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

        supply_targets = {}
        for agent in row.get("agents", []):
            if agent.get("agent_type") != "employee":
                continue
            if agent.get("phase") != "Walk-in supply run":
                continue
            target = agent.get("target") or {}
            key = (
                round(float(target.get("x", 0.0)), 2),
                round(float(target.get("z", 0.0)), 2),
            )
            if key in supply_targets:
                fail_key(
                    failures,
                    seen_failures,
                    ("supply_run_conflict", key),
                    "supply_run_conflict",
                    f"{agent['agent_id']} and {supply_targets[key]} share walk-in supply target {key}",
                )
            else:
                supply_targets[key] = agent["agent_id"]

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
    if customer_heights and employee_heights:
        c_avg = sum(customer_heights) / len(customer_heights)
        e_avg = sum(employee_heights) / len(employee_heights)
        if abs(c_avg - e_avg) > HEIGHT_TOLERANCE_M:
            fail_once(failures, seen_failures, "height_mismatch", f"customer avg {c_avg:.2f}m employee avg {e_avg:.2f}m tolerance {HEIGHT_TOLERANCE_M:.2f}m")

    office_probe_path = out_dir / "manager_office_roundtrip.json"
    office_probe = None
    office_probe_passed = False
    if office_probe_path.exists():
        office_probe = json.loads(office_probe_path.read_text(encoding="utf-8"))
        office_probe_passed = office_probe.get("status") == "pass"
        if not office_probe_passed:
            fail_once(failures, seen_failures, "office_roundtrip_probe_failed", f"manager_office_roundtrip.json status {office_probe.get('status')}")

    office_candidates = {agent_id: t for agent_id, t in office_tracks.items() if t["home"] is not None}
    office_roundtrips = {agent_id: t for agent_id, t in office_candidates.items() if t["departed"] and t["returned"]}
    if not office_probe_passed:
        if not office_candidates:
            fail_once(failures, seen_failures, "office_roundtrip_missing_employee", "no employee sampled at a work_office slot")
        elif not any(t["departed"] for t in office_candidates.values()):
            fail_once(failures, seen_failures, "office_roundtrip_no_departure", "office employee never left far enough to exercise the office door")
        elif not office_roundtrips:
            detail = "; ".join(
                f"{agent_id} max {t['max_distance']:.2f}m last {t['last_phase']} {t['last_slot']}"
                for agent_id, t in office_candidates.items()
            )
            fail_once(failures, seen_failures, "office_roundtrip_no_return", f"office employee left but did not return to work_office slot ({detail})")

    summary = {
        "status": "pass" if not failures else "fail",
        "samples": samples,
        "agents_seen": agents_seen,
        "failures": failures,
        "office_roundtrip": {
            "passed_agents": sorted(office_roundtrips.keys()),
            "probe": office_probe,
        },
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
            "dining_arrival_seconds": DINING_ARRIVAL_SECONDS,
            "right_pos_min_x": RIGHT_POS_MIN_X,
            "height_tolerance_m": HEIGHT_TOLERANCE_M,
            "office_departure_distance_m": OFFICE_DEPARTURE_DISTANCE_M,
            "office_return_radius_m": OFFICE_RETURN_RADIUS_M,
        },
    }
    (out_dir / "movement_summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary))
    return 0 if not failures else 1


if __name__ == "__main__":
    raise SystemExit(main())
