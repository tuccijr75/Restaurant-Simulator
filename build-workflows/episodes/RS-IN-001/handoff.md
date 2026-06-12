# RS-IN-001 — Handoff

**Date:** 2026-06-11

## What landed
- EquipmentUnit wear: Condition = 100 - work/WearK (fryer 180, grill 220, other 600); <40 costs 20% throughput; <=10 fails the unit and raises a breakdown decision (call tech $150/30min vs limp until maintained).
- MaintainWorstEquipment: $40, 10-min downtime, resets wear baseline. Exposed on the station panel.
- Health inspection at a seeded minute (10:30-16:30): deducts for temps out of range, stale temp log, overdue sanitizer, no sanitation tasks, expired hold product observed, line out of control. Score + notes exported; pass_fail gate at 80.
- InspectorIncoming 15-min warning surfaces in alerts and the objectives strip.

## How to verify
1. Open game/ in Godot 4.6 (.NET), run, press F9 — expect PASS for the loaded scenario.
2. Headless: tools/engine-selftest (dotnet run) — 10-scenario suite.
3. F5 exports the 8-file contract to user://outputs/sim_{scenario}_{seed}.

## Known limits / next
- Visual layer compiles only in Godot — smoke-test in-editor (day/night sweep, KDS boards, click panels, decision toast).
- Calibration constants marked operator_calibration_required throughout SimConfig.
- Auto-policy hold waste (grilled ~32% in shoulders) is the headroom a Manager Mode player can beat.
