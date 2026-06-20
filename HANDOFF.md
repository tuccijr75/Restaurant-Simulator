# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Current HEAD: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`  (implementation appears UNCOMMITTED — see Claude Review / commit-state note)
Task ID: `layout-interaction-functional-pass`
Task Name: Seating, object collision, POS location, employee activity, kitchen/office layout, lobby furniture, walk-in scale, model scale
Status: `AWAITING_MICHAEL_VISUAL_SMOKE_AND_MERGE`
Handoff Version: 4.0
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

Prior task `movement-runtime-authority-followup` is CLOSED/ACCEPTED at Base SHA. Do not reopen unless a new direct symptom appears.

## Objective

In runtime smoke, layout and visible interactions read correctly: customers sit aligned in real chairs/booths; characters route around furniture/equipment/walls; POS service at the right-end registers; employees visibly work with animations; office/fryer/walk-in/break-room match Michael's direction; staff and customer apparent heights match. All changes presentation/layout only — deterministic sim outputs unchanged.

## Requirements (as approved & implemented)

Layout / nav:
- Office closed from the kitchen production area, with a back-wall door (all the way back, facing the kitchen/back) so MOD pathing is reachable; two large office windows (one toward drive-thru, one into the kitchen from the seated position); desk against the back wall facing into the room.
- Fryer station against the office wall, left corner, facing into the kitchen; fryers directly to its right. Station keys stable; only positions/anchors moved.
- Walk-in 10% bigger; `freezer_door` anchor and `walkin_standoff_*`/supply slots realigned to the resized door; no fake door; movement parser re-run for no regression.
- Break table replaced with an employee-only break booth; break slots moved to it.
- Lobby furniture reworked into functional groups with clear aisles, no clipping.
- Navmesh obstacle proxies carve furniture/equipment cores while keeping seat/interaction approach slots reachable.

Seating: typed seat reservations (position, seated yaw, tray/table target, furniture type); customers approach without passing through furniture, align to the seat, place tray/cup on the table.

POS (presentation-only): order/service + cashier serve slots at the customer-facing right (`+X`) registers; no `_sim` coupling.

Employees: `Working=true` selects a station work animation with a bounded procedural fallback; visible busyness from read-only signals only (sim load, reservation, `Phase`); no sim load/throughput change.

Scale: AABB normalization to a single shared target height across all variants, with manual override. (Target landed at 1.72m — see ratification item.)

Determinism: `AllJsonl` replay (normal_day/seed 12345) byte-identical to Base SHA — unconditional.

## Acceptance Criteria (verification state)

- [x] Build zero errors; Godot runs with no new ERROR/SCRIPT ERROR/Exception/WARN in `godot.log`.
- [x] Self-test passes; `AllJsonl` byte-identical to Base SHA (hash `05464C88…E83D18B6`).
- [x] Movement parser green on fresh smoke `20260619_200008` (378 / 2328 samples / 0 failures) — no new stalls/jitter/walk-in regression.
- [x] Seat-reach + seat-alignment assertion (caught a real reach failure mid-pass, fixed, now green).
- [x] Staff/customer apparent-height parity (employee 1.98/1.96→1.72, customers 1.00→1.72).
- [x] Counter order/serve slots at confirmed right-end (`+X`).
- [~] Screenshots: only `05_overhead.png` reviewed by Codex; full 8-view set + office close-ups are Michael's gate.
- [~] Manual visual smoke (Michael, BINDING final gate): seating alignment, POS service, work animation, office windows/door/desk, furniture clipping, manager reaching the office.

## Decisions / Ratifications

- M1 office access, M2 right=`+X`, M3 shared height, M4 employee-only break booth — all APPROVED by Michael.
- RATIFY (M3 deviation): target height is **1.72m shared**, which grew customers from ~1.5m. M3 said "normalize to customer height." Michael: accept 1.72m shared, or re-target to the prior customer apparent height (customers unchanged, employees only). Status: OPEN.

## Claude Review

Verdict: `APPROVED_TO_CONTINUE` (Claude review gate cleared; advances to Michael's visual smoke + merge).

Implementation is clean and scope-correct (only allowed paths touched). Strengths: the machine assertions did real work — the seat-reach check went red on the obstacle pass and was fixed before green (the exact unreachable-seat trap I flagged); determinism is byte-identical with a recorded hash; the movement parser stayed green so the walk-in resize and equipment moves did not regress the prior task; height normalization is AABB-measured and verified across all GLB variants; the runtime log was checked clean.

Items for Michael / Codex before merge (none are code rework):
1. (Michael) RATIFY the 1.72m height deviation from M3 (above) — it changed customer apparent size.
2. (Michael) Visual smoke is the BINDING gate for the new office geometry (window sightlines, desk facing, reachable back-wall door) and close-up seating/POS/animation — none of which an overhead or the machine checks confirm. Confirm a manager actually reaches the office in the run (M1's purpose).
3. (Codex) Commit-state: Current HEAD still equals Base SHA, so the implementation is uncommitted; commit it, advance HEAD, and correct the Rollback line before merge.
4. (Codex) Confirm the parser's post-phase-change exemption is exactly one sample, not a window, so it cannot blind a real timeout.

On Michael's clean visual smoke + height ratification, this is merge-ready.

Reviewed By: Claude Opus — 2026-06-19.

## Codex Implementation Summary

Changed files (all in allowed paths): `WorldBuilder.cs` (typed seat metadata; POS anchors to `+X`; fryer/holder to office wall; walk-in 110%; office door/window/desk geometry; employee-only break booth; reworked lobby furniture; furniture obstacle proxies that carve table cores without blocking seats). `CrowdCoordinator.cs` (typed dining seats; employee-only break booth slots; POS slots follow right-end anchors; emits `ApparentHeight`). `CustomerAgent.cs` (seated yaw + tray/table target; align to seat + place tray on table). `AgentManager.cs` (shared `CharacterTargetHeight=1.72f`; fixed old customer scale). `CharacterRig.cs` (AABB/manual-fallback height normalization; bounded work fallback; no `GlobalTransform` before tree entry). `assert_movement.py` (dining-seat arrival, POS-right-end, height-parity assertions; ignore first sample after a phase change). `HANDOFF.md`.

Untracked evidence folders `20260619_194415 / 194834 / 195344 / 200008` — do not commit unless Michael wants runtime artifacts retained. `CrowdCoordinator.cs.uid` left per repo convention.

## Validation Evidence

- Build PASS 0/0. Self-test PASS 120/120, 10/10, 11/11.
- Byte-identical: Base SHA `2d07332` vs current `AllJsonl` (normal_day/seed 12345): `CANONICAL_EVENT_STREAM_BYTE_IDENTICAL: PASS 05464C886B33616332885744D215EE7FC7EF8EC0F18384C67B05DB17E83D18B6`.
- Godot smoke `20260619_200008`: normalization printed (employee_m 1.98→1.72, employee_f/shift_manager 1.96→1.72, customers 1.00→1.72); no errors/warnings in `godot.log`.
- Parser on `20260619_200008`: PASS, 378 / 2328 / 0 failures (existing rules + dining-seat + POS-right-end + height-parity).
- `05_overhead.png` reviewed (office/fryer/walk-in/lobby from overhead). Michael close-up smoke is the final visual gate.

## Tests Required

`dotnet build … --nologo`; `dotnet run --project tools\engine-selftest\harness.csproj` (+ `AllJsonl` byte-identical); Godot smoke via console exe + runner; `python … assert_movement.py <folder>`; manual screenshots; inspect `godot.log`.

## Known Risks (residual)

- New office geometry and close-up seating/clipping are visual-gate only — not machine-verified.
- 1.72m height changed customer apparent size (ratification pending).
- Uncommitted worktree — rollback plan assumes a commit that does not yet exist.

## Rollback

Once committed: targeted restoring commit or revert of the implementation commit(s) — one per group keeps reverts clean. No reset/rebase/discard/delete without Michael approval. (Note: HEAD currently still at Base SHA; commit before relying on revert.)

## Michael Approval

Specification Approved: `APPROVED 2026-06-19`
Material Decisions: `M1–M4 APPROVED`; height-deviation ratification `PENDING`
Human Visual Smoke: `PENDING` (binding)
Merge Approved: `PENDING`

## Next Authorized Action

Michael performs the binding visual smoke (office windows/door/desk + manager office access; close-up seating alignment, right-end POS service, employee work animation, furniture clipping) and ratifies the 1.72m height. Codex commits the implementation (advance HEAD, fix Rollback), confirms the parser one-sample exemption, and confirms the smoke exercised manager office access. No merge until both close.
