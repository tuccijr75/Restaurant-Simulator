# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `9128fd706a7022ebd52a59dd72208eef0a4434dc`
Current HEAD: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Task ID: `movement-runtime-authority-followup`
Task Name: Fix visual movement stalls, POS service choreography, and deprecated movement interference
Status: `AWAITING_MICHAEL_VISUAL_SMOKE_AND_MERGE`
Handoff Version: 5.0
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

The first movement-authority pass landed but Michael observed runtime failures the parser/screenshots missed (twitch in place, first-in-line not moving, two employees stuck at the walk-in, idle counter employee, no explicit POS choreography). This follow-up makes movement and service choreography robust and adds telemetry to catch those failures. Implementation and Corrections 1–3 are complete and Claude-reviewed; the remaining gate is Michael's human visual smoke, then merge. Drink stop deferred (D1=B).

## Confirmed Root Causes (folder `test-artifacts/movement-smoke/20260619_132902`)

- `CustomerAgent.Enter` required exact (~0.12m) arrival; mobile customers — some ticket-complete — stalled 54–162s near target (`cust_ord_000002`: 162.5s, 0.193m away).
- Mobile/delivery customers shared a fallback target instead of reserving distinct entry/wait/pickup slots from frame 1.
- `emp_9`/`emp_28` converged at the walk-in on simultaneous supply runs (217 samples); the supply-run target carried no reservation.
- Employee telemetry reported station/home target, not the active supply/break/serve target, hiding the conflict.
- Front counter used `StationBusy("work_counter") = _sim.Tickets > 0`, so employees reacted to any open ticket, not a specific POS customer.
- Local steering (`AvoidedHeading`, `ResolveCharacterOverlap`, `HoldWithPersonalSpace`, stuck recovery) can fight coordinator reservations near a target. Old `Separate()`/`ResolveOccupiedTarget()` already removed.

## Objective

Over a Godot movement smoke run, the parser fails the runtime failures above (proven via real data + fixtures) and passes after the fix; no customer remains indefinitely in any phase; POS/walk-in/mobile choreography is correct; engine self-test deterministic replay byte-identical to Base SHA.

## Requirements (Claude-approved packet)

Parser / telemetry: parser detects long `Enter`; completed-ticket customer stuck before pickup; duplicate active slot reservation; active employee supply-run target conflict; excessive pair proximity at the walk-in; and a phase-independent JITTER rule (within arrival radius but oscillating — high path-length, near-zero net displacement). Telemetry reports each employee's ACTIVE target.

Customer movement: practical arrival radii (completed-ticket customers advance); mobile/delivery reserve distinct entry/wait/pickup slots immediately.

Walk-in: single-occupancy supply target; if occupied, idle runs wait at a reserved STANDOFF or skip — never crowd/twitch at the door.

POS service (presentation-only): customer reserves a specific POS slot; the matching counter employee reserves that POS's service-side slot, serves only while that customer is in `Phase.Ordering`, then returns; trigger is reservation + `Phase.Ordering`, NOT `_sim.Tickets`; no `SimRunState` write-back; lobby customers carry a presentation-only counter-vs-kiosk designation; kiosk customers self-order.

Sequencing / determinism: reduce conflicting local steering only after telemetry proves the coordinator owns the target, one mechanism at a time; engine self-test deterministic replay (canonical `AllJsonl` export, fixed config/scenario/seed) byte-identical to Base SHA.

Drink stop: DEFERRED (D1=B) — separate future task.

## Acceptance Criteria (verification state)

- [x] Build passes with zero errors.
- [x] Deterministic replay byte-identical to Base SHA — `AllJsonl` raw contents for `normal_day`/seed `12345`, baseline vs current: PASS.
- [x] Parser fails on every required rule class — real data (`enter_timeout`, `complete_ticket_before_pickup`, `walkin_proximity`) + fixtures (`jitter`, `supply_run_conflict`, `duplicate_reserved_slot`). Residual: the jitter rule was proven on a synthetic fixture, not on the real twitch capture — see Claude Review.
- [x] After fixes, parser passes with zero failures (379 / 2157 agent samples).
- [x] Telemetry includes each employee's active target.
- [~] Counter/kiosk choreography — mechanism implemented; visual sequence pending Michael's smoke.
- [x] Walk-in supply runs never share a corner target — proximity rule green.
- [~] Human visual smoke: no twitching, first-in-line moves — BINDING gate, pending Michael (see Claude Review).

## Phase Timeouts (ratified — D2)

Real seconds: `Enter` > 20; `Ordering` > 12 counter / 18 kiosk; `Waiting` > 180 unless ticket incomplete; `ToPickup` > 20 when ticket complete; `Dining` > ~2× max dine duration (Codex confirm `dineSeconds` upper bound, ~65s → ~130s); `Busing` > 30; `Leave` > 25; outside-with-food > 5.

## Allowed Paths

`HANDOFF.md`; `game/scripts/world/{CrowdCoordinator,AgentManager,CustomerAgent,EmployeeAgent,CharacterRig}.cs`; `test-artifacts/movement-smoke/{assert_movement.py,movement_smoke_runner.gd}`.

## Prohibited Changes / Non-Goals

- No deterministic event schema, export ledger, or ASC-contract change; telemetry stays diagnostic-only.
- No `SimRunState` write-back; no new sim-facing order-item state without Michael approval.
- No deterministic sim-core change unless Claude confirms required and Michael approves.
- No ingredient/catalog/vendor/back-office, layout, equipment, wall, navmesh, or aesthetic changes.
- No destructive Git ops; no scope expansion; do not commit caches, build outputs, smoke screenshots, or telemetry folders.
- Screenshots alone are not proof; local steering not removed until telemetry proves it redundant.

## Tests Required

- `dotnet build game\RestaurantSimulator.csproj --nologo`
- `dotnet run --project tools\engine-selftest\harness.csproj` (+ `AllJsonl` byte-identical comparison vs Base SHA)
- Godot movement smoke via `test-artifacts/movement-smoke/movement_smoke_runner.gd`
- `python test-artifacts\movement-smoke\assert_movement.py <folder>` — red on known-violation data/fixtures, green on the clean run
- Manual visual smoke: first counter customer, kiosk customers, mobile/delivery pickup, walk-in corner, POS service, customers after receiving food

## Resolved Decisions

- D1 = B: drink stop deferred to a separate future task; no order-item exposure.
- D2: phase-timeout thresholds ratified (values above).
- D3: N/A (D1=B).

## Known Risks

- A jitter detector tuned for idealized oscillation may miss lower-amplitude real twitch; it did not fire on the real `20260619_132902` capture. The human visual smoke is therefore the binding twitch gate; threshold tuning is a follow-up if needed.
- CharacterRig local steering was intentionally retained; twitch elimination rests on the reservations removing contention.

## Claude Review

Verdict: `APPROVED_TO_CONTINUE` (Claude review gate cleared; advances to Michael's visual smoke + merge).

Corrections 1–3 accepted:
- C3 (byte-identical artifact): fully closed — `AllJsonl` raw compare for `normal_day`/seed `12345`, baseline vs current, PASS. Correct artifact, determinism proven.
- C2 (supply-run-conflict, duplicate-reservation): closed — both fixtures go red with clear messages.
- C1 (jitter): meets the bar I set (synthetic fixture acceptable) — the rule fires on path 1.60m / net 0.00m. Residual, recorded not waived: the same rule did not flag the real twitch in `20260619_132902`, so the synthetic pass proves the logic but not the threshold-vs-real-twitch sensitivity. The green run on `20260619_151950` is therefore necessary but NOT sufficient proof that twitch is gone.

Consequence: Michael's human visual smoke is the BINDING acceptance gate for "no twitching," not a formality. If twitch is visible while the parser is green, that is a jitter-threshold tuning follow-up (parser only, no gameplay change).

Recommended (non-blocking) before merge: re-run the final parser against `20260619_132902`. If `jitter` now fires there, the detector catches the real phenomenon (full confidence); if not, the parser cannot gate twitch and the visual smoke stands alone.

Corroborating: agent-samples fell 2723 → 2157 at equal 379 samples — consistent with customers no longer stalling.

Reviewed By: Claude Opus — 2026-06-19.

## Validation Evidence

Implementation (Codex, 2026-06-19): source changes within allowed paths (`CrowdCoordinator`, `AgentManager`, `CustomerAgent`, `EmployeeAgent`, `assert_movement.py`, handoff; CharacterRig untouched). Fixes: practical arrival radii (Enter/Ordering/Waiting/Dining/Busing/Leave); distinct `mobile_entry/wait/pickup_*` reservations; presentation-only `pos_order_*`/`pos_service_*`; counter service via POS reservation not `_sim.Tickets`; walk-in single-door + `walkin_standoff_*`; active-target telemetry. Build PASS 0/0; self-test PASS 120/120, 10/10, 11/11. Clean smoke `20260619_151950` parser PASS 379 / 2157 / 0 failures. Smoke folder untracked — do not commit unless Michael wants artifacts in repo.

Corrections (Codex, 2026-06-19):
- C1 jitter fixture: red, exit 1, `{"code":"jitter","message":"cust_jitter jitter near lobby_wait_0: path 1.60m net 0.00m"}`.
- C2 supply-run: red, `{"code":"supply_run_conflict","message":"emp_b and emp_a share walk-in supply target (14.23,-8.5)"}`.
- C2 duplicate: red, `{"code":"duplicate_reserved_slot","message":"lobby_wait_0 reserved by cust_a and cust_b at 0.00s"}`.
- C3 byte-identical: canonical stream is `SimRunState.AllJsonl`; baseline via `git archive` from `9128fd7`; baseline vs current `AllJsonl` for `normal_day`/seed `12345` → `CANONICAL_EVENT_STREAM_BYTE_IDENTICAL: PASS`. Temp fixtures/extractor removed.

Pending: Michael human visual smoke; recommended final-parser re-run vs `20260619_132902`.

## Michael Approval

Specification Approved: `APPROVED 2026-06-19`
Material Decisions (D1–D3) Approved: `APPROVED 2026-06-19` (D1=B, D2 ratified, D3 N/A)
Human Visual Smoke: `PENDING` (binding twitch gate)
Merge Approved: `PENDING`

## Rollback

Working tree clean at Base SHA `9128fd7`. Safest rollback is a revert commit of the single follow-up feature commit, or a targeted restoring commit. No reset, rebase, or discard without Michael approval.

## Next Authorized Action

Michael performs the human visual smoke (binding gate for twitch + the counter/kiosk/post-food sequences). Recommended: Codex re-runs the final parser against `20260619_132902` and records whether `jitter` fires. If the smoke is clean, Michael grants merge approval. If twitch is visible, raise a parser-only jitter-threshold tuning follow-up — no gameplay source edits otherwise.
