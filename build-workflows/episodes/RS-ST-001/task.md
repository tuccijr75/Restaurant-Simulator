# Task Brief — RS-ST-001

## Task Metadata

Task ID: RS-ST-001
Task Name: Scheduled Staffing Curve
Task Type: Engine — staffing mechanics
Target Layer: game/scripts/sim/SimRunState.cs, game/scripts/ui/LaborPanel.cs
Runtime Class: T3
Owner: Michael Robertucci
Approval Required: User requested "scheduled staffing curve" (follow-up to RS-3D-001 residual finding).
Expected Output: Daypart-shaped scheduled crew coverage with every change emitted as a schema-valid `staff.assignment.updated` event, automatic staggered breaks, an automatic evented call-off + replacement in the staffing_call_off scenario, demand-weighted auto coverage from the live pool, and a manual-override mode for player control.
Repository Branch: main

Files Allowed To Modify: game/scripts/sim/SimRunState.cs, game/scripts/ui/LaborPanel.cs, build-workflows/** (RS-ST-001 artifacts, state, current-task), tools/engine-selftest/** (regenerated outputs only)
Files Prohibited From Modifying: control-pack/active/**, README.md, pyproject.toml, restaurant-simulator-(4.3)/**, AI Shift Commander repository or files

## Objective

Make the staffing ledger ASC-meaningful in headless runs: replace the static crew level and the blanket staffing_call_off capacity multiplier with a causal scheduled-coverage model whose every transition is an explicit event, reconciling as `scheduled − call_offs + replacements + breaks/returns = active coverage`.

## Stop Rule

If the schema reason enum cannot express a scheduled transition, stop rather than invent a reason. (All transitions mapped into the existing enum: shift_start, rush_support, manager_adjustment, break_coverage, call_off, shift_end.)
