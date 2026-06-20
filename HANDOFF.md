# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Current HEAD: `0d8c2595b39af52f09d5c8b368819240af5abc06`
Task ID: `layout-interaction-functional-pass`
Task Name: Seating, object collision, POS location, employee activity, kitchen/office layout, lobby furniture, walk-in scale, model scale
Status: `CLAUDE_REVIEW_CLEARED_AWAITING_MICHAEL_OV2_DSTALL_MERGE`
Handoff Version: 7.0
Last Updated: 2026-06-19
Updated By: Claude Opus

## Workflow Contract

HANDOFF.md is the permanent, revolving coordination channel between Claude Opus and Codex. It describes only the current task state; stale detail is removed each update; Git history preserves prior versions.

- Michael owns product intent, material decisions, merge/release approval, final acceptance.
- Claude Opus owns task definition, architecture/risk review, acceptance criteria, packet/implementation review.
- Codex owns repository inspection, source implementation, test execution, implementation evidence.
- Codex must not edit source until Claude approves the packet and Michael approves the spec.
- Codex must not author or sign the "Claude Review" section; if a fresh review is needed, leave it `PENDING_CLAUDE_REVIEW`.
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
- [x] Movement parser — PASS at HEAD on full **non-headless** smoke `20260619_222253`: 323 / 1958 / 0. The earlier `205213` mobile-entry stall did not reproduce in the rendered run.
- [x] Build / self-test / byte-identical / full smoke / packaging / compat — re-run at HEAD; evidence in Verification Gate. Determinism hash matches base (no sim-stream leakage).
- [x] Seat-reach, height-parity, POS-right-end assertions re-confirmed in HEAD full smoke `20260619_222253`.
- [x] Menu render (R4) — `lobby_menu.png` visible in non-headless close-up `menu-render-check/menu_board_closeup_inside.png`.
- [~] Michael visual smoke (OV2, BINDING) — pending; fresh HEAD screenshots available under `20260619_222253/` + menu close-up.

## Required Before Re-Review (Codex) — all CLOSED at HEAD

R1 — CLOSED. Full Verification Gate run at HEAD `0d8c259`; populated below.
R2 — CLOSED. `AllJsonl` normal_day/12345 hash at HEAD `05464c88…e83d18b6` — byte-identical to base.
R3 — CLOSED (non-reproduced, see D-STALL). Stall did not reproduce in full non-headless smoke; parser 323/1958/0.
R4 — CLOSED. Real non-headless menu rendering confirmed.

## Decisions Reserved for Michael

- D-STALL — Status: READY_FOR_MICHAEL.
  Result: the `cust_ord_000002` mobile-entry stall did not reproduce in the full rendered smoke; the gate is green without it. It is therefore NOT a current blocking failure.
  Claude position: record it as **non-reproduced, not resolved** — one clean full run is not proof an intermittent presentation-layer symptom is gone, and this is the same customer with prior entry-pathing history; causation (this task's nav changes vs. a latent prior-task mobile-entry edge) is unanswered, acceptable only because the layout criteria pass independently.
  Claude recommendation: do not block this task on it, but before merge either (a) run 2–3 additional full smokes to confirm it stays clean, or (b) split a small `mobile-entry-stall-watch` follow-up. Michael chooses (a), (b), or accept-as-is.

## Claude Review

Verdict: `APPROVED_TO_CONTINUE` (Claude review gate cleared; advances to Michael for OV2 + D-STALL + merge). No prior verdict inherited; this is a fresh review of HEAD `0d8c259`.

The v6.1 gate is real, current, and comprehensive: every check carries a command, exit code, report path, and this commit's SHA. The implementation concerns from v6.0 are answered. Strengths verified at HEAD:
- Determinism: `AllJsonl` hash is byte-identical to base — the correct pass condition; confirms the layout work did not leak into the sim event stream.
- The full non-headless movement smoke is green (323/1958/0) and exercises the actual layout changes (seat-reach, POS-right-end `+X`, height-parity 1.72m, OV1 office round-trip), not just the untouched sim core.
- The menu-loading change is confirmed safe in a real rendered close-up (R4).
- Codex surfaced the headless stall honestly rather than burying it, and did not fake the gate.

Non-blocking notes for the record:
1. D-STALL (above) — Michael's call; non-reproduced ≠ resolved.
2. Auditability: deterministic replay was produced by a temporary out-of-repo helper rather than the canonical `engine-selftest` byte-identical assertion. The matching hash makes it credible; routing it through the repo tool would be more auditable. Confirm whether the 120/120 self-test already embeds the canonical byte-identical check.
3. "Static Analysis" is `py_compile` of the parser script only — it does not analyze the C# game code (the build's warn-as-error on NU1605/SYSLIB0011 is the C# guard).

Reviewed By: Claude Opus — 2026-06-19.

## Codex Implementation Summary

Changed files (allowed paths): `WorldBuilder.cs` (typed seat metadata; POS `+X`; fryer to office wall; walk-in 110%; office door/window/desk; employee-only break booth; reworked lobby furniture; furniture obstacle proxies; `lobby_menu.png` `ResourceLoader`→`ImageTexture`). `CrowdCoordinator.cs` (typed seats; break booth slots; right-end POS slots; emits `ApparentHeight`). `CustomerAgent.cs` (seated yaw + tray target; align + place tray). `AgentManager.cs` (`CharacterTargetHeight=1.72f`; fixed customer scale). `CharacterRig.cs` (AABB/fallback height normalization; bounded work fallback; no `GlobalTransform` before tree entry). `assert_movement.py` (dining-seat, POS-right-end, height-parity, one-sample exemption; manager-office round-trip probe support). `movement_smoke_runner.gd` (OV1 probe; skips screenshots under headless dummy renderer). `HANDOFF.md`.
Untracked evidence at HEAD: `test-artifacts/movement-smoke/20260619_222253/`, `test-artifacts/menu-render-check/`, `test-artifacts/compat-bundles/current-head-normal-day-csharp/`, `test-artifacts/packaging/`. Do not commit unless Michael wants artifacts retained. `CrowdCoordinator.cs.uid` left per repo convention.

## Verification Gate (run at HEAD `0d8c2595b39af52f09d5c8b368819240af5abc06`)

Build: PASSED
Unit Tests: PASSED
Integration Tests: PASSED
Static Analysis: PASSED (py_compile of parser only — see review note 3)
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
- Build: `dotnet build game\RestaurantSimulator.csproj --nologo`; exit 0; SHA `0d8c259`.
- Unit/integration/schema: `python -m unittest discover -s tests -v`; exit 0; 35 run, 14 skipped (no bundle in broad run — those 14 run under Save/Load below); SHA `0d8c259`.
- Static analysis: `python -m py_compile test-artifacts\movement-smoke\assert_movement.py`; exit 0; SHA `0d8c259`.
- Engine self-test: `dotnet run --project tools\engine-selftest\harness.csproj`; exit 0; PASS 120/120, 10/10, 11/11; SHA `0d8c259`.
- Deterministic replay: temporary out-of-repo .NET helper (same sim sources); exit 0; `AllJsonlHash=05464c88…e83d18b6`, events 8923/orders 792/completed 763, validation OK; SHA `0d8c259`.
- C# export bundle: temporary helper `Exports.BuildAll`; exit 0; 8 files → `test-artifacts/compat-bundles/current-head-normal-day-csharp`; SHA `0d8c259`.
- Save/load compat: `RS_COMPAT_BUNDLES=<…csharp>; python -m unittest tests.test_compatibility -v`; exit 0; 14 OK; SHA `0d8c259`.
- Runtime smoke: Godot 4.6.3 mono console **non-headless** `--path game --script ..\test-artifacts\movement-smoke\movement_smoke_runner.gd`; exit 0; screenshots `00_start.png`–`05_overhead.png`; `manager_office_roundtrip.json` PASS; SHA `0d8c259`.
- Runtime parser: `python test-artifacts\movement-smoke\assert_movement.py …\20260619_222253`; exit 0; `movement_summary.json` PASS 323/1958/0; SHA `0d8c259`.
- Menu render: temporary non-headless close-up runner; exit 0; `test-artifacts/menu-render-check/menu_board_closeup_inside.png`; SHA `0d8c259`.
- Packaging: `dotnet publish game\RestaurantSimulator.csproj -c Debug --nologo --no-restore -o test-artifacts\packaging\RestaurantSimulator-debug`; exit 0; SHA `0d8c259`.
- Generated file check: `git status --short --ignored`; exit 0; only expected untracked evidence + ignored build/cache; no source modified by generated output; SHA `0d8c259`.

### Gate Rule
Task may not move to READY_FOR_CLAUDE_REVIEW unless all required checks pass. Task may not move to APPROVED_FOR_MERGE unless: gate passes; Claude completes review; unresolved blocking risks are zero; Michael approves the merge. Codex: do not mark a check passed by inspection/reasoning; a check passes only when its command completes with verifiable output. Do not weaken/skip/delete failing tests to obtain a pass; if a check cannot run, mark BLOCKED with the exact reason.

## Validation Evidence (status @ HEAD)

- OV1 office round-trip `20260619_222253`: PASS at HEAD.
- Movement parser `20260619_222253`: PASS 323/1958/0 at HEAD.
- Build PASS 0/0; self-test PASS 120/120/10/10/11/11 at HEAD.
- Byte-identical hash at HEAD `05464c88…e83d18b6` — matches base.
- Compat 14/14 OK; packaging publish OK; generated-file check clean.
- Screenshots at HEAD `00_start.png`–`05_overhead.png`; menu close-up `menu_board_closeup_inside.png`.

## Known Risks (residual)

- Mobile-entry stall (`cust_ord_000002` → `mobile_entry_0`) — non-reproduced in full rendered smoke; not a current gate failure; recorded as non-reproduced, not resolved (D-STALL).
- Office window sightlines / desk facing and close-up seating/clipping remain visual-gate only (OV2).
- Deterministic replay via temporary helper rather than canonical tool (auditability; review note 2).

## Rollback

Targeted restoring commit or revert of implementation commit(s) up to `0d8c259`. No reset/rebase/discard/delete without Michael approval.

## Michael Approval

Specification Approved: `APPROVED 2026-06-19`
Material Decisions: `M1–M4 APPROVED`; height `RATIFIED 1.72m`; D-STALL `READY_FOR_MICHAEL`
Human Visual Smoke: `PENDING` (binding — OV2; fresh HEAD screenshots available)
Merge Approved: `PENDING` (gate PASSED + Claude review cleared; awaits OV2 + D-STALL + Michael approval)

## Next Authorized Action

Michael: (1) decide D-STALL — accept-as-is, request 2–3 confirmation smokes, or split a watch follow-up; (2) perform the binding OV2 visual smoke on the HEAD screenshots (seating alignment, right-end POS service, work animation, office windows/door/desk, furniture clipping); (3) grant merge if both close. No further source edits unless OV2 or D-STALL surfaces a defect, which would scope a new packet.
