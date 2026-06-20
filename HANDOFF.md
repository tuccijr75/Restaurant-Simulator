# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Current HEAD: `dd7bcbaaf72defdcb426598361851ca55cf3a2c8`
Task ID: `layout-interaction-functional-pass`
Task Name: Seating, object collision, POS location, employee activity, kitchen/office layout, lobby furniture, walk-in scale, model scale
Status: `REWORK_REQUIRED_GATE_RERUN_AND_STALL_TRIAGE`
Handoff Version: 6.0
Last Updated: 2026-06-19
Updated By: Claude Opus

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

## Acceptance Criteria (verification state @ HEAD `dd7bcba`)

- [x] Manager office-door reachability (OV1) — machine-proven at HEAD: `20260619_205213/manager_office_roundtrip.json` PASS (`emp_28` depart 216, max 2.002m, return 399).
- [!] Movement parser — FAILS at HEAD on `20260619_205213`: `cust_ord_000002 stuck 4.03s at 3.90m` entering `mobile_entry_0`. (That run is a 72-sample headless probe, not a full smoke.)
- [stale] Build / self-test / byte-identical / full movement smoke (378/2328/0) / screenshots — green results are from prior commit `b4f8b98` (run `200008`); byte-identical NOT re-run after the v5.1 `WorldBuilder` menu change; screenshots skipped under headless. Must be re-run at HEAD.
- [x] (carried) Seat-reach, height-parity, POS-right-end assertions existed green at `200008` — must be re-confirmed in the HEAD full smoke.
- [~] Michael visual smoke (OV2, BINDING) — pending; no fresh screenshots exist at HEAD.

## Required Before Re-Review (Codex)

R1 — Run the full Verification Gate at `dd7bcba` and populate the gate evidence (command, exit code, report path, commit SHA) for each check. No PENDING/NOT_RUN at re-review.
R2 — Re-run the byte-identical `AllJsonl` check at HEAD (after the v5.1 menu-loading change) and record the hash + commit SHA.
R3 — Reproduce/triage the `cust_ord_000002` mobile-entry stall in a FULL-length smoke (not the 72-sample headless probe). Report: is it deterministic/persistent, and is it caused by THIS task's nav/anchor changes or a latent mobile-entry issue from the prior task?
R4 — Confirm the `lobby_menu.png` `ResourceLoader→ImageTexture` change does not alter real (non-headless) menu rendering; if it does, scope it to headless only. (Made for the harness; must not change production visuals.)

## Decisions Reserved for Michael

- D-STALL — After R3 triage: if the stall reproduces in a full smoke and traces to this task's changes, it BLOCKS and is fixed in this task; if it is independent/latent, split it into its own mobile-entry packet so the layout task may merge once the gate is otherwise green at HEAD. Claude recommendation pending triage. Status: OPEN (do not decide before R3).

## Claude Review

Verdict: `REWORK_REQUIRED` (narrow — verification at HEAD + stall triage + menu-change confirmation; NOT a redesign).

Note: the v5.1 file presented to review still carried a stale `APPROVED_TO_CONTINUE` verdict and an approval-presupposing Next Action. Both are void on this commit and are corrected here — there is no standing approval while a required check fails at HEAD.

Accepted: OV1 office-door reachability is genuinely closed (machine-proven at HEAD). Codex also behaved correctly under the gate rule — it did not falsely mark the gate passed and it surfaced the failing stall rather than hiding it.

Not approvable for merge because, per Michael's evidence rule: (1) the current commit FAILS the movement parser (the mobile-entry stall); (2) the Verification Gate is NOT_RUN and the green build/self-test/byte-identical/full-smoke/screenshot evidence is STALE — from prior commit `b4f8b98`/run `200008`, and the byte-identical was not re-run after the v5.1 menu change; (3) no fresh visual evidence exists at HEAD (headless skipped screenshots). R1–R4 close these; D-STALL is Michael's once R3 is in.

Also flag (scope): the `lobby_menu.png` loading change is a new, harness-motivated change not in the original packet — allowed path, but it must be confirmed real-game-safe (R4).

Reviewed By: Claude Opus — 2026-06-19.

## Codex Implementation Summary

Changed files (allowed paths): `WorldBuilder.cs` (typed seat metadata; POS `+X`; fryer to office wall; walk-in 110%; office door/window/desk; employee-only break booth; reworked lobby furniture; furniture obstacle proxies; v5.1: `lobby_menu.png` `ResourceLoader`→`ImageTexture` to remove a headless load error). `CrowdCoordinator.cs` (typed seats; break booth slots; right-end POS slots; emits `ApparentHeight`). `CustomerAgent.cs` (seated yaw + tray target; align + place tray). `AgentManager.cs` (`CharacterTargetHeight=1.72f`; fixed customer scale). `CharacterRig.cs` (AABB/fallback height normalization; bounded work fallback; no `GlobalTransform` before tree entry). `assert_movement.py` (dining-seat, POS-right-end, height-parity, one-sample exemption; v5.1 manager-office round-trip probe support). `movement_smoke_runner.gd` (v5.1 OV1 probe; skips screenshots under headless dummy renderer). `HANDOFF.md`.
Untracked evidence: `…/194415/194834/195344/200008/204625/205038/205213`; `204625`/`205038` are failed intermediate OV1 attempts; `205213` is current. Do not commit unless Michael wants artifacts retained. `CrowdCoordinator.cs.uid` left per repo convention.

## Verification Gate (must be run at HEAD `dd7bcba`)

Build: PENDING
Unit Tests: PENDING
Integration Tests: PENDING
Static Analysis: PENDING
Schema Validation: PENDING
Deterministic Replay: PENDING
Save/Load Compatibility: PENDING
Runtime Smoke Test: FAILING on last run `205213`
Packaging: PENDING
Generated File Check: PENDING

### Gate Result
Status: FAILED
Allowed: NOT_RUN | FAILED | PASSED | BLOCKED
Evidence:
- Command: `python test-artifacts\movement-smoke\assert_movement.py test-artifacts\movement-smoke\20260619_205213`
- Exit code: non-zero (1 failure)
- Log/report path: `test-artifacts/movement-smoke/20260619_205213/` (+ `manager_office_roundtrip.json` PASS)
- Commit SHA: `dd7bcbaaf72defdcb426598361851ca55cf3a2c8`

### Gate Rule
Task may not move to READY_FOR_CLAUDE_REVIEW unless all required checks pass. Task may not move to APPROVED_FOR_MERGE unless: gate passes; Claude completes review; unresolved blocking risks are zero; Michael approves the merge. Codex: do not mark a check passed by inspection/reasoning; a check passes only when its command completes with verifiable output. Do not weaken/skip/delete failing tests to obtain a pass; if a check cannot run, mark BLOCKED with the exact reason.

## Validation Evidence (status @ HEAD)

- OV1 office round-trip: `20260619_205213/manager_office_roundtrip.json` PASS (current commit). ACCEPTED.
- Movement parser `205213`: FAIL 72/384/1 (`cust_ord_000002` stall). CURRENT.
- v5.1 build PASS 0/0 and self-test PASS 120/120/10/10/11/11 reported by Codex — record with commit SHA in the gate (R1).
- Byte-identical hash `05464C88…E83D18B6` and full smoke `200008` (378/2328/0) — PRIOR commit `b4f8b98`, STALE; re-run at HEAD (R1/R2).
- Screenshots: none at HEAD (headless skipped).

## Known Risks (residual)

- Mobile-entry stall (`cust_ord_000002` entering `mobile_entry_0`) — unknown if persistent or this-task-caused (R3).
- `lobby_menu.png` loading change may alter real-game menu rendering (R4).
- Office window sightlines / desk facing and close-up seating/clipping remain visual-gate only (OV2).

## Rollback

Targeted restoring commit or revert of implementation commit(s) up to `dd7bcba`. No reset/rebase/discard/delete without Michael approval.

## Michael Approval

Specification Approved: `APPROVED 2026-06-19`
Material Decisions: `M1–M4 APPROVED`; height `RATIFIED 1.72m`; D-STALL `PENDING` (after R3)
Human Visual Smoke: `PENDING` (binding — OV2, needs fresh HEAD screenshots)
Merge Approved: `PENDING` (blocked by FAILED gate)

## Next Authorized Action

Codex completes R1–R4: run the full Verification Gate at `dd7bcba` with recorded evidence; re-run byte-identical at HEAD; reproduce/triage the mobile-entry stall in a full smoke; confirm the menu change is real-game-safe. Then Claude re-reviews and Michael decides D-STALL. No merge until the gate is PASSED at HEAD, blocking risks are zero, and Michael's OV2 smoke is clean.
