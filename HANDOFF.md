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
Handoff Version: 3.1
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

The first movement-authority pass (a `CrowdCoordinator`, reservation slots, movement telemetry, and an automated smoke parser) landed at HEAD and passed the parser. While the smoke test was open, Michael observed runtime failures the parser and screenshots missed: customers/employees twitch in place, the first customer in line did not move, two employees stuck at the walk-in corner, the counter employee looked idle, and POS service choreography is not explicit. This follow-up makes movement and service choreography robust and adds telemetry that catches these runtime failures. (The lobby-drink stop is part of the original intent but is blocked on a data decision — see D1; it is deferred from the active packet.)

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

Sequencing / determinism:
- Reduce conflicting local steering for reserved/holding agents ONLY after telemetry proves the coordinator owns the target; one mechanism at a time, re-validating after each.
- Engine self-test deterministic replay (canonical event export for a fixed config/scenario/seed) must be byte-identical to Base SHA, not merely a passing suite. Codex confirms the comparison method.

Drink stop: DEFERRED from this packet pending Decision D1. Do not implement `ToDrink`/`GettingDrink` or any soda-machine choreography unless Michael approves D1 Option A.

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
- No write-back to `SimRunState`; no new sim-facing order-item state without Michael approval (gates the drink stop — see D1).
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
- Walk-in contention is part reservation, part local steering.
- Parser must catch human-visible jitter, not only large stuck distances.

## Decisions Reserved for Michael

- D1 — Drink-stop data exposure. Codex read-only inspection (evidence below) confirms there is NO clean per-order drink flag at the presentation boundary: `OrderCreatedEvt` carries only `(channel, order_id)` and `CustomerAgent` receives no cart/item list. Drink data exists deterministically in the sim (`game/config/menu_products.json`, `SimRunState.BuildCart()`); item ids appear in the `item.taken` export, but driving choreography from the exported log inverts the presentation→sim dependency and is rejected. Options:
  - A) Approve a minimal in-process read-only drink signal (e.g. a `hasDrink` bool on the `OrderCreatedEvt` payload, or a read-only `SimRunState.OrderHasDrink(orderId)` query) derived from the already-deterministic cart. Constraint: in-process only; must NOT alter the exported event schema, ASC contract, or any deterministic output; the byte-identical gate still applies. Unblocks the drink stop.
  - B) Defer the drink stop entirely; ship the movement/POS/walk-in/mobile fixes now and revisit drinks as a separate small task.
  - Claude recommendation: B for this task — the runtime failures Michael flagged are the priority and should not wait on a data-exposure decision; the drink stop is a clean fast-follow. A is acceptable and low-risk if Michael wants drinks in scope now, provided it stays in-process read-only.
  - Status: AWAITING MICHAEL DECISION.
- D2 — Ratify the proposed phase-timeout thresholds. Status: OPEN.
- D3 — Product-visible pacing introduced by the drink detour. Contingent on D1 = A; N/A if D1 = B. Status: OPEN.

## Claude Review

Verdict: `APPROVED_WITH_CORRECTIONS`

Corrections (byte-identical determinism gate; jitter assertion; conditional drink-data gating; presentation-only POS boundary with counter/kiosk designation; walk-in standoff; minimal subphases; removal sequencing; phase timeouts) are folded into Requirements and Acceptance above. Basis: the packet is evidence-driven, parser-first, and scope-disciplined. D1 is now fact-reported — no clean presentation drink flag exists — and Codex correctly deferred rather than improvising; the drink stop is the only blocked item and should not hold up the movement fixes. Everything except the drink stop may proceed once Michael approves the specification.

Reviewed By: Claude Opus — 2026-06-19.

## Codex Implementation Notes

No source edits until Michael approves the specification (and D1 for drink scope). D1 read-only inspection is complete. Once approved, implement in order:
1. Update parser + telemetry so the current data fails (including jitter); confirm red.
2. Customer arrival radii + mobile distinct-slot reservations.
3. Walk-in supply-run reservation + standoff + active-target telemetry.
4. POS service choreography (presentation-only) + counter/kiosk designation.
5. Reduce conflicting local steering for reserved/holding agents, one at a time.
6. Re-run build, self-test (byte-identical), smoke, parser (green), visual review; record evidence below.
7. Drink stop only if Michael approves D1 Option A — then add the in-process read-only signal and `ToDrink`/`GettingDrink`, re-validating the byte-identical gate.

## Validation Evidence

Prior baseline (HEAD `9128fd7`, now known insufficient): build 0 warnings / 0 errors; self-test 120/120, ingredient-model 10/10, career-test 11/11; smoke folder `20260619_132902` passed the old parser (379 samples / 2723 agent samples) but missed long `Enter` stalls and walk-in contention.

D1 read-only evidence (Codex, inspection only): `OrderCreatedEvt` is `(channel, order_id)` and `AgentManager` cannot know whether a spawned visual customer ordered a drink; `SimRunState.BuildCart()` selects drink items deterministically; `item.taken` emits `item_id` and `ItemLedger`/`AllJsonl` carry item info, but none is a clean typed presentation API for order-time drink choreography. Conclusion: no drink-stop implementation without Michael approving a read-only order-item exposure (D1 Option A).

New movement evidence: PENDING — Codex to populate after approval (commands run, parser red→green, byte-identical replay result, runtime notes).

## Michael Approval

Specification Approved: `PENDING`
Material Decisions (D1–D3) Approved: `PENDING`
Merge Approved: `PENDING`

## Rollback

Working tree is clean at Base SHA `9128fd7`. Safest rollback is a revert commit of the single follow-up feature commit, or a targeted commit restoring prior behavior. No reset, rebase, or discard without Michael approval.

## Next Authorized Action

Michael: (1) approve or amend the specification; (2) decide D1 (A = approve in-process read-only drink signal, B = defer drink stop); (3) ratify D2 thresholds; (4) decide D3 only if D1 = A. Codex must not make source edits until specification approval. After approval, Codex implements steps 1–6 in the order above (step 7 only if D1 = A) and updates this file with evidence.
