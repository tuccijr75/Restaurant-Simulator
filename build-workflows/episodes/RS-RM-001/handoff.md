# RS-RM-001 Handoff

## New files
- game/scripts/sim/CareerState.cs — reputation model, deterministic week schedule, JSON persistence
- game/scripts/sim/CareerTest.cs — headless 7-day week determinism gate
- game/scripts/sim/CareerHook.cs — Godot autoload (F6 advance day + persistence), decoupled from Main
- tools/engine-selftest/Program.cs, harness.csproj, nuget.config — headless gate
- docs/08_CAREER_MODE.md

## Touched
- game/scripts/sim/SimRunState.cs — added `public double ReputationDemandMultiplier=1.0;`
  and `*ReputationDemandMultiplier` in RatePerSimMinute(). Neutral at default 1.0.

## Run the gate locally
    cd tools/engine-selftest && dotnet run -c Release
Expect: "SELF-TEST TOTAL: 120/120", "CAREER-TEST TOTAL: 11/11", "RESULT: PASS".
Writes career_week_summary.json (the 7-day sample) next to the harness.

## Wire the Godot layer (in-editor — the pending step)
1. Register game/scripts/sim/CareerHook.cs as autoload "CareerHook".
2. Main._Ready(), before starting the sim:
     GetNode<CareerHook>("/root/CareerHook").ConfigureSim(sim);
3. Bind F6 in Main's input handler:
     GetNode<CareerHook>("/root/CareerHook").AdvanceDay(sim);
Then open game/ in Godot 4.6, press F6 across a few days, confirm reputation
evolves and user://career/career_state.json updates.

## IMPORTANT (carried from RS-CX-001)
The delivered game/ here is the post-RS-FIELD-001 / game-0.3.0 engine (TAB _Input
interceptor, sell-through pars, fries bagging at assembly, supply runs carry Raw).
Merge this drop-in over the same synced tree; do not apply over a 0.2.0 game/.

## Next recommended task
In-editor smoke test of CareerHook (above), then RS-RM-002 candidates: surface
reputation/day in the dashboard HUD, and a multi-week season rollup for ASC
trend analysis.
