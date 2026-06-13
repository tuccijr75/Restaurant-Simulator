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

---

## UPDATE (F6 fix — self-sufficient autoload)
Symptom reported: F6 did not advance the day. Diagnosis from the uploaded run:
- Godot logs had zero `[CareerHook]` lines  -> the autoload was never registered/loaded.
- F5 export was `sim_normal_day_12345` (default seed) -> ConfigureSim never ran.
Root cause: the prior CareerHook required hand-editing Main.cs AND registering an
autoload; neither was done.

Fix: CareerHook.cs rewritten to be self-sufficient — NO Main.cs edits.
- Finds SimRunState by reflection (any node/field/property).
- Configures day scenario/seed/multiplier in _Process before start (EventSeq==0 guard).
- Handles F6/F7 in _Input (autoload _Input runs ahead of Main's focus-interceptor).
Setup is now a single autoload registration (see QUICKSTART_CAREER_F6.md).
Confirmation after registering: F5 export reads sim_normal_day_132195522 for day 0.
Engine gate re-run after the change: 120/120 + 11/11 PASS (CareerHook is Godot-only).
