# RS-CF-001 — Handoff

**Date:** 2026-06-11

## What landed
- SimConfig static class: all new tunables with defaults equal to prior constants (absent config reproduces identical runs). System.Text.Json parsing, fail-safe to defaults.
- Godot Main loads res://config/*.json at startup; headless harness can load from the repo path.
- CrewPace(seed, station): deterministic 3-profile blend from human_behavior_profiles pace multipliers, clamped 0.88-1.12 — an aggregate station-capacity nuance, never an individual signal (doctrine: no employee scoring).
- Fatigue: capacity eases up to 6% after the 8th shift hour (operator_calibration_required).
- SOS floors/targets, hold limits, patience, FDA temps, wear constants all config-driven.

## How to verify
1. Open game/ in Godot 4.6 (.NET), run, press F9 — expect PASS for the loaded scenario.
2. Headless: tools/engine-selftest (dotnet run) — 10-scenario suite.
3. F5 exports the 8-file contract to user://outputs/sim_{scenario}_{seed}.

## Known limits / next
- Visual layer compiles only in Godot — smoke-test in-editor (day/night sweep, KDS boards, click panels, decision toast).
- Calibration constants marked operator_calibration_required throughout SimConfig.
- Auto-policy hold waste (grilled ~32% in shoulders) is the headroom a Manager Mode player can beat.
