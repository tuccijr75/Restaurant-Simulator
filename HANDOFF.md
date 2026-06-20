# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Current HEAD: `b4f8b98da28d61ca887ea2325847c494ef7b5792`
Task ID: `layout-interaction-functional-pass`
Task Name: Seating, object collision, POS location, employee activity, kitchen/office layout, lobby furniture, walk-in scale, model scale
Status: `AWAITING_MICHAEL_VISUAL_SMOKE_AND_MERGE`
Handoff Version: 5.0
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

Employees: `Working=true` selects a station work animation with a bounded procedural fallback; visible busyness from read-only signals only; no sim load/throughput change.

Scale: AABB normalization to a single shared target height (1.72m, ratified) across all variants, with manual override.

Determinism: `AllJsonl` replay (normal_day/seed 12345) byte-identical to Base SHA — unconditional.

## Acceptance Criteria (verification state)

- [x] Build zero errors; Godot runs with no new ERROR/SCRIPT ERROR/Exception/WARN in `godot.log`.
- [x] Self-test passes; `AllJsonl` byte-identical to Base SHA (hash `05464C88…E83D18B6`).
- [x] Movement parser green on `20260619_200008` (378 / 2328 / 0 failures) — no new stalls/jitter/walk-in regression.
- [x] Seat-reach + seat-alignment assertion (caught a real reach failure mid-pass, fixed, now green).
- [x] Staff/customer apparent-height parity at 1.72m (employee 1.98/1.96→1.72, customers 1.00→1.72) — ratified.
- [x] Counter order/serve slots at confirmed right-end (`+X`).
- [~] Manager office-access (M1 door) reachability — NOT proven; smoke shows a manager starting at `work_office_0`, not pathing through the new door. See Open Verification.
- [~] Screenshots / Michael visual smoke (BINDING): office windows/desk sightlines, close-up seating alignment, right-end POS service, employee work animation, furniture clipping.

## Open Verification (last gate before merge)

OV1 — Manager office-door reachability (M1's functional purpose). Recommended: Codex adds a targeted check in allowed paths (force a manager to leave the office and return through the new back-wall door; assert no stall), so this is machine-proven like seating/height. Alternatively Michael confirms a manager round-trips the door during the visual smoke. Status: OPEN.

OV2 — Michael visual smoke (aesthetic/close-up): office window sightlines (drive-thru + kitchen-from-desk), desk facing into the room, seated customers aligned in chairs/booths, POS service at right-end, employees animating while busy, no furniture clipping. Status: PENDING.

## Decisions / Ratifications

- M1 office access, M2 right=`+X`, M3 shared height, M4 employee-only break booth — APPROVED.
- M3 height deviation — RATIFIED by Michael: 1.72m shared accepted (customers grew from ~1.5m, accepted).

## Claude Review

Verdict: `APPROVED_TO_CONTINUE` (Claude review gate fully cleared; remaining gates are Michael's visual smoke + OV1).

Implementation is clean and scope-correct. The machine assertions did real work (seat-reach went red on the obstacle pass, fixed before green); determinism byte-identical with a recorded hash; movement parser green so the walk-in resize/equipment moves did not regress the prior task; height AABB-normalized across all variants; runtime log clean.

Closed since v4.0:
- Height ratified (1.72m) by Michael.
- Codex commit-state CLOSED — committed/pushed at `b4f8b98`, `HEAD == origin/main`, clean worktree.
- Parser one-sample exemption CLOSED — `phase_changed` is true only on the first sample after a change; timeout checks resume next sample (single sample, not a window).

Remaining before merge: OV1 (office-door reachability — recommend a targeted machine check; it is a functional, not aesthetic, requirement) and OV2 (Michael's binding visual smoke). No code rework outstanding.

Reviewed By: Claude Opus — 2026-06-19.

## Codex Implementation Summary

Changed files (all allowed paths): `WorldBuilder.cs` (typed seat metadata; POS anchors to `+X`; fryer/holder to office wall; walk-in 110%; office door/window/desk geometry; employee-only break booth; reworked lobby furniture; furniture obstacle proxies that carve cores without blocking seats). `CrowdCoordinator.cs` (typed dining seats; employee-only break booth slots; POS slots follow right-end anchors; emits `ApparentHeight`). `CustomerAgent.cs` (seated yaw + tray/table target; align + place tray). `AgentManager.cs` (shared `CharacterTargetHeight=1.72f`; fixed old customer scale). `CharacterRig.cs` (AABB/manual-fallback height normalization; bounded work fallback; no `GlobalTransform` before tree entry). `assert_movement.py` (dining-seat, POS-right-end, height-parity assertions; one-sample post-phase-change exemption). `HANDOFF.md`.

Untracked evidence folders `20260619_194415/194834/195344/200008` — do not commit unless Michael wants artifacts retained. `CrowdCoordinator.cs.uid` left per repo convention.

## Validation Evidence

- Build PASS 0/0. Self-test PASS 120/120, 10/10, 11/11.
- Byte-identical: Base SHA `2d07332` vs current `AllJsonl` (normal_day/seed 12345): `CANONICAL_EVENT_STREAM_BYTE_IDENTICAL: PASS 05464C886B33616332885744D215EE7FC7EF8EC0F18384C67B05DB17E83D18B6`.
- Godot smoke `20260619_200008`: normalization printed (employee 1.98/1.96→1.72, customers 1.00→1.72); no errors/warnings in `godot.log`.
- Parser on `20260619_200008`: PASS 378 / 2328 / 0 failures (existing + dining-seat + POS-right-end + height-parity).
- `05_overhead.png` reviewed (office/fryer/walk-in/lobby overhead). Manager office-door round-trip not exercised — OV1.

## Tests Required

`dotnet build … --nologo`; `dotnet run --project tools\engine-selftest\harness.csproj` (+ `AllJsonl` byte-identical); Godot smoke via console exe + runner; `python … assert_movement.py <folder>`; manual screenshots; inspect `godot.log`. Add OV1 manager-round-trip check.

## Known Risks (residual)

- OV1: a sealed office with an unreachable door would only surface when a manager patrols out and cannot return; not yet exercised.
- Office window sightlines / desk facing and close-up seating/clipping are visual-gate only.

## Rollback

Targeted restoring commit or revert of implementation commit `b4f8b98da28d61ca887ea2325847c494ef7b5792`. No reset/rebase/discard/delete without Michael approval.

## Michael Approval

Specification Approved: `APPROVED 2026-06-19`
Material Decisions: `M1–M4 APPROVED`; height deviation `RATIFIED 1.72m`
Human Visual Smoke: `PENDING` (binding — OV2)
Merge Approved: `PENDING`

## Next Authorized Action

Close OV1 (Codex adds the manager office-door round-trip check, or Michael confirms it visually) and OV2 (Michael's binding visual smoke). On both clean, Michael grants merge approval and the task closes. No other source edits.
