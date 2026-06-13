# Current Task

Task ID: RS-RM-001
Task Brief: `build-workflows/task-briefs/RS-RM-001.md`
Branch: `main`
Lane: Engine / Godot
Status: ready_for_review
Previous Task: RS-CX-001 — ASC compatibility profile + tests (ready_for_review)
Verification: self-test 120/120 PASS (10 scenarios, seed 12345); career week 11/11 PASS (WeekSeed 777001)

## Pending (in-editor)
- Register `game/scripts/sim/CareerHook.cs` as autoload "CareerHook".
- Add the two documented call sites to Main.cs (ConfigureSim in _Ready, AdvanceDay on F6).
- Open game/ in Godot 4.6 and smoke-test F6 across a week.

## Next Task
RS-RM-002 — surface reputation/day in the dashboard HUD; multi-week season rollup for ASC trend analysis.
