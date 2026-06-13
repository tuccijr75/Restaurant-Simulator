# 08 — Career Mode (Multi-Day Reputation Carryover)

## Purpose

RS-RM-001 turns the single-day simulator into a persistent **career week**: 7
seeded business days driven by one `WeekSeed`, where the store's reputation
carries over from day to day and shapes the next day's demand. This gives ASC
longitudinal validation data — a causal reputation trajectory across a week —
instead of a pile of independent single days.

The whole week is deterministic. Same `WeekSeed` ⇒ same per-day scenarios, same
per-day engine seeds, same reputation trajectory, same event streams. The
single-day determinism contract (same `(scenario, seed)` ⇒ identical event
stream) is extended, not weakened.

## What carries over

Exactly one store-level scalar: **Reputation** (start 70, clamped to [40, 100]).
It is derived from store-level daily outcomes only — customer satisfaction,
health-inspection score, and abandonment rate. No per-employee signal exists in
this system, consistent with control-pack 00 (no employee scoring; synthetic
data only).

## Reputation model

`game/scripts/sim/CareerState.cs`.

Demand multiplier from reputation (applied as `SimRunState.ReputationDemandMultiplier`):

```
DemandMultiplier = clamp(0.90 + (Reputation - 70) * 0.005, 0.85, 1.05)
```

At the starting reputation of 70 the multiplier is 0.90; a top-reputation store
(100) reaches the 1.05 ceiling, a struggling one (40) the 0.85 floor. The
multiplier scales arrival rate at the store level only.

End-of-day reputation delta:

```
delta = 0.08*(csat - 85)
      + 0.05*(inspection - 80)          # only on days an inspection occurred
      - 100*max(0, abandonRate - 0.05)  # abandonment over 5% bites
delta = clamp(delta, -6, +6)            # one day can dent, never destroy
Reputation = clamp(Reputation + delta, 40, 100)
```

**Calibration note:** the slope (0.005), the demand band [0.85, 1.05], the delta
coefficients, and the ±6/day clamp are `operator_calibration_required` synthetic
defaults — plausible, internally consistent, and bounded, but not field-measured
constants. They are the knobs an operator tunes against real longitudinal data.

## Week scheduling

- **Day 0** is always `normal_day` — it establishes the week's baseline.
- **Days 1–6** are drawn deterministically from a weighted scenario pool
  (`normal_day` weighted 3×, plus the nine disruption scenarios), via an FNV-1a
  style integer mix of `(WeekSeed, day)`. No string hashing or platform
  `GetHashCode`, so weeks replay identically across runtimes.
- Each day's engine seed is a second independent mix of `(WeekSeed, day)`.

## Persistence

`user://career/career_state.json`, stable hand-rolled JSON (invariant culture,
no external JSON dependency — same constraint as the rest of `game/scripts/sim`).
It records `week_seed`, `day_index`, current `reputation`, the next
`demand_multiplier`, and a per-day history (scenario, seed, multiplier used,
csat, inspection, orders, abandoned, sales, reputation before/delta/after, and
the day's event-stream SHA-256). `CareerState.FromJson` round-trips its own
output exactly (asserted in the gate).

## Engine integration

`SimRunState` gained one deterministic input, `ReputationDemandMultiplier`
(default **1.0** = neutral). It multiplies into `RatePerSimMinute()`. Because the
default is 1.0, every pre-career `(scenario, seed)` replay is byte-identical —
proven by the 120/120 self-test below. It is a store-level input like config; it
is never an individual signal.

## Godot integration (one autoload, no Main.cs edits)

`game/scripts/sim/CareerHook.cs` is a **self-sufficient autoload**. It needs no
changes to `Main.cs`. Register it once:

> **Project ▸ Project Settings ▸ Autoload tab** → set Path to
> `res://game/scripts/sim/CareerHook.cs` → set Node Name to `CareerHook` →
> **Add** → make sure its **Enable** checkbox is ticked.

That's the entire setup. On launch you should see in the Output panel:

    [CareerHook] ready — week_seed=777001 day=0/7 reputation=70.0 ...

How it works without touching `Main`:

- It finds the running `SimRunState` in the scene by reflection (any node, any
  field or property — it does not depend on `Main`'s internal names).
- In `_Process` it configures the day's scenario, seed, and reputation
  multiplier **before** the shift starts, guarded on `EventSeq == 0` so it never
  mutates a run already in progress. Because autoloads run their `_Process` ahead
  of the main scene each frame, configuration always lands before `Main`'s first
  step.
- It handles **F6** in `_Input`. Autoloads receive `_Input` ahead of the main
  scene, so F6 is seen *before* `Main`'s focus-interceptor can swallow it — this
  is exactly why the earlier "F6 does nothing" symptom occurred (the hook simply
  wasn't loaded). **F7** resets the week.

`ConfigureSim` / `AdvanceDay` / `ResetWeek` remain public, so manual wiring is
still possible, but is no longer required.

### Verifying it took
After registering the autoload, run and press **F5** to export. The bundle's
`simulation_id` should read `sim_normal_day_132195522` (week 777001, day 0) —
**not** `sim_normal_day_12345`. The changed seed is the proof the career day was
configured. Press **F6** to fold the day into reputation and reload for day 1.

This autoload is the only part of RS-RM-001 not exercised by the headless gate;
it is the standing in-editor smoke-test item.

## Verification

`tools/engine-selftest` runs:
- the 10-scenario self-test at seed 12345 — **120/120 PASS**, confirming the
  `ReputationDemandMultiplier=1.0` default preserves byte-identical replays;
- `CareerTest` — one 7-day week run **twice** from `WeekSeed` 777001, asserting
  identical schedule, identical 7/7 day event-stream hashes, identical final
  career JSON, JSON round-trip, and that reputation stays bounded while actually
  responding to outcomes — **11/11 PASS**.

Sample week (777001): reputation climbs 70.0 → 78.0 across a mostly-clean week.
Cross-seed probes confirm causality: `equipment_failure` days post negative
deltas (≈ −2.7 to −3.9), and a week with two failure days (seed 131313) finishes
flat near its start with a mid-week dip to 67.3.
