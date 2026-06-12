# Audit — RS-ST-001

## Task

RS-ST-001 — Scheduled Staffing Curve

## What Changed

1. **Scheduled crew curve** `ScheduledCrewAt(minute)`: breakfast 4 → mid-morning trough 3 → lunch build 6 → lunch peak 7 → afternoon 5 → dinner 7 → taper 5 → late night 4. Shape derived from docs/06 daypart ticket shares; exact heads `operator_calibration_required`.
2. **Evented transitions.** Every scheduled add/drop emits `staff.assignment.updated` with a schema-enum reason: opening roster `shift_start` (each on-clock head explicit), peak builds `rush_support`, tapers `manager_adjustment`, close `shift_end`. The `StaffReason` mapper now passes all enum values through instead of defaulting unknown reasons to `rush_support` (latent bug — `shift_start` previously emitted as `rush_support`).
3. **Automatic staggered breaks** in the off-peak windows (10:15–11:20, 14:30–16:00), 30 min each, paired `break_coverage` events, skipped when the effective crew is at skeleton level (observed: the 10:50 break correctly skips when the trough crew is 3).
4. **staffing_call_off is now causal.** The first lunch-build head no-shows at 11:00 (evented `call_off`), the store runs a one-head deficit until a 14:00 replacement arrives (evented `rush_support`). The blanket 0.74 capacity multiplier was removed — the missing head flows pool → coverage plan → capacity, eliminating double-counting.
5. **Demand-weighted auto coverage** distributes the live pool across drive/fryer/kitchen/counter/prep each minute, emitting one assignment event per changed unit (≈8 transitions/day, no spam).
6. **Manual override.** Any player staffing action (crew/lead/manager ±, breaks, call-off, coverage ±) switches `AutoSchedule` off; the Labor panel shows the mode and an "Auto Schedule" button resumes it. The 3D employee layer mirrors the schedule automatically (coverage-driven).

## Verification

- `tools/engine-selftest`: 90/90 PASS across all 10 scenarios (deterministic replay hashes, ledger reconciliation, lifecycle, chronology). The scheduler is keyed to integer sim-minutes, so determinism is preserved.
- Regenerated sample bundles: normal_day produces 42 `staff.assignment.updated` events (6 shift_start, 6 rush_support, 6 break_coverage pairs, 24 adjustments); staffing_call_off adds the 11:00 `call_off` and 14:00 replacement. Staffing ledger entries reconcile with the event stream 1:1.
- Labor lands at 22.5% on normal_day (band ≤30); slow_day 33.2% correctly trips the labor pass/fail gate (overstaffed for the volume — the gate working as intended).

## Honest Trade-off Recorded

Demand-weighted deployment flattens steady-state SOS differentiation: a properly deployed 7-crew lunch now absorbs rush_day (~83 s kitchen-ready vs 358 s under the old static deployment); strain reappears only in multi_rush_condition (520 s). Scenario signatures for ASC now live primarily in the staffing ledger (call_off/replacement events, coverage deltas), the temperature gates (equipment_failure), and overload events (multi_rush). The genuinely unmodeled stressor is front-end window/order-taking labor — drive-thru orders currently consume no service labor, which is why peaks rarely bind. Recommended follow-up: **RS-FE-001** (front-end service work model) to restore peak-hour SOS degradation causally rather than by knob.

## Security Review

Passed. Synthetic role tokens only (`crew_sched_NN`); aggregate coverage only; no individual scoring.

## Result

Ready for review.
