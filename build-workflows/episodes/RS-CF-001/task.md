# RS-CF-001 — Config + behavior profile wiring

**Date:** 2026-06-11  ·  **Status:** complete  ·  **Workflow:** build-workflows v1

## Objective
Wire config/realism_baseline.json and human_behavior_profiles.json into the engine
as deterministic inputs: service-time floors/targets, food-safety temps, and
aggregate crew pace/fatigue capacity modifiers.

## Scope of change
- SimConfig static class: all new tunables with defaults equal to prior constants (absent config reproduces identical runs). System.Text.Json parsing, fail-safe to defaults.
- Godot Main loads res://config/*.json at startup; headless harness can load from the repo path.
- CrewPace(seed, station): deterministic 3-profile blend from human_behavior_profiles pace multipliers, clamped 0.88-1.12 — an aggregate station-capacity nuance, never an individual signal (doctrine: no employee scoring).
- Fatigue: capacity eases up to 6% after the 8th shift hour (operator_calibration_required).
- SOS floors/targets, hold limits, patience, FDA temps, wear constants all config-driven.

## Acceptance
- Headless self-test suite passes for all 10 scenarios, seed 12345 (F9 gate in-game).
- Schema contract intact: envelope fields, split item lifecycle, staff/waste reason enums, no item.sold.
- Inventory ledger equation reconciles for every component.
- Determinism: identical replay for identical (config, scenario, seed).
