# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `9128fd706a7022ebd52a59dd72208eef0a4434dc`
Current HEAD: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Task ID: `movement-runtime-authority-followup`
Task Name: Fix visual movement stalls, POS service choreography, and deprecated movement interference
Status: `AWAITING_CODEX_CORRECTIONS`
Handoff Version: 4.0
Last Updated: 2026-06-19
Updated By: Claude Opus

## Workflow Contract

HANDOFF.md is the permanent, revolving coordination channel between Claude Opus and Codex. It describes only the current task state; stale or superseded content is removed on each update; Git history preserves prior versions.

- Michael owns product intent, material decisions, merge/release approval, final acceptance.
- Claude Opus owns task definition, architecture/risk review, acceptance criteria, packet approval, implementation review.
- Codex owns repository inspection, source implementation, test execution, implementation evidence.
- Codex must not make source edits until Claude approves the packet and Michael approves the specification.
- Repository files are the source of truth; model memory is secondary.
- Both models may read/update this file; neither may merge, discard, reset, rebase, or delete work without Michael approval.

## Current Task

The first movement-authority pass (a `CrowdCoordinator`, reservation slots, telemetry, parser) landed and passed its parser, but Michael observed runtime failures the parser/screenshots missed: customers/employees twitch in place, the first customer in line did not move, two employees stuck at the walk-in, the counter employee looked idle, POS choreography not explicit. This follow-up makes movement and service choreography robust and adds telemetry that catches those runtime failures. The implementation is complete and under Claude review (see Claude Review). The lobby-drink stop is deferred (D1=B).

## Confirmed Root Causes (Codex telemetry, folder `test-artifacts/movement-smoke/20260619_132902`)

- `CustomerAgent.Enter` required exact (~0.12m) arrival; mobile customers — some ticket-complete — stalled 54–162s near target (e.g. `cust_ord_000002`: 162.5s, 0.193m away).
- Mobile/delivery customers shared a fallback target instead of reserving distinct entry/wait/pickup slots from frame 1.
- `emp_9`/`emp_28` converged at the walk-in on simultaneous supply runs (217 samples); the supply-run target carried no reservation.
- Employee telemetry reported station/home target, not the active supply/break/serve target, hiding the conflict.
- Front counter used `StationBusy("work_counter") = _sim.Tickets > 0`, so employees reacted to any open ticket, not a specific lobby customer ordering at a POS.
- Local steering (`AvoidedHeading`, `ResolveCharacterOverlap`, `HoldWithPersonalSpace`, stuck recovery) can fight coordinator reservations near a target. Old `Separate()`/`ResolveOccupiedTarget()` already removed.

## Objective

Over a Godot movement smoke run, the parser fails the runtime failures above before the fix and passes after; no customer remains indefinitely in any phase; POS/walk-in/mobile choreography is correct; and the engine self-test deterministic replay stays byte-identical to Base SHA.

## Requirements (Claude-approved packet, corrections folded in)

Parser / telemetry:
- Parser updated FIRST so the current `20260619_132902` data fails on: long `Enter`; completed-ticket customer stuck before pickup; duplicate active slot reservation; active employee supply-run target conflict; excessive pair proximity near the walk-in; and a phase-independent JITTER rule (agent within arrival radius of its reserved target but oscillating — near-zero net displacement with high path-length over a short window).
- Telemetry reports each employee's ACTIVE target (station/serve/break/supply-run/patrol/return), not station/home.

Customer movement: practical arrival radii (completed-ticket customers must advance); mobile/delivery reserve distinct entry/wait/pickup slots immediately.

Walk-in: single-occupancy on the supply target; if occupied, idle supply runs wait at a reserved STANDOFF or skip — never crowd/twitch at the door.

POS service (presentation-only): customer reserves a specific POS slot; the matching counter employee reserves that POS's service-side slot, serves only while that customer is in `Phase.Ordering`, then returns to assigned work; trigger is the reservation + `Phase.Ordering`, NOT `_sim.Tickets`; no `SimRunState` write-back; lobby customers carry a presentation-only counter-vs-kiosk designation; kiosk customers self-order.

Sequencing / determinism: reduce conflicting local steering for reserved/holding agents ONLY after telemetry proves the coordinator owns the target, one mechanism at a time; engine self-test deterministic replay (canonical event export for a fixed config/scenario/seed) byte-identical to Base SHA.

Drink stop: DEFERRED (D1=B) — separate future task; not in this packet.

## Acceptance Criteria (verification state from Claude review)

- [x] Build passes with zero errors.
- [x] Engine self-test deterministic replay byte-identical to Base SHA — PASS (confirm compared artifact, Correction 3).
- [~] Parser FAILS the old data on every new rule — only `enter_timeout`, `complete_ticket_before_pickup`, `walkin_proximity` proven; JITTER, supply-run-conflict, duplicate-reservation NOT proven (Corrections 1–2).
- [x] After fixes, parser passes with zero failures (379 samples / 2157 agent samples) — meaningful only once detectors are proven.
- [x] Telemetry includes each employee's actual active target.
- [~] Counter/kiosk choreography — reservation mechanism implemented; visual sequence not yet confirmed (Correction 4).
- [x] Walk-in supply runs never share a corner target — proximity rule green.
- [~] Human visual smoke: no twitching, first-in-line moves — NOT proven by one overhead still (Corrections 1, 4).

## Phase Timeouts (ratified — D2)

Real seconds: `Enter` > 20; `Ordering` > 12 counter / 18 kiosk; `Waiting` > 180 unless linked ticket incomplete; `ToPickup` > 20 when ticket complete; `Dining` > ~2× max dine duration (Codex confirm `dineSeconds` upper bound, believed ~65s → ~130s); `Busing` > 30; `Leave` > 25; outside-with-food > 5.

## Allowed Paths

`HANDOFF.md`; `game/scripts/world/{CrowdCoordinator,AgentManager,CustomerAgent,EmployeeAgent,CharacterRig}.cs`; `test-artifacts/movement-smoke/{assert_movement.py,movement_smoke_runner.gd}`.

## Prohibited Changes / Non-Goals

- No deterministic event schema, export ledger, or ASC-contract change; movement telemetry stays diagnostic-only.
- No write-back to `SimRunState`; no new sim-facing order-item state without Michael approval.
- No deterministic sim-core change unless Claude confirms required and Michael approves.
- No ingredient/catalog/vendor/back-office, layout, equipment, wall, navmesh, or aesthetic changes.
- No destructive Git operations; no scope expansion; do not commit caches, build outputs, smoke screenshots, or telemetry folders.
- Screenshots alone are not proof; local steering not removed until telemetry proves it redundant.

## Tests Required

- `dotnet build game\RestaurantSimulator.csproj --nologo`
- `dotnet run --project tools\engine-selftest\harness.csproj` (+ byte-identical event-export comparison vs Base SHA)
- Godot movement smoke via `test-artifacts/movement-smoke/movement_smoke_runner.gd`
- `python test-artifacts\movement-smoke\assert_movement.py <latest folder>` — must FAIL on a known-violation fixture, PASS on the clean run
- Manual visual smoke: first counter customer, kiosk customers, mobile/delivery pickup, walk-in corner, POS service, customers after receiving food

## Resolved Decisions

- D1 = B: drink stop deferred to a separate future task; no order-item exposure.
- D2: phase-timeout thresholds ratified (values above).
- D3: N/A (D1=B).

## Known Risks

- Tight arrival thresholds can create false stuck — practical radii required.
- Local `HoldWithPersonalSpace`/overlap correction can fight reservations and cause twitch — CharacterRig steering was intentionally left in place this pass; twitch elimination therefore rests on the reservations removing contention, which the jitter detector must confirm.
- A jitter detector that never fires reproduces the original false-green; it must be proven against known-twitch data.

## Claude Review

Verdict: `APPROVED_WITH_CORRECTIONS`

Implementation is scope-clean and the core fixes are sound: build green, self-test 120/120, byte-identical PASS, new smoke green, active-target telemetry added, POS/mobile/walk-in reservations in place, drink stop correctly deferred. Three verification gaps must close before merge; the work itself is not in question, so these are corrections, not rework.

Required Corrections:
1. Prove the JITTER rule fires. The old folder `20260619_132902` was captured during the session where Michael saw twitching, yet the red run flagged only `enter_timeout`, `complete_ticket_before_pickup`, `walkin_proximity` — not jitter. The detector is therefore either mis-tuned or aliasing the sample interval. Demonstrate it goes red on a known-twitch capture (pre-fix steering or a synthetic oscillation fixture) before any green run is trusted. This is Michael's headline symptom.
2. Prove the supply-run-conflict and duplicate-reservation rules fire on a fixture containing each violation (the old data couldn't exercise supply-run-conflict — it predates active-target telemetry).
3. Confirm the byte-identical comparison diffed the canonical event export (deterministic stream), not the harness pass/fail summary. The git-archive method is fine; just confirm the artifact.
4. (Michael) Human visual smoke still owed: one overhead still cannot confirm "no twitching" (a motion artifact) or the POS/kiosk/post-food sequences. Watch the running sim before merge.

Corroborating: agent-samples fell 2723 → 2157 at equal 379 samples, consistent with customers no longer stalling — a good sign the Enter/pickup fixes landed.

On closing 1–3 and Michael's visual smoke (4), this is `APPROVED_TO_CONTINUE` toward merge.

Reviewed By: Claude Opus — 2026-06-19.

## Codex Implementation Notes

Address Corrections 1–3, then re-run the full validation set and update the evidence below. Make no other source changes; do not reduce CharacterRig local steering this pass unless the jitter proof shows it is still firing harmfully (then one mechanism at a time, re-validated).

## Validation Evidence

Implementation evidence (Codex, 2026-06-19):
- Source changes within allowed paths: `CrowdCoordinator.cs`, `AgentManager.cs`, `CustomerAgent.cs`, `EmployeeAgent.cs`, `assert_movement.py`, this handoff. (CharacterRig untouched — local steering intentionally retained this pass.)
- Parser updated first; old folder `20260619_132902` red run flagged `enter_timeout`, `complete_ticket_before_pickup`, `walkin_proximity`. (Jitter / supply-run-conflict / duplicate-reservation NOT shown — Corrections 1–2.)
- Fixes: practical arrival radii (Enter/Ordering/Waiting/Dining/Busing/Leave); distinct `mobile_entry/wait/pickup_*` reservations; presentation-only `pos_order_*`/`pos_service_*`; counter service driven by POS reservation not `_sim.Tickets`; walk-in single-door + `walkin_standoff_*`; employee telemetry uses active target.
- `dotnet build`: PASS, 0/0. Self-test: PASS 120/120, 10/10, 11/11.
- New smoke `20260619_151950`; new parser: PASS, 379 samples / 2157 agent samples / 0 failures.
- Byte-identical: baseline `9128fd7` via `git archive` (worktree add hung); baseline vs current harness outputs compared byte-for-byte: PASS (confirm artifact — Correction 3).
- Visual: `20260619_151950/05_overhead.png` reviewed, no pileup at that sample. Evidence folder untracked; do not commit unless Michael wants smoke artifacts in the repo.

D1 read-only basis (settled, B): no clean per-order drink flag at the presentation boundary; `OrderCreatedEvt` is `(channel, order_id)`.

Correction evidence: PENDING — Codex to add (jitter/supply-run/duplicate red proofs; byte-identical artifact confirmation).

## Michael Approval

Specification Approved: `APPROVED 2026-06-19`
Material Decisions (D1–D3) Approved: `APPROVED 2026-06-19` (D1=B, D2 ratified, D3 N/A)
Human Visual Smoke: `PENDING` (Correction 4)
Merge Approved: `PENDING`

## Rollback

Working tree clean at Base SHA `9128fd7`. Safest rollback is a revert commit of the single follow-up feature commit, or a targeted restoring commit. No reset, rebase, or discard without Michael approval.

## Next Authorized Action

Codex addresses Corrections 1–3 (prove the jitter, supply-run-conflict, and duplicate-reservation rules go red on known-violation fixtures; confirm the byte-identical artifact), re-runs the full validation set, and updates the evidence above — no other source edits. Michael performs the human visual smoke (Correction 4). No merge until corrections close and Claude returns `APPROVED_TO_CONTINUE`.
