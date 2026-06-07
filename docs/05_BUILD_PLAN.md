# 05 Build Plan

## Purpose

Control the build order for Restaurant Simulator.

## Phase 0: Repo recovery

- Repair or remove broken Python prototype files.
- Keep docs as source of truth.
- Do not start Godot work until repo builds cleanly or legacy files are isolated.

## Phase 1: Godot shell

- Create Godot 4 C# project.
- Add main menu.
- Add scenario select.
- Add simulation clock.
- Add restaurant dashboard placeholder.

## Phase 2: Core simulation

- Deterministic seed system.
- Customer arrivals.
- POS orders.
- KDS tickets.
- Stations.
- SOS timers.

## Phase 3: Operations systems

- Inventory.
- Prep.
- Cook times.
- Hold times.
- Waste.
- Equipment.

## Phase 4: People and safety

- Staffing.
- Labor percent.
- Breaks.
- Sanitation.
- Temperature checks.
- Health inspections.

## Phase 5: Scoring

- Customer satisfaction.
- Pass/fail conditions.
- End-of-shift report.

## Phase 6: ASC test harness

- Event stream.
- State snapshot.
- Recommendation intake.
- Evaluation log.

## Rule

Build one playable, testable slice at a time. Do not add live integrations or real data without approval.
