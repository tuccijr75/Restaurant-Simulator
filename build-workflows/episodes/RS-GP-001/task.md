# RS-GP-001 — Manager Mode: decisions, clickable stations, objectives HUD

**Date:** 2026-06-11  ·  **Status:** complete  ·  **Workflow:** build-workflows v1

## Objective
Make the simulation player-led: an M-key Manager Mode that disables the auto
policies and surfaces costed decision interrupts, clickable 3D stations with direct
controls, and a live objectives strip tied to the end-of-shift pass/fail dimensions.

## Scope of change
- Engine: ManagerMode flag (default off — ASC/headless semantics unchanged); AutoHold off in Manager Mode (player drops batches); auto-prep gated; truck arrival becomes a decision.
- Decision system: truck (receive/defer), call-off (run short / overtime cover +$33), breakdown (call tech $150 / limp), complaint (apologize / comp $8 + goodwill), emergency supply ($120). 3-min deadline, deterministic auto-default when unanswered or in auto mode.
- Commands work while paused (pause-to-command).
- Godot: stations carry StaticBody3D + metadata; left-click raycast opens a station panel (coverage +/-, drop batch, maintain, prep, temps, sanitizer). Decision toast top-center with A/B buttons + countdown. Objectives strip: sales pace vs target, labor, CSAT, DT SOS, abandonment, hold levels, worst equipment, inspection, cost lines (OT/comps/maintenance/supply runs).
- Costs (OvertimePremium, CompCost, MaintSpend, SupplyRunCost) booked and exported.

## Acceptance
- Headless self-test suite passes for all 10 scenarios, seed 12345 (F9 gate in-game).
- Schema contract intact: envelope fields, split item lifecycle, staff/waste reason enums, no item.sold.
- Inventory ledger equation reconciles for every component.
- Determinism: identical replay for identical (config, scenario, seed).
