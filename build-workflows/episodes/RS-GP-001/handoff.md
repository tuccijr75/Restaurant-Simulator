# RS-GP-001 — Handoff

**Date:** 2026-06-11

## What landed
- Engine: ManagerMode flag (default off — ASC/headless semantics unchanged); AutoHold off in Manager Mode (player drops batches); auto-prep gated; truck arrival becomes a decision.
- Decision system: truck (receive/defer), call-off (run short / overtime cover +$33), breakdown (call tech $150 / limp), complaint (apologize / comp $8 + goodwill), emergency supply ($120). 3-min deadline, deterministic auto-default when unanswered or in auto mode.
- Commands work while paused (pause-to-command).
- Godot: stations carry StaticBody3D + metadata; left-click raycast opens a station panel (coverage +/-, drop batch, maintain, prep, temps, sanitizer). Decision toast top-center with A/B buttons + countdown. Objectives strip: sales pace vs target, labor, CSAT, DT SOS, abandonment, hold levels, worst equipment, inspection, cost lines (OT/comps/maintenance/supply runs).
- Costs (OvertimePremium, CompCost, MaintSpend, SupplyRunCost) booked and exported.

## How to verify
1. Open game/ in Godot 4.6 (.NET), run, press F9 — expect PASS for the loaded scenario.
2. Headless: tools/engine-selftest (dotnet run) — 10-scenario suite.
3. F5 exports the 8-file contract to user://outputs/sim_{scenario}_{seed}.

## Known limits / next
- Visual layer compiles only in Godot — smoke-test in-editor (day/night sweep, KDS boards, click panels, decision toast).
- Calibration constants marked operator_calibration_required throughout SimConfig.
- Auto-policy hold waste (grilled ~32% in shoulders) is the headroom a Manager Mode player can beat.
