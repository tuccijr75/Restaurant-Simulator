# Handoff — RS-3D-001

## Project

Restaurant Daily Flow Simulator

## Files Modified (game/)

- scripts/sim/SimRunState.cs — fixed timestep; visual events; truck receiving; measured SOS; sales/item reconciliation; equipment-failure window + contextual recovery reasons; per-component inventory ledger; validation checkpoint rows; StringBuilder ledgers; batch-cycle cook model; assembly recalibration
- scripts/ui/ClockPanel.cs — no double-stepping when externally driven
- scripts/MainDashboard.cs — accepts a shared SimRunState
- scripts/ui/ScenarioPanel.cs — speeds 1/10/60/300/900×
- scripts/ui/ExportPanel.cs — writes the full 8-file README output contract
- scenes/Main.tscn — boots the 3D world (dashboard remains on TAB)

## Files Created

- game/scripts/sim/Exports.cs, game/scripts/sim/SelfTest.cs
- game/scripts/Main.cs, game/scripts/ui/Hud3D.cs
- game/scripts/world/{WorldBuilder, CameraDirector, CharacterRig, AgentManager, CustomerAgent, EmployeeAgent, CarAgent}.cs
- game/icon.svg
- docs/07_3D_WORLD_AND_CAMERAS.md
- tools/engine-selftest/{Program.cs, harness.csproj, nuget.config} (+ engine sources compiled by reference)
- build-workflows RS-3D-001 brief/audit/handoff/episode/receipt; updated state.json and current-task.md

## Tests Run

`dotnet run` in tools/engine-selftest: 10 scenarios × 9 checks = 90 PASS / 0 FAIL (deterministic replay hashes, ledger reconciliation, lifecycle ordering, chronology, envelope fields, no item.sold). Realism: normal_day 942 tickets / $10.73 avg check / labor 24.2% / boards clear in every scenario.

## Blockers

- One local Godot 4.6 open/run to confirm scene compile (no Godot in build environment).
- F-14..F-18 repo findings need owner decisions (see audit).

## Next Recommended Task

RS-CF-001 — load config/realism_baseline.json + human_behavior_profiles.json into the engine and replace inline constants (closes F-17, applies F-18 wage calibration).

## Controls (3D)

SPACE start/pause · TAB ops dashboard · 1–9,0 cameras · O overhead · C next cam · T tour · F free cam (WASD+QZ, Shift fast, mouse look, ESC release) · F9 self-test · F5 export contract
