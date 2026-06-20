# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Current HEAD: `dd7bcbaaf72defdcb426598361851ca55cf3a2c8`
Task ID: `layout-interaction-functional-pass`
Task Name: Seating, object collision, POS location, employee activity, kitchen/office layout, lobby furniture, walk-in scale, model scale
Status: `AWAITING_CLAUDE_REVIEW_OF_OV1_AND_CUSTOMER_STALL`
Handoff Version: 5.1
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
- [x] Manager office-access (M1 door) reachability — targeted probe PASS on `20260619_205213`: `emp_28` departed at frame 216, max distance 2.002m, returned at frame 399, status `pass`.
- [~] Screenshots / Michael visual smoke (BINDING): office windows/desk sightlines, close-up seating alignment, right-end POS service, employee work animation, furniture clipping.
- [!] Current movement parser on `20260619_205213` FAILS for a separate customer stall: `cust_ord_000002 stuck 4.03s at 3.90m from target` while entering `mobile_entry_0`. This is outside the OV1 office-door check and needs Claude/Michael direction before gameplay source changes.

## Open Verification (last gate before merge)

OV1 — Manager office-door reachability (M1's functional purpose). CLOSED by Codex targeted check in `test-artifacts/movement-smoke/movement_smoke_runner.gd` plus parser support in `assert_movement.py`. Fresh Godot headless smoke `20260619_205213` wrote `manager_office_roundtrip.json` with status `pass` (`emp_28`, depart frame 216, return frame 399). Status: CLOSED, pending Claude review of the diff.

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

Remaining before merge: OV2 (Michael's binding visual smoke) plus review/decision on the newly exposed mobile-entry customer stall from the hardened movement parser. OV1 now has machine evidence.

Reviewed By: Claude Opus — 2026-06-19.

## Codex Implementation Summary

Changed files (all allowed paths): `WorldBuilder.cs` (typed seat metadata; POS anchors to `+X`; fryer/holder to office wall; walk-in 110%; office door/window/desk geometry; employee-only break booth; reworked lobby furniture; furniture obstacle proxies that carve cores without blocking seats; Codex v5.1 also changed `lobby_menu.png` loading from import-cache `ResourceLoader` to direct `ImageTexture` to remove a headless runtime load error). `CrowdCoordinator.cs` (typed dining seats; employee-only break booth slots; POS slots follow right-end anchors; emits `ApparentHeight`). `CustomerAgent.cs` (seated yaw + tray/table target; align + place tray). `AgentManager.cs` (shared `CharacterTargetHeight=1.72f`; fixed old customer scale). `CharacterRig.cs` (AABB/manual-fallback height normalization; bounded work fallback; no `GlobalTransform` before tree entry). `assert_movement.py` (dining-seat, POS-right-end, height-parity assertions; one-sample post-phase-change exemption; Codex v5.1 adds manager-office round-trip assertion/probe support). `movement_smoke_runner.gd` (Codex v5.1 adds targeted OV1 office round-trip probe and skips screenshot capture under headless dummy renderer). `HANDOFF.md`.

Untracked evidence folders `20260619_194415/194834/195344/200008/204625/205038/205213` — do not commit unless Michael wants artifacts retained. `204625` and `205038` are failed intermediate OV1 probe attempts; `205213` is the useful current evidence folder. `CrowdCoordinator.cs.uid` left per repo convention.

## Validation Evidence

- Build PASS 0/0. Self-test PASS 120/120, 10/10, 11/11.
- Byte-identical: Base SHA `2d07332` vs current `AllJsonl` (normal_day/seed 12345): `CANONICAL_EVENT_STREAM_BYTE_IDENTICAL: PASS 05464C886B33616332885744D215EE7FC7EF8EC0F18384C67B05DB17E83D18B6`.
- Godot smoke `20260619_200008`: normalization printed (employee 1.98/1.96→1.72, customers 1.00→1.72); no errors/warnings in `godot.log`.
- Parser on `20260619_200008`: PASS 378 / 2328 / 0 failures (existing + dining-seat + POS-right-end + height-parity).
- `05_overhead.png` reviewed (office/fryer/walk-in/lobby overhead). Manager office-door round-trip not exercised — OV1.
- Codex v5.1 build: `dotnet build game\RestaurantSimulator.csproj --nologo` PASS 0/0.
- Codex v5.1 self-test: `dotnet run --project tools\engine-selftest\harness.csproj` PASS 120/120, ingredient 10/10, career 11/11.
- Codex v5.1 Godot headless movement smoke: `20260619_205213` completed without the prior `lobby_menu.png` load error; screenshots skipped intentionally because headless display mode has no viewport image.
- Codex v5.1 OV1 probe: `test-artifacts/movement-smoke/20260619_205213/manager_office_roundtrip.json` PASS (`emp_28`, depart frame 216, return frame 399).
- Codex v5.1 parser on `20260619_205213`: FAIL 72 / 384 / 1 failure: `cust_ord_000002 stuck 4.03s at 3.90m from target`. Office probe PASS is recorded in the parser summary.

## Tests Required

`dotnet build … --nologo`; `dotnet run --project tools\engine-selftest\harness.csproj` (+ `AllJsonl` byte-identical); Godot smoke via console exe + runner; `python … assert_movement.py <folder>`; manual screenshots; inspect `godot.log`. Claude review needed for v5.1 OV1 probe diff and the new mobile-entry customer stall.

## Known Risks (residual)

- New movement-parser finding: `cust_ord_000002` stalls entering `mobile_entry_0` in `20260619_205213`. This may indicate a remaining entrance/mobile pathing issue, but it was discovered while closing OV1 and is not yet approved for source changes.
- Office window sightlines / desk facing and close-up seating/clipping are visual-gate only.

## Rollback

Targeted restoring commit or revert of implementation commit `b4f8b98da28d61ca887ea2325847c494ef7b5792`. No reset/rebase/discard/delete without Michael approval.

## Michael Approval

Specification Approved: `APPROVED 2026-06-19`
Material Decisions: `M1–M4 APPROVED`; height deviation `RATIFIED 1.72m`
Human Visual Smoke: `PENDING` (binding — OV2)
Merge Approved: `PENDING`

## Next Authorized Action

Claude review v5.1 diff/evidence. If approved, decide whether the `cust_ord_000002` mobile-entry stall becomes the next implementation packet or is treated as a non-blocking transient. Michael still owns OV2 visual smoke and merge approval. No further gameplay source edits until Claude/Michael approve the next packet.
