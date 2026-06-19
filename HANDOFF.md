# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Current HEAD: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Task ID: `layout-interaction-functional-pass`
Task Name: Fix seating, object collision, POS location, employee activity, kitchen/office layout, lobby furniture, walk-in scale, and character model scale
Status: `AWAITING_CLAUDE_PACKET_REVIEW`
Handoff Version: 1.0
Last Updated: 2026-06-19
Updated By: Codex

## Workflow Contract

HANDOFF.md is the permanent coordination channel between Claude Opus and Codex. It describes only the current task state; stale or superseded task detail is removed on each update.

- Michael owns product intent, material decisions, merge/release approval, final acceptance.
- Claude Opus owns task definition, architecture/risk review, acceptance criteria, packet approval, and implementation review.
- Codex owns repository inspection, source implementation, test execution, and implementation evidence after approval.
- Codex must not make source edits until Claude approves this packet and Michael approves the specification.
- Repository files are the source of truth; model memory is secondary.
- Neither model may merge, discard, reset, rebase, or delete work without Michael approval.

## Current User Findings

Michael's human visual smoke says character avoidance now appears fixed. Do not reopen the just-completed movement-authority task unless a new direct symptom appears.

New issues to fix:
- Characters do not sit in chairs correctly.
- Characters go through objects.
- POS interaction is still effectively centered; the real POS belongs at the right end of the counter where the registers are.
- Employees appear to stand at stations rather than react visibly to orders/customers.
- Employees should use work animation when working.
- Office should be closed off from the kitchen.
- Fryer station should go against the office wall, facing into the kitchen, on the left corner; fryers go directly to its right.
- Use a booth for the break table.
- Lobby furniture should be functional and not clip.
- Walk-in should be 10% bigger.
- Employee models should be reduced to the same apparent size as customer models.

Visual references provided by Michael:
- `C:\Users\micha\OneDrive\Pictures\Screenshots\Screenshot 2026-06-19 180551.png`
- `C:\Users\micha\OneDrive\Pictures\Screenshots\Screenshot 2026-06-19 181144.png`
- `C:\Users\micha\OneDrive\Pictures\Screenshots\Screenshot 2026-06-19 182624.png`
- `C:\Users\micha\OneDrive\Pictures\Screenshots\Screenshot 2026-06-19 182639.png`
- `C:\Users\micha\OneDrive\Pictures\Screenshots\Screenshot 2026-06-19 182656.png`
- `C:\Users\micha\OneDrive\Pictures\Screenshots\Screenshot 2026-06-19 182722.png`

Latest user-provided runtime exports/logs for reference only:
- `C:\Users\micha\AppData\Roaming\Godot\app_userdata\Restaurant Simulator\logs\godot.log`
- `C:\Users\micha\AppData\Roaming\Godot\app_userdata\Restaurant Simulator\outputs\sim_normal_day_132195522\*.json`
- `C:\Users\micha\AppData\Roaming\Godot\app_userdata\Restaurant Simulator\outputs\sim_normal_day_132195522\event_stream.jsonl`

## Initial Repo Findings

Relevant current source:
- `game/scripts/world/WorldBuilder.cs` owns procedural layout, anchors, stations, furniture, support room, office, walk-in, navmesh source geometry, and model loading.
- `game/scripts/world/CrowdCoordinator.cs` owns POS, kiosk, dining, break, walk-in, and employee reservation slots.
- `game/scripts/world/AgentManager.cs` owns staff/customer model scale, employee station assignment, cashier setup, station busyness, and employee drive loop.
- `game/scripts/world/CustomerAgent.cs` owns dine-in state transition, seat request, tray/cup placement, pickup/leave behavior.
- `game/scripts/world/EmployeeAgent.cs` owns station/supply/break/patrol behavior and `Working` state.
- `game/scripts/world/CharacterRig.cs` owns imported-model animation selection, seat animation state, movement, and local overlap handling.

Specific observations:
- Dining furniture is deliberately treated as visual decor, not navmesh obstacles: `Decor()` creates `table`, `chair`, and `booth` mesh names, and `BuildNavigation()` only carves `st_*`, `counter*`, `wall_*`, `trash`, and `break_table`. This explains "characters going through objects" around lobby furniture.
- Dining spots are stored as loose `WorldLayout.Tables` positions, not typed seat anchors with facing/yaw/table metadata. This explains customers sitting offset from chairs/booths.
- POS visuals are at `pos_register_1=(-2, -1)` and `pos_register_2=(2, -1)`, and coordinator service slots are generated generically. Michael now wants POS interaction at the right end of the counter where the registers are, not mid-counter.
- Staff and customer sizes use fixed constants: `StaffModelScale = 1f`, `CustomerModelScale = 1.5f`. Apparent model heights can differ despite these constants; a robust fix should normalize imported model height using measured AABB or an explicit target height rather than only guessing a scale.
- `EmployeeAgent.Init()` maps station-specific work animation keys, and `CharacterRig.PickWork()` already attempts clip matching, but the visual report says work animation is not obvious. This likely needs better `Working` state coverage and/or fallback procedural work motion for imported models whose clips do not match station keywords.
- `StationBusy(e)` is currently data-driven from sim loads plus POS service. This may be correct for deterministic sim causality but insufficient visually if load counters update too coarsely or staff are assigned to low-load stations.
- Current office walls are partial and include a kitchen-side opening. Michael wants the office closed off from the kitchen.
- Current fryer/fry holder placement is not yet aligned with the new office-wall-left-corner instruction.
- A new untracked editor artifact exists: `game/scripts/world/CrowdCoordinator.cs.uid`. Treat as generated Godot/editor metadata and do not commit unless intentionally accepted.

## Proposed Objective

Make the restaurant layout and visible character interactions read correctly in runtime smoke: customers sit in real chairs/booths, characters route around furniture/equipment/walls instead of through them, POS service happens at the right-end registers, employees visibly react to work/customer demand and use work animations, the office/fryer/walk-in/break-room layout matches Michael's direction, and staff/customer model sizes are normalized.

## Proposed Source Of Truth

- Michael's 2026-06-19 visual smoke findings and screenshots listed above.
- Current repo source under `game/scripts/world`.
- Existing deterministic sim contracts: runtime presentation changes must not alter event schemas, export ledgers, ASC contracts, or core sim causality unless separately approved.

## Proposed Allowed Paths

- `HANDOFF.md`
- `game/scripts/world/WorldBuilder.cs`
- `game/scripts/world/CrowdCoordinator.cs`
- `game/scripts/world/AgentManager.cs`
- `game/scripts/world/CustomerAgent.cs`
- `game/scripts/world/EmployeeAgent.cs`
- `game/scripts/world/CharacterRig.cs`
- `game/scripts/world/CameraDirector.cs` only if camera targets need to follow moved anchors
- `test-artifacts/movement-smoke/assert_movement.py` only for adding layout/interaction assertions
- `test-artifacts/movement-smoke/movement_smoke_runner.gd` only for screenshots/telemetry coverage of seating/POS/furniture

## Proposed Prohibited Paths / Non-Goals

- No deterministic event schema changes.
- No export ledger/schema changes.
- No ingredient, catalog, vendor, back-office, pricing, or staffing model changes.
- No ASC compatibility changes.
- No unrelated UI/dashboard work.
- No broad rewrite of the movement avoidance system; Michael says avoidance appears fixed.
- No deletion of generated/editor artifacts unless Michael explicitly approves cleanup.
- No new external dependencies.

## Proposed Requirements

Layout / nav:
- Close the office off from the kitchen. If office access remains needed, route it through an intentional door/opening that is not the kitchen production aisle, or document that the office is visually closed and manager access is deferred.
- Move fryer station against the office wall, left corner, facing into the kitchen; place fryers directly to its right with enough aisle clearance.
- Make the walk-in 10% bigger from the current runtime appearance, verify anchor and door still align with the actual walk-in model door, and do not create a separate fake door object.
- Replace the break table with a booth-based break area.
- Rework lobby furniture into functional seating groups with clear walking aisles and no chair/booth/table clipping.
- Add navmesh obstacle proxies for furniture and equipment while preserving reachable seat/customer interaction slots. Do not make seat targets unreachable by carving the exact point customers must stand/sit on.

Seating:
- Replace loose `Tables` positions with explicit seat reservations or equivalent metadata: seat position, seated facing direction, table/tray target, and furniture type.
- Customers should approach the seat without walking through table/chair/booth geometry, then align visually to the chair/booth while seated.
- Tray/cup placement should land on/near the table surface, not float through the body or furniture.

POS:
- Move counter POS order/service slots to the right end of the counter where the register models are.
- Counter customers should queue to the correct right-end order points, and cashiers should approach matching service-side points.
- Keep POS choreography presentation-only unless Claude/Michael explicitly approve deterministic sim changes.

Employees / animation:
- Ensure station employees visibly do useful work when their station has active load or a customer/service need.
- Ensure `Working = true` selects a visible station-appropriate work animation or a reliable fallback motion on imported staff models.
- Cashiers should visibly serve at POS/DT when customers/cars require it and return to assigned work afterward.
- Avoid changing core sim throughput unless explicitly approved; this pass should primarily improve presentation and station choreography.

Scale:
- Normalize employee model apparent height to customer model apparent height. Prefer measuring imported model AABB and applying a target-height scale over fixed constants if feasible.
- Verify managers and crew are all scaled consistently unless intentionally differentiated.

Collision / object pass:
- Identify which objects are physical/nav obstacles versus interactable targets.
- Characters should not path through furniture, equipment, counters, office/walls, walk-in body, fryers, or lobby soda/kiosk objects.
- Interactable positions should sit just outside obstacle extents with enough clearance for animation and personal space.

## Proposed Acceptance Criteria

- `dotnet build game\RestaurantSimulator.csproj --nologo` passes with zero errors.
- `dotnet run --project tools\engine-selftest\harness.csproj` passes; deterministic event outputs remain byte-identical if presentation-only changes are claimed.
- Godot project opens/runs without errors in `godot.log`.
- Fresh visual smoke screenshots include: wide kitchen, fryer/office wall, walk-in door, POS/right-register service, lobby furniture, seated customer, break booth, staff/customer side-by-side scale.
- Movement smoke parser remains green for avoidance/stall rules from the prior task.
- Manual runtime observation confirms: no characters walking through furniture/equipment/walls; customers sit aligned with chairs/booths; employees show work animation while busy; counter service occurs at right-end POS.
- If any change affects deterministic sim behavior, document the reason and require Claude + Michael approval before implementation.

## Proposed Tests Required

- `git status --short --branch` before and after.
- `dotnet build game\RestaurantSimulator.csproj --nologo`
- `dotnet run --project tools\engine-selftest\harness.csproj`
- Godot runtime smoke via existing console executable and movement smoke runner.
- `python test-artifacts\movement-smoke\assert_movement.py <latest smoke folder>`
- Manual screenshot capture from key cameras/free camera after implementation.
- Inspect `C:\Users\micha\AppData\Roaming\Godot\app_userdata\Restaurant Simulator\logs\godot.log` for new warnings/errors after runtime.

## Known Risks

- Making all dining furniture nav obstacles can make seat targets unreachable unless seat approach slots are separated from furniture obstacle geometry.
- Moving kitchen equipment changes anchor-derived employee station slots and camera framing; `CrowdCoordinator` and `CameraDirector` may need coordinated updates.
- Closing the office can trap manager pathing if `work_office` remains assigned but no reachable route exists.
- Staff scale normalization by fixed constant may fail across manager/crew GLBs; AABB-based normalization is safer but touches model-loading behavior.
- Work animation names may differ by imported GLB; station keyword matching may need fallback work motions rather than assuming clips exist.
- Presentation changes should not mutate `SimRunState`; any throughput/coverage change has determinism and realism implications.

## Open Questions For Claude / Michael

- Should the office be completely inaccessible visually, or should it have a non-kitchen door for managers?
- Is the "right end" of the counter from the lobby/customer perspective or kitchen/employee perspective? Current coordinates likely need confirmation against the screenshots before moving POS anchors.
- Should furniture become hard collision only through navmesh, or should visible physics/click collision bodies also be added?
- Should break-booth seating be usable by employees only, or can customers also reserve it if the lobby is full?
- Should employee model normalization target customer height exactly, or retain slight role variation while keeping employees from looking oversized?

## Proposed Remaining Steps After Approval

1. Preserve current worktree state and ignore the untracked `CrowdCoordinator.cs.uid` unless cleanup is approved.
2. Implement layout/anchor changes in `WorldBuilder.cs`, keeping anchors and visual models in sync.
3. Add seat/furniture reservation metadata and update `CrowdCoordinator` + `CustomerAgent` seating behavior.
4. Move POS order/service slots and cashier targets to the right-end registers.
5. Add/adjust nav obstacle proxies for furniture/equipment while preserving reachable interaction targets.
6. Normalize staff/customer model scale.
7. Improve employee `Working` animation/fallback visibility without changing sim causality.
8. Rebuild, run self-test, run Godot smoke, inspect logs, and capture screenshots.
9. Update this handoff with implementation evidence and changed-file rationale.

## Rollback Plan

Use a targeted restoring commit or revert commit for the approved implementation commit. Do not use reset, rebase, checkout discard, or delete operations without Michael approval.

## Next Authorized Action

Claude reviews this proposed packet. If Claude returns `APPROVED_TO_IMPLEMENT` and Michael approves, Codex may implement within the allowed paths. Until then, Codex should make no gameplay/layout source edits.
