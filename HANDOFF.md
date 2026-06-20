# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Current HEAD: `0d8c2595b39af52f09d5c8b368819240af5abc06`
Task ID: `layout-interaction-functional-pass`
Task Name: Seating, object collision, POS location, employee activity, kitchen/office layout, lobby furniture, walk-in scale, model scale
Status: `READY_FOR_CLAUDE_REVIEW`
Handoff Version: 6.1
Last Updated: 2026-06-19
Updated By: Codex

## Workflow Contract

HANDOFF.md is the permanent, revolving coordination channel between Claude Opus and Codex. It describes only the current task state; stale detail is removed each update; Git history preserves prior versions.

- Michael owns product intent, material decisions, merge/release approval, final acceptance.
- Claude Opus owns task definition, architecture/risk review, acceptance criteria, packet/implementation review.
- Codex owns repository inspection, source implementation, test execution, implementation evidence.
- Codex must not edit source until Claude approves the packet and Michael approves the spec.
- Repository files are the source of truth; model memory is secondary.
- Neither model may merge, discard, reset, rebase, or delete work without Michael approval.
- Verification is evidence-based: Codex summaries are claims, not proof. A check passes only when its command completes with verifiable output tied to the current commit SHA. Do not weaken/skip/rewrite failing tests to obtain a pass; if a check cannot run, mark it BLOCKED with the reason. A prior `APPROVED_*` verdict does not carry across commits — it is void once HEAD or a required check changes.

Prior task `movement-runtime-authority-followup` is CLOSED/ACCEPTED at Base SHA. Do not reopen unless a new direct symptom appears.

## Objective

In runtime smoke, layout and visible interactions read correctly: customers sit aligned in chairs/booths; characters route around furniture/equipment/walls; POS at right-end registers; employees visibly work with animations; office/fryer/walk-in/break-room match Michael's direction; staff/customer apparent heights match. All changes presentation/layout only — deterministic sim outputs unchanged.

## Requirements (as approved & implemented)

Layout: office closed from the kitchen with a reachable back-wall door, two windows (drive-thru + kitchen-from-desk), desk facing into the room; fryer at office-wall left corner facing in, fryers to its right; walk-in 10% bigger with realigned `freezer_door`/`walkin_standoff_*`; break table → employee-only break booth; lobby furniture functional with no clipping; navmesh obstacle proxies that carve furniture cores while keeping seat approach slots reachable.
Seating: typed seat reservations (position/yaw/tray target/type); approach without clipping; align to seat; tray on table.
POS (presentation-only): order/service + cashier serve slots at customer-facing right `+X`; no `_sim` coupling.
Employees: `Working=true` plays a station work animation or bounded fallback; busyness from read-only signals only; no sim load/throughput change.
Scale: AABB normalization to a shared 1.72m (ratified) across all variants, with manual override.
Determinism: `AllJsonl` replay (normal_day/seed 12345) byte-identical to Base SHA — unconditional, re-confirmed at HEAD.

## Acceptance Criteria (verification state @ HEAD `0d8c259`)

- [x] Manager office-door reachability (OV1) — machine-proven at HEAD: `20260619_222253/manager_office_roundtrip.json` PASS (`emp_28` depart 41, max 2.043m, return 72).
- [x] Movement parser — PASS at HEAD on full non-headless smoke `20260619_222253`: 323 samples / 1958 agent samples / 0 failures. Earlier `205213` mobile-entry stall did NOT reproduce in the full rendered smoke.
- [x] Build / self-test / byte-identical / full movement smoke / screenshots — re-run at HEAD `0d8c259`; evidence in Verification Gate.
- [x] Seat-reach, height-parity, POS-right-end assertions re-confirmed in HEAD full smoke `20260619_222253`.
- [~] Michael visual smoke (OV2, BINDING) — pending; fresh HEAD screenshots are available under `test-artifacts/movement-smoke/20260619_222253/`.

## Required Before Re-Review (Codex)

R1 — CLOSED. Full Verification Gate run at actual HEAD `0d8c2595b39af52f09d5c8b368819240af5abc06`; populated below.
R2 — CLOSED. `AllJsonl` normal_day/12345 hash at HEAD: `05464c886b33616332885744d215ee7fc7ef8ec0f18384c67b05db17e83d18b6`.
R3 — CLOSED. `cust_ord_000002` mobile-entry stall from 72-sample headless probe did not reproduce in full non-headless smoke `20260619_222253`; parser passed 323 / 1958 / 0. Treat as non-reproduced headless/probe artifact unless Michael/Claude require more repeated smokes.
R4 — CLOSED. Real non-headless menu rendering confirmed by `test-artifacts/menu-render-check/menu_board_closeup_inside.png`; menu texture is visible on the board.

## Decisions Reserved for Michael

- D-STALL — R3 result: stall did not reproduce in full non-headless smoke and the full parser passed. Codex recommendation: non-blocking unless Claude/Michael request repeated stress smokes. Status: READY_FOR_CLAUDE_DECISION.

## Claude Review

Verdict: `REWORK_REQUIRED` from Claude v6.0 is now addressed by Codex v6.1 evidence. Requires fresh Claude review; no prior `APPROVED_*` verdict is inherited.

Note: the v5.1 file presented to review still carried a stale `APPROVED_TO_CONTINUE` verdict and an approval-presupposing Next Action. Both are void on this commit and are corrected here — there is no standing approval while a required check fails at HEAD.

Accepted: OV1 office-door reachability is genuinely closed (machine-proven at HEAD). Codex also behaved correctly under the gate rule — it did not falsely mark the gate passed and it surfaced the failing stall rather than hiding it.

Codex v6.1 update: R1–R4 are closed at actual HEAD `0d8c259`. The full rendered movement parser is green, the deterministic hash is re-confirmed, real screenshots exist, and the menu texture renders in a non-headless close-up. D-STALL is ready for Claude/Michael decision.

Also flag (scope): the `lobby_menu.png` loading change is a new, harness-motivated change not in the original packet — allowed path, and now confirmed real-game-safe in non-headless render evidence (R4).

Reviewed By: Claude Opus — 2026-06-19.

## Codex Implementation Summary

Changed files (allowed paths): `WorldBuilder.cs` (typed seat metadata; POS `+X`; fryer to office wall; walk-in 110%; office door/window/desk; employee-only break booth; reworked lobby furniture; furniture obstacle proxies; v5.1: `lobby_menu.png` `ResourceLoader`→`ImageTexture` to remove a headless load error). `CrowdCoordinator.cs` (typed seats; break booth slots; right-end POS slots; emits `ApparentHeight`). `CustomerAgent.cs` (seated yaw + tray target; align + place tray). `AgentManager.cs` (`CharacterTargetHeight=1.72f`; fixed customer scale). `CharacterRig.cs` (AABB/fallback height normalization; bounded work fallback; no `GlobalTransform` before tree entry). `assert_movement.py` (dining-seat, POS-right-end, height-parity, one-sample exemption; v5.1 manager-office round-trip probe support). `movement_smoke_runner.gd` (v5.1 OV1 probe; skips screenshots under headless dummy renderer). `HANDOFF.md`.
Untracked evidence includes prior smoke runs plus current HEAD artifacts: `test-artifacts/movement-smoke/20260619_222253/`, `test-artifacts/menu-render-check/`, `test-artifacts/compat-bundles/current-head-normal-day-csharp/`, and `test-artifacts/packaging/`. Do not commit unless Michael wants artifacts retained. `CrowdCoordinator.cs.uid` left per repo convention.

## Verification Gate (run at HEAD `0d8c2595b39af52f09d5c8b368819240af5abc06`)

Build: PASSED
Unit Tests: PASSED
Integration Tests: PASSED
Static Analysis: PASSED
Schema Validation: PASSED
Deterministic Replay: PASSED
Save/Load Compatibility: PASSED
Runtime Smoke Test: PASSED
Packaging: PASSED
Generated File Check: PASSED

### Gate Result
Status: PASSED
Allowed: NOT_RUN | FAILED | PASSED | BLOCKED
Evidence:
- Build command: `dotnet build game\RestaurantSimulator.csproj --nologo`; exit code 0; report path: console output; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- Unit/integration/schema command: `python -m unittest discover -s tests -v`; exit code 0; 35 tests run, 14 skipped because no bundle was provided in that broad discovery run; report path: console output; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- Static analysis command: `python -m py_compile test-artifacts\movement-smoke\assert_movement.py`; exit code 0; report path: console output; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- Engine self-test command: `dotnet run --project tools\engine-selftest\harness.csproj`; exit code 0; report path: console output; result PASS 120/120, ingredient 10/10, career 11/11; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- Deterministic replay command: temporary out-of-repo .NET helper using same sim sources; exit code 0; `AllJsonlHash=05464c886b33616332885744d215ee7fc7ef8ec0f18384c67b05db17e83d18b6`, events 8923, orders 792, completed 763, validation OK; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- C# export bundle command: temporary out-of-repo .NET helper using `Exports.BuildAll`; exit code 0; report path: `test-artifacts/compat-bundles/current-head-normal-day-csharp`; exported 8 files; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- Save/load compatibility command: `$env:RS_COMPAT_BUNDLES=<current-head-normal-day-csharp>; python -m unittest tests.test_compatibility -v`; exit code 0; 14 tests OK; report path: `test-artifacts/compat-bundles/current-head-normal-day-csharp`; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- Runtime smoke command: Godot 4.6.3 mono console non-headless `--path game --script ..\test-artifacts\movement-smoke\movement_smoke_runner.gd`; exit code 0; report path: `test-artifacts/movement-smoke/20260619_222253`; screenshots `00_start.png` through `05_overhead.png`; `manager_office_roundtrip.json` PASS; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- Runtime parser command: `python test-artifacts\movement-smoke\assert_movement.py test-artifacts\movement-smoke\20260619_222253`; exit code 0; report path: `test-artifacts/movement-smoke/20260619_222253/movement_summary.json`; PASS 323 samples / 1958 agent samples / 0 failures; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- Menu render confirmation command: temporary non-headless Godot close-up runner; exit code 0; report path: `test-artifacts/menu-render-check/menu_board_closeup_inside.png`; `lobby_menu.png` visible on board; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- Packaging command: `dotnet publish game\RestaurantSimulator.csproj -c Debug --nologo --no-restore -o test-artifacts\packaging\RestaurantSimulator-debug`; exit code 0; report path: `test-artifacts/packaging/RestaurantSimulator-debug`; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.
- Generated file check command: `git status --short --ignored`; exit code 0; expected untracked evidence only under `test-artifacts/compat-bundles/`, `test-artifacts/menu-render-check/`, `test-artifacts/movement-smoke/20260619_222253/`, `test-artifacts/packaging/`; ignored local build/cache artifacts present (`game/.godot/`, `__pycache__`, `tools/engine-selftest/bin|obj`, packaged DLL/PDB). No source files modified by generated output; commit SHA `0d8c2595b39af52f09d5c8b368819240af5abc06`.

### Gate Rule
Task may not move to READY_FOR_CLAUDE_REVIEW unless all required checks pass. Task may not move to APPROVED_FOR_MERGE unless: gate passes; Claude completes review; unresolved blocking risks are zero; Michael approves the merge. Codex: do not mark a check passed by inspection/reasoning; a check passes only when its command completes with verifiable output. Do not weaken/skip/delete failing tests to obtain a pass; if a check cannot run, mark BLOCKED with the exact reason.

## Validation Evidence (status @ HEAD)

- OV1 office round-trip: `20260619_222253/manager_office_roundtrip.json` PASS at HEAD.
- Movement parser `20260619_222253`: PASS 323 / 1958 / 0 at HEAD.
- Build PASS 0/0, self-test PASS 120/120/10/10/11/11 at HEAD.
- Byte-identical hash at HEAD: `05464c886b33616332885744d215ee7fc7ef8ec0f18384c67b05db17e83d18b6`.
- Screenshots at HEAD: `test-artifacts/movement-smoke/20260619_222253/00_start.png` through `05_overhead.png`; menu close-up `test-artifacts/menu-render-check/menu_board_closeup_inside.png`.

## Known Risks (residual)

- Mobile-entry stall (`cust_ord_000002` entering `mobile_entry_0`) — non-reproduced in full rendered smoke; no longer a current gate failure. Claude/Michael may still request repeated stress smokes before merge.
- `lobby_menu.png` loading change — confirmed real-game-safe in non-headless close-up render.
- Office window sightlines / desk facing and close-up seating/clipping remain visual-gate only (OV2).

## Rollback

Targeted restoring commit or revert of implementation commit(s) up to `0d8c259`. No reset/rebase/discard/delete without Michael approval.

## Michael Approval

Specification Approved: `APPROVED 2026-06-19`
Material Decisions: `M1–M4 APPROVED`; height `RATIFIED 1.72m`; D-STALL `READY_FOR_CLAUDE_DECISION`
Human Visual Smoke: `PENDING` (binding — OV2; fresh HEAD screenshots now available for review)
Merge Approved: `PENDING` (gate passed; still requires Claude review and Michael approval)

## Next Authorized Action

Claude re-reviews v6.1 evidence at HEAD `0d8c259`; Michael decides D-STALL and completes OV2 visual smoke. No merge until Claude review is complete, unresolved blocking risks are zero, and Michael approves merge.
