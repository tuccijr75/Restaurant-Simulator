# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `9128fd706a7022ebd52a59dd72208eef0a4434dc`
Current HEAD: `9128fd706a7022ebd52a59dd72208eef0a4434dc`
Task ID: `movement-runtime-authority-followup`
Task Name: Fix visual movement stalls, POS service choreography, and deprecated movement interference
Status: `AWAITING_MICHAEL_APPROVAL`
Handoff Version: 3.0
Last Updated: 2026-06-19
Updated By: Claude Opus

## Workflow Contract

HANDOFF.md is the permanent, revolving coordination channel between Claude Opus and Codex. It describes only the current task state; stale or superseded content is removed on each update; Git history preserves prior versions.

- Michael owns product intent, material decisions, merge/release approval, final acceptance.
- Claude Opus owns task definition, architecture/risk review, acceptance criteria, packet approval.
- Codex owns repository inspection, source implementation, test execution, implementation evidence.
- Codex must not make source edits until Claude approves the packet and Michael approves the specification.
- Repository files are the source of truth; model memory is secondary.
- Both models may read/update this file; neither may merge, discard, reset, rebase, or delete work without Michael approval.

## Current Task

The first movement-authority pass (a `CrowdCoordinator`, reservation slots, movement telemetry, and an automated smoke parser) landed at HEAD and passed the parser. While the smoke test was open, Michael observed runtime failures the parser and screenshots missed: customers/employees twitch in place, the first customer in line did not move, two employees stuck at the walk-in corner, the counter employee looked idle, and POS/drink service choreography is not explicit. This follow-up makes movement and service choreography robust and adds telemetry that catches these runtime failures.

## Confirmed Root Causes (Codex telemetry, folder `test-artifacts/movement-smoke/20260619_132902`)

- `CustomerAgent.Enter` requires exact (~0.12m) arrival; mobile customers — some ticket-complete — stalled 54–162s near their target (e.g. `cust_ord_000002`: 162.5s, 0.193m away). The parser did not fail long `Enter`, so the run was falsely green.
- Mobile/delivery customers share a fallback target instead of reserving distinct entry/wait/pickup slots from the first frame.
- `emp_9` and `emp_28` converged at the walk-in on simultaneous supply runs (217 samples); the supply-run target carries no reservation.
- Employee telemetry reports station/home target, not the active supply/break/serve target, hiding the real conflict.
- Front counter uses `StationBusy("work_counter") = _sim.Tickets > 0`, so employees react to any open ticket rather than a specific lobby customer ordering at a POS.
- Remaining local steering (`AvoidedHeading`, `ResolveCharacterOverlap`, `HoldWithPersonalSpace`, stuck lateral/reverse recovery) may fight coordinator reservations when agents hold near a target. Old `Separate()` and `ResolveOccupiedTarget()` are already removed.

## Objective

Over a Godot movement smoke run, the parser fails the runtime failures above before the fix and passes with zero failures after; no customer remains indefinitely in any phase; POS, walk-in, and mobile choreography are correct; and the engine self-test deterministic replay stays byte-identical to Base SHA.

## Requirements (Claude-approved packet, corrections folded in)

Parser / telemetry:
- Update the parser FIRST so the current `20260619_132902` data fails on: long `Enter`; completed-ticket customer stuck before pickup; duplicate active slot reservation; active employee supply-run target conflict; excessive pair proximity near the walk-in; and a phase-independent JITTER rule — agent within arrival radius of its reserved target but oscillating (near-zero net displacement with high path-length over a short window).
- Telemetry reports each employee's ACTIVE target (station / serve / break / supply-run / patrol / return), not just station/home.

Customer movement:
- Replace exact ~0.12m arrival with practical arrival radii where appropriate; completed-ticket customers must advance.
- Mobile/delivery reserve distinct entry/wait/pickup slots immediately (no shared fallback).

Walk-in:
- Single-occupancy on the walk-in supply target. If occupied, idle supply runs wait at a reserved STANDOFF or skip — never crowd or twitch at the door.

POS service (presentation-only):
- Customer reserves a specific POS/order slot; the matching counter employee reserves the service-side slot of THAT POS, serves only while that customer is in `Phase.Ordering`, then releases and returns to assigned work.
- The serve trigger is the coordinator reservation + `Phase.Ordering`, NOT `_sim.Tickets`; no write-back to `SimRunState`.
- Requires a presentation-only counter-vs-kiosk designation for lobby customers; kiosk customers self-order with no employee required.

Drink stop (CONDITIONAL — see Decision D1):
- Add a lobby soda-machine stop before waiting ONLY if a drink indicator is already readable read-only from existing order/ticket data with no sim or export contract change. If unavailable, EXCLUDE from this task and raise to Michael.
- If implemented, presentation-only subphases `ToDrink`/`GettingDrink` are acceptable; do not add `ToWait` (redundant with the Waiting reservation).

Sequencing / determinism:
- Reduce conflicting local steering for reserved/holding agents ONLY after telemetry proves the coordinator owns the target; one mechanism at a time, re-validating after each.
- Engine self-test deterministic replay (canonical event export for a fixed config/scenario/seed) must be byte-identical to Base SHA, not merely a passing suite. Codex confirms the comparison method.

## Acceptance Criteria

- [ ] Build passes with zero errors.
- [ ] Engine self-test deterministic replay byte-identical to Base SHA.
- [ ] Parser FAILS the current `20260619_132902` data on every new rule (long `Enter`, stuck-before-pickup, duplicate reservation, supply-run conflict, walk-in proximity, jitter) — demonstrated before any fix.
- [ ] After fixes, parser passes with zero failures.
- [ ] Telemetry includes each employee's actual active target.
- [ ] Counter customers visibly walk to a POS, order, leave, then wait/pick up; the counter employee approaches the POS only while needed, then returns; kiosk customers self-serve.
- [ ] Walk-in supply runs never send two employees into the same corner target.
- [ ] Human visual smoke: no twitching groups at counter or walk-in; first-in-line moves.

## Phase Timeouts (proposed — Michael ratifies, Decision D2)

Real seconds: `Enter` > 20; `Ordering` > 12 counter / 18 kiosk; `Waiting` > 180 unless linked ticket incomplete; `ToPickup` > 20 when ticket complete; `Dining` > ~2× the configured max dine duration (Codex confirm `dineSeconds` upper bound, believed ~65s → ~130s); `Busing` > 30; `Leave` > 25; outside-with-food > 5.

## Allowed Paths

`HANDOFF.md`; `game/scripts/world/{CrowdCoordinator,AgentManager,CustomerAgent,EmployeeAgent,CharacterRig}.cs`; `test-artifacts/movement-smoke/{assert_movement.py,movement_smoke_runner.gd}`.

## Prohibited Changes / Non-Goals

- No deterministic event schema, export ledger, or ASC-contract change; movement telemetry stays diagnostic-only.
- No write-back to `SimRunState`; no new sim-facing order-item state without Michael approval (gates the drink stop).
- No deterministic sim-core change unless Claude confirms it is required and Michael approves.
- No ingredient/catalog/vendor/back-office changes; no layout, equipment, wall, navmesh, or aesthetic changes.
- No destructive Git operations; no scope expansion; do not commit generated caches, build outputs, timestamped smoke screenshots, or telemetry folders.
- Screenshots alone are not proof; local steering is not removed until telemetry proves it redundant.

## Tests Required

- `dotnet build game\RestaurantSimulator.csproj --nologo`
- `dotnet run --project tools\engine-selftest\harness.csproj` (plus the byte-identical event-export comparison vs Base SHA)
- Godot movement smoke via `test-artifacts/movement-smoke/movement_smoke_runner.gd`
- `python test-artifacts\movement-smoke\assert_movement.py <latest folder>` — must FAIL pre-fix, PASS post-fix
- Manual visual smoke: first counter customer, kiosk customers, mobile/delivery pickup, walk-in corner, counter POS service, customers after receiving food

## Known Risks

- Tight arrival thresholds can create false stuck even when an agent is visually close enough — practical radii required.
- Local `HoldWithPersonalSpace`/overlap correction can fight reservation slots and cause twitch — reduce only after proof.
- POS choreography needs a presentation-only assignment layer so it never alters the deterministic sim.
- Drink stop may need item-level order visibility; gated on D1.
- Walk-in contention is part reservation, part local steering.
- Parser must catch human-visible jitter, not only large stuck distances.

## Decisions Reserved for Michael

- D1 — Drink-data exposure. Codex read-only inspection found drink/frozen-dessert products in `game/config/menu_products.json` and item/cart logic in `game/scripts/sim/SimRunState.cs`, but no clean per-order drink flag is exposed at the current presentation boundary (`OrderCreatedEvt` only provides channel + order id; `CustomerAgent` receives no cart/item list). `item.taken` events include item ids in the sim event stream, but using those as presentation choreography input would be indirect log parsing and still does not provide an order-time drink flag. Claude recommendation stands: DEFER drink-stop choreography unless Michael approves a new read-only order-item exposure. Status: FACT REPORTED / AWAITING MICHAEL DECISION.
- D2 — Ratify the proposed phase-timeout thresholds. Status: OPEN.
- D3 — Any product-visible pacing introduced by the drink detour. Status: OPEN.

## Claude Review

Verdict: `APPROVED_WITH_CORRECTIONS`

Corrections (byte-identical determinism gate; jitter assertion; conditional drink-data gating; presentation-only POS boundary with counter/kiosk designation; walk-in standoff; minimal subphases; removal sequencing; phase timeouts) are folded into Requirements and Acceptance above. Basis: the packet is evidence-driven, parser-first, and scope-disciplined; the gaps were the missing jitter assertion, the under-specified determinism gate, and the unconfirmed drink-data assumption — all now constrained. Implementation may proceed on everything EXCEPT the drink stop once Michael approves the specification; the drink stop waits on D1.

Reviewed By: Claude Opus — 2026-06-19.

## Codex Implementation Notes

No source edits until Michael approves the specification (and D1 for drink scope). Codex MAY now perform the read-only drink-signal inspection to resolve D1 — inspection only, no edits. Once approved, implement in order:
1. Update parser + telemetry so the current data fails (including jitter); confirm red.
2. Customer arrival radii + mobile distinct-slot reservations.
3. Walk-in supply-run reservation + standoff + active-target telemetry.
4. POS service choreography (presentation-only) + counter/kiosk designation.
5. (Only if D1 = available) drink-stop choreography.
6. Reduce conflicting local steering for reserved/holding agents, one at a time.
7. Re-run build, self-test (byte-identical), smoke, parser (green), visual review; record evidence below.

## Validation Evidence

Prior baseline (HEAD `9128fd7`, now known insufficient): build 0 warnings / 0 errors; self-test 120/120, ingredient-model 10/10, career-test 11/11; smoke folder `20260619_132902` passed the old parser (379 samples / 2723 agent samples) but missed long `Enter` stalls and walk-in contention.

New evidence: PENDING — Codex to populate (commands run, parser red→green, byte-identical replay result, runtime notes).

Read-only D1 evidence:

- `rg` over `game/scripts`, `game/config`, and `tools` found beverage/frozen-dessert catalog entries and `SimRunState.BuildCart()` drink item selection.
- `game/scripts/sim/SimRunState.cs`: `OrderCreatedEvt` is `(channel, order_id)` only; `AgentManager` subscribes to this and therefore cannot directly know whether a spawned visual customer ordered a drink.
- `game/scripts/sim/SimRunState.cs`: `item.taken` emits `item_id`, and `ItemLedger`/`AllJsonl` contain item information, but this is not a clean typed presentation API for order-time drink choreography.
- Conclusion: no source edits should implement drink-stop choreography under the current packet unless Michael approves exposing read-only order item data.

## Michael Approval

Specification Approved: `PENDING`
Material Decisions (D1–D3) Approved: `PENDING`
Merge Approved: `PENDING`

## Rollback

Working tree is clean at Base SHA `9128fd7`. Safest rollback is a revert commit of the single follow-up feature commit, or a targeted commit restoring prior behavior. No reset, rebase, or discard without Michael approval.

## Next Authorized Action

Michael reviews and approves the specification and Decisions D1–D3. Codex may perform the read-only drink-signal inspection now to resolve D1 but must not make source edits until Michael approves. After approval, Codex implements in the order above and updates this file with evidence.
