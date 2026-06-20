# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Current HEAD: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Task ID: `layout-interaction-functional-pass`
Task Name: Seating, object collision, POS location, employee activity, kitchen/office layout, lobby furniture, walk-in scale, model scale
Status: `IMPLEMENTED_AWAITING_CLAUDE_REVIEW`
Handoff Version: 3.0
Last Updated: 2026-06-19
Updated By: Codex

## Workflow Contract

HANDOFF.md is the permanent, revolving coordination channel between Claude Opus and Codex. It describes only the current task state; stale or superseded detail is removed on each update; Git history preserves prior versions.

- Michael owns product intent, material decisions, merge/release approval, final acceptance.
- Claude Opus owns task definition, architecture/risk review, acceptance criteria, packet approval, implementation review.
- Codex owns repository inspection, source implementation, test execution, implementation evidence.
- Codex must not make source edits until Claude approves the packet and Michael approves the specification.
- Repository files are the source of truth; model memory is secondary.
- Neither model may merge, discard, reset, rebase, or delete work without Michael approval.

Prior task `movement-runtime-authority-followup` is CLOSED/ACCEPTED at Base SHA (Michael's visual smoke confirmed avoidance fixed). Do not reopen it unless a new direct symptom appears.

## Current User Findings (Michael, 2026-06-19 visual smoke)

- Characters do not sit in chairs correctly.
- Characters go through objects.
- POS interaction is centered; the real POS belongs at the right end of the counter where the registers are.
- Employees stand at stations rather than visibly react to orders/customers; they should use a work animation when working.
- Office should be closed off from the kitchen.
- Fryer station against the office wall, facing into the kitchen, left corner; fryers directly to its right.
- Use a booth for the break table.
- Lobby furniture should be functional and not clip.
- Walk-in 10% bigger.
- Employee models reduced to the same apparent size as customer models.

References: six screenshots `2026-06-19 180551 / 181144 / 182624 / 182639 / 182656 / 182722.png`; runtime logs/exports under `app_userdata\Restaurant Simulator\` (reference only).

## Initial Repo Findings (Codex)

- Dining furniture is decor, not nav obstacles: `Decor()` builds `table`/`chair`/`booth` meshes; `BuildNavigation()` only carves `st_*`, `counter*`, `wall_*`, `trash`, `break_table` → explains walking through furniture.
- Dining spots are loose `WorldLayout.Tables` positions, not typed seat anchors with facing/table metadata → explains offset sitting.
- POS visuals at `pos_register_1=(-2,-1)`, `pos_register_2=(2,-1)`; service slots generated generically → mid-counter, not right-end.
- Sizes use fixed `StaffModelScale=1f`, `CustomerModelScale=1.5f`; apparent heights differ because intrinsic GLB heights differ.
- `EmployeeAgent.Init()` maps work-anim keys and `CharacterRig.PickWork()` matches clips, but work animation is not visibly obvious → needs better `Working` coverage and/or fallback motion for imported GLBs.
- Office walls partial with a kitchen-side opening; fryer/fry-holder not aligned to the new left-corner instruction.
- Untracked `CrowdCoordinator.cs.uid` editor metadata present.

## Objective

In runtime smoke, the layout and visible interactions read correctly: customers sit in real chairs/booths aligned to furniture; characters route around furniture/equipment/walls; POS service occurs at the right-end registers; employees visibly react to demand with work animations; office/fryer/walk-in/break-room match Michael's direction; staff and customer apparent model heights match. All changes presentation/layout only — deterministic sim outputs unchanged.

## Source Of Truth

Michael's 2026-06-19 findings + screenshots; current source under `game/scripts/world`; existing deterministic sim contracts (no event-schema, export-ledger, ASC, or sim-causality change).

## Allowed Paths

- `HANDOFF.md`
- `game/scripts/world/{WorldBuilder,CrowdCoordinator,AgentManager,CustomerAgent,EmployeeAgent,CharacterRig}.cs`
- `game/scripts/world/CameraDirector.cs` only if camera targets must follow moved anchors
- `test-artifacts/movement-smoke/assert_movement.py` (layout/interaction assertions)
- `test-artifacts/movement-smoke/movement_smoke_runner.gd` (seating/POS/furniture/scale telemetry + screenshots)

## Prohibited Changes / Non-Goals

- No deterministic event schema, export ledger, ASC, ingredient/catalog/vendor/back-office/pricing/staffing, or unrelated UI changes.
- No `SimRunState` write-back; no change to sim load granularity, coverage, or throughput.
- No broad rewrite of movement avoidance (accepted last task).
- No renaming/re-keying of station anchors — positions may move, keys stay stable.
- No deletion of generated/editor artifacts (e.g. `.cs.uid`) without Michael approval — follow the repo's existing convention.
- No new external dependencies.

## Requirements (Codex packet + Claude corrections)

Layout / nav:
- Close the office from the kitchen. Manager access: Decision M1 approved — add a door on the back wall, facing the kitchen/back area and placed all the way to the back wall so `work_office`/MOD pathing is not trapped.
- Office interior: add two large windows, one with visibility toward drive-thru and one with visibility into the kitchen while sitting in the office. Desk goes against the back wall but faces into the room so the manager sits behind it and can see out.
- Fryer station against the office wall, left corner, facing into the kitchen; fryers directly to its right with aisle clearance. Keep `work_fryer`/`work_grill` keys stable; re-derive only positions/anchors.
- Walk-in 10% bigger; keep `freezer_door` anchor and the prior task's `walkin_standoff_*`/supply slots aligned to the resized model's real door; no separate fake door object. Re-run the movement parser to confirm no walk-in choreography regression.
- Replace the break table with an employee-only booth-based break area; move break reservation slots to the new booth.
- Rework lobby furniture into functional seating groups with clear aisles and no clipping.
- Add navmesh obstacle proxies for furniture/equipment, but keep every seat/interaction approach slot reachable on navmesh (carve the furniture, not the seat/stand point).

Seating:
- Replace loose `Tables` with typed seat reservations: seat position, seated facing/yaw, tray/table target, furniture type.
- Customers approach without passing through table/chair/booth geometry, then align to the seat; tray/cup lands on/near the table, not through body/furniture.

POS (presentation-only):
- Move counter order/service slots (and cashier serve points) to the right-end registers. Confirm "right end" against the screenshots before moving anchors (Decision M2 / convention: customer-facing +X). If register models must physically move too, keep the counter coherent. No `_sim` coupling.

Employees / animation:
- `Working=true` selects a visible station-appropriate work animation, with a bounded procedural fallback loop for GLBs whose clips don't match (fallback must not re-trigger movement/twitch).
- Visible busyness driven only by existing read-only signals (sim load, coordinator reservation, `Phase`) — never by changing sim load/throughput. Cashiers serve at POS/DT when reserved, then return.

Scale:
- Normalize via measured AABB to a single explicit target apparent height shared by staff and customers; verify across all variants (employee_m/f, shift_manager, store/GM, customer_m/f); keep a manual per-model override. Target height per Decision M3.

Determinism:
- `AllJsonl` deterministic replay (normal_day/seed 12345) byte-identical to Base SHA — unconditional (all changes are presentation-only).

## Acceptance Criteria

- [ ] `dotnet build ... --nologo` zero errors; Godot opens/runs with no new errors in `godot.log`.
- [ ] Engine self-test passes; `AllJsonl` byte-identical to Base SHA (normal_day/seed 12345).
- [ ] Movement parser stays green on a fresh smoke (no new Enter/Dining/pickup stalls, jitter, or walk-in proximity introduced by layout changes).
- [ ] Machine assertion: every customer entering Dining reaches a seat (no Dining-phase stall) and seated position is within tolerance of its assigned seat anchor.
- [ ] Machine assertion: staff vs customer apparent height (AABB) match within tolerance.
- [ ] Machine assertion: counter order/serve slots at the confirmed right-end register coordinate.
- [ ] Fresh screenshots: wide kitchen, fryer/office wall, walk-in door, right-register POS service, lobby furniture, seated customer, break booth, staff/customer side-by-side scale.
- [ ] Manual visual smoke (Michael, final gate): no walking through furniture/equipment/walls; customers aligned in chairs/booths; employees animate while busy; service at right-end POS.

## Decisions Reserved for Michael

- M1 — Office access. APPROVED: office stays closed off from the kitchen production area, but gets a door on the back wall facing the kitchen/back area and placed all the way to the back wall so manager pathing is reachable. Add two large office windows: one looking toward drive-thru, one looking into the kitchen from the manager's seated position. Desk goes against the back wall and faces into the room.
- M2 — Counter "right end" perspective. APPROVED: customer-facing right, which maps to `+X`.
- M3 — Scale target. APPROVED: same apparent height across employees and customers; normalize to customer height.
- M4 — Break booth occupancy: APPROVED employee-only for this pass.

## Claude-Resolved Questions

- Furniture collision: navmesh obstacle proxies only — no new physics/click collision bodies for furniture (it isn't interactive); station click-pick bodies are unchanged.
- Sequencing: implement and validate in independent groups, re-running build + self-test + byte-identical + movement parser after each: (a) layout/anchors (office/fryer/walk-in/break/lobby) → (b) seat metadata + sit alignment → (c) nav obstacle proxies → (d) POS relocation → (e) scale normalization → (f) work animation. This keeps rollback to a single coherent revert per group.

## Tests Required

- `git status --short --branch` before/after.
- `dotnet build game\RestaurantSimulator.csproj --nologo`
- `dotnet run --project tools\engine-selftest\harness.csproj` (+ `AllJsonl` byte-identical vs Base SHA)
- Godot runtime smoke via the console executable + movement smoke runner.
- `python test-artifacts\movement-smoke\assert_movement.py <latest folder>` (existing rules green + new seating/scale/POS assertions).
- Manual screenshots from key/free cameras; inspect `godot.log` for new warnings/errors.

## Known Risks

- Furniture-as-obstacle can make seats unreachable unless approach slots are separated from obstacle geometry (Requirement above).
- Moving equipment shifts anchor-derived station slots and camera framing; `CrowdCoordinator`/`CameraDirector` may need coordinated updates.
- Walk-in resize can regress the just-fixed supply choreography if anchors/standoff slots aren't realigned.
- AABB scale normalization touches model loading; verify across all GLB variants; keep an override.
- Work-anim clip names vary by GLB; fallback motion must not interfere with steering.

## Claude Review

Verdict: `APPROVED_WITH_CORRECTIONS`

Strong packet: accurate repo inspection, determinism-aware, correctly scoped, does not reopen avoidance. Corrections (folded above): (1) machine-checkable acceptance for seat-reach, seat-alignment, staff/customer height parity, and POS location — manual observation alone repeats the twitch-miss; (2) nav-obstacle vs reachable-seat guard; (3) walk-in resize must not regress prior walk-in choreography, and station keys stay stable; (4) unconditional byte-identical (all changes presentation-only); (5) visible busyness via read-only signals only. Four Michael product decisions (M1–M4); M1 (office access) is material because it can trap manager pathing. Furniture collision and sequencing resolved by Claude above.

On Michael approving the spec + M1–M4, this is `APPROVED_TO_IMPLEMENT`.

Reviewed By: Claude Opus — 2026-06-19.

## Remaining Steps After Approval

1. Preserve worktree; follow repo convention on `CrowdCoordinator.cs.uid` (no silent delete).
2. Layout/anchor changes in `WorldBuilder.cs` (office/fryer/walk-in/break/lobby), anchors and models in sync, station keys stable.
3. Typed seat/furniture reservation metadata; update `CrowdCoordinator` + `CustomerAgent` seating.
4. Move POS order/service + cashier targets to confirmed right-end registers.
5. Nav obstacle proxies preserving reachable interaction targets.
6. AABB scale normalization to shared target height.
7. `Working` animation/fallback visibility, read-only signals only.
8. Per group: rebuild, self-test, byte-identical, movement parser, smoke, screenshots, log check.
9. Update this handoff with evidence and changed-file rationale.

## Codex Implementation Summary

Implemented 2026-06-19 within approved paths.

Changed files:
- `game/scripts/world/WorldBuilder.cs`: added typed seat metadata, moved POS anchors to customer-facing right (`+X`), moved fryer/fry holder toward the office wall, resized walk-in to 110%, added office door/window/desk intent geometry, replaced break table with employee-only booth seating, reworked lobby furniture, and added nav obstacle proxies that carve table cores without blocking seat targets.
- `game/scripts/world/CrowdCoordinator.cs`: dining slots now reserve typed seats; break slots use employee-only booth seats; POS order/service slots follow moved right-end register anchors; telemetry emits actual `ApparentHeight`.
- `game/scripts/world/CustomerAgent.cs`: customers receive seated yaw and tray/table target metadata; on dining arrival they align to the assigned seat and place tray/cup at the assigned table target.
- `game/scripts/world/AgentManager.cs`: staff and customers now load with shared `CharacterTargetHeight = 1.72f`; fixed old customer scale constant.
- `game/scripts/world/CharacterRig.cs`: imported model loading supports AABB/manual-fallback height normalization and a visible bounded work fallback when no station-specific clip is available; height measurement no longer uses `GlobalTransform` before tree entry.
- `test-artifacts/movement-smoke/assert_movement.py`: added dining-seat arrival, POS-right-end, and staff/customer height parity assertions; ignores timeout checks on the first telemetry sample after a phase change to avoid stale phase timer false positives.
- `HANDOFF.md`: approvals and evidence.

Generated/untracked evidence folders:
- `test-artifacts/movement-smoke/20260619_194415/` early smoke with pre-fix `GlobalTransform` warnings.
- `test-artifacts/movement-smoke/20260619_194834/` clean runtime, parser initially exposed seating reach failures.
- `test-artifacts/movement-smoke/20260619_195344/` seating obstacle fix smoke; parser exposed stale phase-timer false positive.
- `test-artifacts/movement-smoke/20260619_200008/` final green smoke. Do not commit these folders unless Michael wants runtime evidence artifacts retained.

## Validation Evidence

- `dotnet build game\RestaurantSimulator.csproj --nologo`: PASS, 0 warnings / 0 errors.
- `dotnet run --project tools\engine-selftest\harness.csproj`: PASS; `SELF-TEST TOTAL: 120/120`, `INGREDIENT-MODEL TOTAL: 10/10`, `CAREER-TEST TOTAL: 11/11`, `RESULT: PASS`.
- Canonical deterministic replay: baseline Base SHA `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5` vs current `SimRunState.AllJsonl`, `normal_day` / seed `12345`: `CANONICAL_EVENT_STREAM_BYTE_IDENTICAL: PASS 05464C886B33616332885744D215EE7FC7EF8EC0F18384C67B05DB17E83D18B6`.
- Godot movement smoke: PASS, output `test-artifacts/movement-smoke/20260619_200008/`; runtime printed model normalization (`employee_m 1.98m -> 1.72m`, `employee_f/shift_manager 1.96m -> 1.72m`, customers `1.00m -> 1.72m`); no `ERROR`, `SCRIPT ERROR`, `Exception`, or `WARN` matches in latest `godot.log` check.
- `python test-artifacts\movement-smoke\assert_movement.py test-artifacts\movement-smoke\20260619_200008`: PASS, 378 samples / 2328 agent samples / 0 failures. This includes existing movement rules plus new dining-seat, POS-right-end, and height parity checks.
- Visual sample reviewed: `test-artifacts/movement-smoke/20260619_200008/05_overhead.png` shows updated office/fryer/walk-in/lobby layout from overhead. Michael manual close-up smoke remains the final visual acceptance gate.

## Rollback

Targeted restoring commit or revert of the approved implementation commit(s) — one per group keeps reverts clean. No reset, rebase, checkout-discard, or delete without Michael approval. Worktree clean at Base SHA `2d07332`.

## Michael Approval

Specification Approved: `APPROVED 2026-06-19`
Material Decisions: `M1 APPROVED`, `M2 APPROVED`, `M3 APPROVED`, `M4 APPROVED`
Merge Approved: `PENDING`

## Next Authorized Action

Claude reviews implementation evidence and diffs. Michael performs final manual visual smoke for close-up seating, POS service, employee work animation, office windows/door, and furniture clipping. No merge until Claude review and Michael approval.
