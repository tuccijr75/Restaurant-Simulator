# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Current HEAD: `9128fd706a7022ebd52a59dd72208eef0a4434dc`
Task ID: `movement-runtime-authority-followup`
Task Name: Fix remaining visual movement stalls, POS service choreography, and deprecated movement interference
Status: `AWAITING_CLAUDE_REVIEW`
Handoff Version: 2.0
Last Updated: 2026-06-19
Updated By: Codex

## Workflow Contract

This file is now the permanent coordination channel between Claude Opus and Codex for this project.

- Michael owns product intent, material decisions, merge/release approval, and final acceptance.
- Claude Opus owns task definition, architecture review, risk review, acceptance criteria, and approval of implementation packets.
- Codex owns repository inspection, source implementation, test execution, and implementation evidence.
- Codex must not make code edits until Claude has reviewed and approved the implementation packet in this file.
- Claude and Codex may both read and update this file.
- Repository files are the source of truth. Model memory is secondary.
- Neither model may merge, discard, reset, rebase, or delete work without Michael approval.

## Current Context

The first movement authority pass added a `CrowdCoordinator`, reservation slots, movement telemetry, and an automated movement smoke parser. The latest validated smoke run passed the parser, but Michael observed runtime behavior while the smoke test was open that the parser and screenshots did not catch:

- Customer and employee groups can still twitch in place.
- The first customer in line appeared not to move.
- Two employees twitched/stuck in the right corner near the walk-in.
- The employee standing in front of the counter appeared idle/unhelpful.
- Front counter behavior should be explicit: customer walks to a POS, a counter employee approaches the matching POS and takes the order, then returns to assigned work.
- After ordering, a customer should get their drink from the lobby soda machine if applicable, then wait for food.
- Deprecated movement/path/collision data may still be interfering.

## Investigation Findings

Telemetry from `test-artifacts/movement-smoke/20260619_132902/movement_samples.jsonl` confirms important issues not covered by the current parser:

- Several mobile customers remained in `Enter` for 54-162 seconds.
- Some of those mobile customers had completed tickets but did not advance because `CustomerAgent.Enter` still required exact `StepToward()` arrival at a shared target.
- Example: `cust_ord_000002` was in `Enter` for 162.5 seconds, ticket complete, 0.193m from target, but did not transition because the exact arrival threshold is about 0.12m.
- The parser did not fail long `Enter` phases, so the smoke result was falsely green for this class of runtime failure.
- `emp_9` and `emp_28` were near each other at the walk-in for 217 telemetry samples while both were on walk-in supply runs.
- Employee telemetry currently reports station/home target instead of the active supply-run/break/serve target, which hides the real walk-in target conflict.
- Front counter service is too generic: `StationBusy("work_counter")` uses `_sim.Tickets > 0`, so employees respond to any open ticket instead of an actual lobby customer currently ordering at a POS.
- The old manager-level `Separate()` and old `ResolveOccupiedTarget()` appear removed. Remaining local movement rules are `AvoidedHeading`, `ResolveCharacterOverlap`, `HoldWithPersonalSpace`, and stuck lateral/reverse recovery. These may still fight coordinator reservations when characters hold close to a target.

## Objective

Create a robust, testable movement and service-choreography system where:

- No customer remains indefinitely in `Enter`, `Ordering`, `Waiting`, `ToPickup`, `Dining`, `Busing`, or `Leave`.
- Mobile/delivery customers use distinct entry/wait/pickup reservations from the first frame.
- Walk-in supply runs do not send multiple employees into the same walk-in doorway at the same time.
- Front counter customers reserve a specific POS.
- The matching counter employee walks to the service side of that POS, serves only that ordering customer, then returns to their assigned task.
- Customers who order drinks use a lobby soda-machine stop when appropriate before waiting for food.
- Automated telemetry detects the runtime issues Michael saw, not just screenshot-visible overlap.

## Proposed Implementation Packet

task_id: `movement-runtime-authority-followup`

repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`

branch: `main`

starting_sha: `9128fd706a7022ebd52a59dd72208eef0a4434dc`

allowed_paths:

- `HANDOFF.md`
- `game/scripts/world/CrowdCoordinator.cs`
- `game/scripts/world/AgentManager.cs`
- `game/scripts/world/CustomerAgent.cs`
- `game/scripts/world/EmployeeAgent.cs`
- `game/scripts/world/CharacterRig.cs`
- `test-artifacts/movement-smoke/assert_movement.py`
- `test-artifacts/movement-smoke/movement_smoke_runner.gd`

prohibited_paths:

- Doctrine files unless Michael explicitly approves.
- Schemas/contracts/exports unless Michael explicitly approves.
- Deterministic sim core unless Claude explicitly confirms the change is required and Michael approves.
- Ingredient/catalog/vendor systems.
- Layout, equipment, walls, navmesh geometry, or aesthetics unless separately approved.
- Generated Godot caches, build outputs, timestamped smoke screenshots, and telemetry output folders.

## Requirements

- Add movement-smoke assertions for excessive `Enter` duration.
- Add movement-smoke assertions for phase progress across all customer phases, including completed-ticket customers stuck before pickup.
- Give customer phase transitions practical arrival radii instead of exact 0.12m arrival where appropriate.
- Add distinct coordinator slots for mobile/delivery entry, mobile/delivery waiting, and mobile/delivery pickup.
- Ensure mobile/delivery customers reserve a slot immediately instead of sharing a fallback target.
- Add active employee target tracking for telemetry, including station, serve, break, supply-run, patrol, and return-to-station targets.
- Prevent multiple employees from simultaneously reserving or occupying the same walk-in supply-run target.
- If the walk-in is occupied, idle supply runs should wait briefly or skip; they must not twitch in front of the walk-in.
- Replace generic front-counter busy logic with visual POS service choreography:
  - customer reserves POS/order slot;
  - counter employee reserves matching service-side POS slot;
  - employee serves while that customer is ordering;
  - employee returns to assigned station/task after order intake.
- Preserve kiosk behavior as customer self-ordering; no employee should be required for kiosk ordering.
- Add drink pickup choreography if customer order/menu data exposes a drink signal without modifying deterministic sim contracts.
- If drink contents cannot be safely known in presentation layer, mark drink choreography as `UNKNOWN / needs Claude-Michael decision` rather than inventing sim data.
- Reduce local twitch sources only after telemetry proves the coordinator owns the relevant target.

## Acceptance Criteria

- Build passes with zero errors.
- Engine self-test passes.
- Movement smoke parser fails the current bad behavior:
  - long `Enter` phase;
  - completed-ticket customer stuck before pickup;
  - duplicate active slot reservations;
  - active employee supply-run target conflict;
  - excessive pair proximity/twitch near walk-in.
- After implementation, movement smoke parser passes with zero failures.
- Telemetry includes each employee's actual active target, not just station/home target.
- Front counter customers visibly walk to POS, order, leave POS, and wait/pick up correctly.
- Counter employee visibly approaches the relevant POS only while needed, then returns to normal assigned work.
- Walk-in supply runs do not send two employees into the same corner target.
- Human visual smoke confirms no twitching groups at counter or walk-in.

## Tests Required

- `dotnet build game\RestaurantSimulator.csproj --nologo`
- `dotnet run --project tools\engine-selftest\harness.csproj`
- Godot movement smoke via `test-artifacts/movement-smoke/movement_smoke_runner.gd`
- Parser: `python test-artifacts\movement-smoke\assert_movement.py <latest movement smoke folder>`
- Manual visual smoke while Godot is running, with special attention to:
  - first counter customer;
  - kiosk customers;
  - mobile/delivery pickup customers;
  - walk-in corner;
  - counter employee POS service;
  - customers after receiving food.

## Non-Goals

- Do not change deterministic event schemas.
- Do not change export ledgers or ASC compatibility.
- Do not redesign restaurant layout in this task.
- Do not change sellable catalog, ingredients, vendor systems, or back-office systems.
- Do not use screenshots alone as proof of movement correctness.
- Do not remove remaining local steering until the telemetry proves it is redundant.

## Known Risks

- Tight arrival thresholds can create false stuck behavior even when an agent is visually close enough.
- Local `HoldWithPersonalSpace` and overlap correction can fight reservation slots and create twitching.
- POS choreography may need a presentation-only order assignment layer so it does not alter the deterministic sim.
- Drink choreography may require item-level order visibility; if unavailable, it needs separate approval.
- Walk-in target contention is partly a reservation issue and partly a local steering issue.
- Parser thresholds must catch human-visible jitter, not just large stuck distances.

## Open Questions For Claude

- Should POS service be modeled as presentation-only reservations tied to `CustomerAgent.Phase.Ordering`, or should Codex expose additional read-only sim task state for `order_intake`?
- Is it acceptable to add presentation-only customer subphases such as `ToDrink`, `GettingDrink`, `ToWait`, or should this remain inside existing `CustomerAgent.Phase` values?
- Can presentation safely infer drink orders from current catalog/order data, or should drink pickup wait for a separate sim-facing order-item view?
- Should local `HoldWithPersonalSpace` be reduced immediately for reserved/holding agents, or only after the new telemetry catches and proves the walk-in/counter twitch is gone?
- What are the exact phase timeout thresholds Claude wants for `Enter`, `Dining`, `Busing`, and `Leave`?

## Claude Review

Verdict: `PENDING`

Allowed verdicts:

- `APPROVED_TO_IMPLEMENT`
- `APPROVED_WITH_CORRECTIONS`
- `REWORK_REQUIRED`
- `HUMAN_DECISION_REQUIRED`

Claude should update this section before Codex makes source code changes.

### Claude Notes

PENDING

### Required Corrections

PENDING

## Codex Implementation Notes

No source implementation should proceed until Claude approves this packet.

Once approved, Codex should:

1. Update the parser first so current bad behavior fails.
2. Fix customer arrival/slot behavior.
3. Fix walk-in supply-run reservation/telemetry behavior.
4. Fix POS service choreography.
5. Re-run build, engine self-test, movement smoke, parser, and visual review.
6. Update this file with implementation evidence.

## Validation Evidence

Most recent known evidence before this handoff replacement:

- `dotnet build game\RestaurantSimulator.csproj --nologo`: passed with 0 warnings and 0 errors.
- `dotnet run --project tools\engine-selftest\harness.csproj`: passed with `SELF-TEST TOTAL: 120/120`, `INGREDIENT-MODEL TOTAL: 10/10`, `CAREER-TEST TOTAL: 11/11`.
- Movement smoke folder: `test-artifacts/movement-smoke/20260619_132902`
- Existing parser result on that folder: passed with 379 samples and 2723 agent samples, but this is now known insufficient because it missed long `Enter` stalls and walk-in target contention.

## Michael Approval

Specification Approved: `PENDING`
Implementation Approved: `PENDING_CLAUDE_REVIEW`
Merge Approved: `PENDING`

