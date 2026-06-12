# RS-IN-001 — Inspections + equipment wear

**Date:** 2026-06-11  ·  **Status:** complete  ·  **Workflow:** build-workflows v1

## Objective
Equipment condition that degrades with processed work, costed maintenance and
breakdown decisions, and a seeded health inspection that audits the compliance
state the player (or auto-crew) actually maintained.

## Scope of change
- EquipmentUnit wear: Condition = 100 - work/WearK (fryer 180, grill 220, other 600); <40 costs 20% throughput; <=10 fails the unit and raises a breakdown decision (call tech $150/30min vs limp until maintained).
- MaintainWorstEquipment: $40, 10-min downtime, resets wear baseline. Exposed on the station panel.
- Health inspection at a seeded minute (10:30-16:30): deducts for temps out of range, stale temp log, overdue sanitizer, no sanitation tasks, expired hold product observed, line out of control. Score + notes exported; pass_fail gate at 80.
- InspectorIncoming 15-min warning surfaces in alerts and the objectives strip.

## Acceptance
- Headless self-test suite passes for all 10 scenarios, seed 12345 (F9 gate in-game).
- Schema contract intact: envelope fields, split item lifecycle, staff/waste reason enums, no item.sold.
- Inventory ledger equation reconciles for every component.
- Determinism: identical replay for identical (config, scenario, seed).
