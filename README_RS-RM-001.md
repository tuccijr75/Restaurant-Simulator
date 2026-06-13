# RS-RM-001 drop-in — Multi-Day Career Mode

Merge these paths over your synced post-RS-FIELD-001 (game-0.3.0) tree.

## Files
- game/scripts/sim/CareerState.cs   (new) reputation model + deterministic week schedule + JSON persistence
- game/scripts/sim/CareerTest.cs    (new) headless 7-day week determinism gate
- game/scripts/sim/CareerHook.cs    (new) Godot autoload: F6 advance day + user://career persistence
- game/scripts/sim/SimRunState.cs   (edit) +ReputationDemandMultiplier (default 1.0) applied in RatePerSimMinute
- tools/engine-selftest/*           (edit) Program.cs runs 10-scenario self-test + career week
- docs/08_CAREER_MODE.md            (new) design + integration + calibration notes
- build-workflows/*                 governance (brief, audit, handoff, episode, state, current-task)

## Verify
    cd tools/engine-selftest && dotnet run -c Release
Expect: SELF-TEST 120/120, CAREER-TEST 11/11, RESULT: PASS.

## In-editor (pending)
Register CareerHook autoload; in Main._Ready call ConfigureSim(sim); bind F6 to
AdvanceDay(sim). See docs/08_CAREER_MODE.md.
