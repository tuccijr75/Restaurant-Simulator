# RS-RM-001 — Multi-Day Career Mode (Reputation Carryover, Seeded Weeks)

Lane: Engine / Godot
Branch: main
Status: ready_for_review
Previous: RS-CX-001 — ASC compatibility profile + tests (ready_for_review)

## Goal
Extend the single-day simulator into a deterministic 7-day career week with
store-level reputation carrying over between days and shaping next-day demand,
to give ASC longitudinal validation data.

## Scope
- New: game/scripts/sim/CareerState.cs (reputation model, deterministic week
  schedule, JSON persistence), CareerTest.cs (headless week gate), CareerHook.cs
  (decoupled Godot autoload: F6 advance + persistence).
- Touched: game/scripts/sim/SimRunState.cs (+ReputationDemandMultiplier input,
  default 1.0, applied in RatePerSimMinute); tools/engine-selftest harness
  (Program.cs runs 10-scenario self-test + career week; harness.csproj includes
  the engine sources + career module; nuget.config clears sources).
- Docs: docs/08_CAREER_MODE.md.

## Acceptance
- 120/120 self-test PASS at seed 12345 (proves the multiplier default 1.0
  preserves byte-identical replays — no regression to existing bundles).
- Career week run twice from one WeekSeed: identical schedule, 7/7 identical day
  event-stream hashes, identical final career JSON, JSON round-trip; reputation
  bounded [40,100], multiplier bounded [0.85,1.05], delta bounded [-6,+6], and
  non-constant (responds to outcomes). 11/11 PASS.

## Security / doctrine
Reputation is a store-level synthetic signal from store-level outcomes (csat,
inspection, abandonment). No per-employee scoring introduced. Determinism and
the item.taken->item.completed lifecycle are unchanged; no item.sold.

## Calibration
Reputation slope/band/coefficients/clamp are operator_calibration_required
synthetic defaults (see docs/08).
