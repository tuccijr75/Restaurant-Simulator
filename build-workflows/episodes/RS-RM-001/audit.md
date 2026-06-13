# RS-RM-001 Audit

## Verification performed (sandbox, .NET 8.0.128)
- Built tools/engine-selftest from the pure-C# engine sources + new career module.
- Self-test: 10 scenarios at seed 12345 -> 120/120 PASS. This is the regression
  guard: with ReputationDemandMultiplier defaulting to 1.0, every (scenario,seed)
  replay is byte-identical to the pre-career engine. Representative numbers held
  to prior baselines (normal_day csat 90.6, abandoned 21, inspection 100,
  avg check $10.49, labor 25.2%).
- Career week (WeekSeed 777001) run twice -> 11/11 PASS: identical schedule,
  7/7 identical day event-stream hashes, identical final career JSON,
  JSON round-trip, reputation/multiplier/delta all bounded and non-constant.
- Cross-seed causality probe (777001,12345,90210,555,2026,131313): equipment_failure
  days post negative reputation deltas (-2.7..-3.9); clean weeks climb ~+0.9..+1.5/day;
  week 131313 (two failure days) finishes flat near start, dipping to 67.3.

## Design decisions
- Reputation is store-level only; derived from csat + inspection + abandonment.
  No per-employee data (control-pack 00).
- Determinism preserved by treating reputation carryover as a deterministic input
  (ReputationDemandMultiplier) defaulting to neutral 1.0.
- FNV-1a integer mixing for week schedule + per-day seeds — no platform GetHashCode,
  so weeks replay identically across runtimes.
- Self-contained JSON persistence (no external dependency), round-trip verified.

## Assumptions
- Reputation slope (0.005), band [0.85,1.05], delta coefficients, and +/-6/day
  clamp are operator_calibration_required synthetic defaults, not field constants.
- normal_day weighted 3x in the day 1-6 pool; day 0 anchored to normal_day.
- WeekSeed default 777001; gate seed 12345 unchanged.

## Blockers
- In-editor Godot 4.6 smoke test: register CareerHook autoload + add the two
  documented Main.cs call sites (ConfigureSim in _Ready, AdvanceDay on F6), then
  confirm scene compiles and F6 advances days. No Godot in the build environment;
  CareerHook references only Godot APIs + the engine-verified CareerState, but the
  in-engine compile/run is unverified here (same boundary as all prior tasks).

## Rollback
Safe. New files are additive; the only edit to existing code is two lines in
SimRunState.cs (field + multiplier), neutral at default 1.0. Revert RS-RM-001
files to restore prior behavior exactly.
