# RS-FE-001 — Handoff

**Date:** 2026-06-11

## What landed
- New service equipment: dt_window_1/2 (dt_service), counter_pos_1/2 (counter_service), pickup_shelf_1 (pickup_service).
- Per-order intake task (DT 40s, lobby 35s; mobile/delivery skip — ordered remotely). Kitchen tasks depend on intake completion.
- Per-order handoff task queued lazily once all items plate (DT 35s, FC 12s, mobile 10s, delivery 20s). Ticket completes only after handoff.
- CoverageFactor cases for drive_thru/lobby/pickup stations: unmanned window = zero throughput.
- ExpectedTicketSeconds includes service phases per channel.
- Intake/handoff seconds in SimConfig (Intouch DT decomposition; operator_calibration_required).

## How to verify
1. Open game/ in Godot 4.6 (.NET), run, press F9 — expect PASS for the loaded scenario.
2. Headless: tools/engine-selftest (dotnet run) — 10-scenario suite.
3. F5 exports the 8-file contract to user://outputs/sim_{scenario}_{seed}.

## Known limits / next
- Visual layer compiles only in Godot — smoke-test in-editor (day/night sweep, KDS boards, click panels, decision toast).
- Calibration constants marked operator_calibration_required throughout SimConfig.
- Auto-policy hold waste (grilled ~32% in shoulders) is the headroom a Manager Mode player can beat.
