# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Task ID: movement-avoidance-authority
Task Name: Movement, Reservation, and Telemetry Authority Layer
Status: AWAITING_CODEX_SYNC
Handoff Version: 1.0
Specification Version: 1.0
Repository: PENDING_CODEX_VERIFICATION
Branch: PENDING_CODEX_VERIFICATION
Base SHA: PENDING_CODEX_VERIFICATION
Current SHA: PENDING_CODEX_VERIFICATION
Last Updated: 2026-06-19
Updated By: Claude Opus

## Role Authority

### Michael
* Product intent; material decisions; customer-visible decisions
* Contract and schema approval; architecture approval where required
* Merge and release approval

### Claude Opus
* Task specification; architecture and contract analysis
* Acceptance criteria; edge cases and risk analysis
* Read-only implementation review; rework specification

### Codex
* Repository inspection; source implementation
* Build and test execution; validation evidence
* Commits and implementation receipts

### Shared Rules
* Repository files outrank model recollection.
* Codex is the sole implementation writer.
* Claude does not produce an alternate implementation.
* Neither model may approve a merge.
* Material uncertainty must be escalated to Michael.
* Existing work must not be discarded during migration.

## Source of Truth

* Original task spec: `movement-avoidance-authority` (cites `starting_sha 4a29f4bc03df89aae98b28c179145feb76342b35`, clean worktree). VERIFY — see Assumption A4.
* Resolved Gates A–E (Michael-approved slot model, thresholds, telemetry format/location/commit policy, ASC scope, layout latitude).
* Project doctrine: deterministic pure-C# sim core + Godot presentation layer; the sim event stream is never written from the presentation layer; synthetic-only; deterministic seeded replay. Doctrine file path: `PENDING_CODEX_VERIFICATION`.
* Engine self-test / determinism gate: `tools/engine-selftest` (project + invocation). `PENDING_CODEX_VERIFICATION`.
* ASC compatibility profile: `profiles/compatibility.json`, `tests/test_compatibility.py`. `PENDING_CODEX_VERIFICATION`.
* Governance: control-pack packets and build-workflows directories. `PENDING_CODEX_VERIFICATION`.
* Allowed source files under `game/scripts/world/` and smoke harness under `test-artifacts/`. See Allowed Paths.

## Objective

Over a Godot movement smoke run, machine-checked telemetry shows zero reservation duplications and zero overlap, stuck, lingering, or outside-with-food violations above the Gate B thresholds, while the engine self-test deterministic replay output remains byte-identical to the Base SHA.

## Current Known State

* Before this task: the simulator's existing crowd behavior is committed at HEAD; avoidance relies on at least one push-apart mechanism (`AgentManager.Separate`, O(n²)) plus per-agent steering, and on queue counting in `AgentManager`. `PENDING_CODEX_VERIFICATION` (Assumption A2).
* Recently delivered/adjacent work (roster + employee stats + staff UI + dine-in tray-bussing) and an in-progress parking-arrival feature touch the same `world/` files. Their merge status relative to the Base SHA is unknown — `PENDING_CODEX_VERIFICATION` (Assumptions A1, A5).
* Believed completed for THIS task: none. No `CrowdCoordinator`, reservation model, telemetry, or movement harness exists yet (clean worktree).
* In progress for THIS task: none.
* Remaining: define/approve slot model; implement coordinator + reservations; route all customer/employee destinations through it; add telemetry + assertion harness; retire redundant avoidance last; run validation.

## Requirements

* `CrowdCoordinator` owns destination reservations and line advancement; no two active agents reserve or occupy the same slot.
* Customers acquire counter/kiosk/wait/pickup slots (and dining-table + tray-return destinations — see Decision D2) through the coordinator.
* Employees acquire station/break/walk-in-door slots through the coordinator.
* Add runtime movement telemetry + machine assertions before relying on screenshots.
* Remove or simplify redundant local collision rules that conflict with coordinator authority — only after telemetry is green.
* Preserve deterministic sim outputs (byte-identical self-test); coordinator is read-only toward `SimRunState`.

## Acceptance Criteria

- [ ] Build passes with zero errors.
- [ ] Engine self-test deterministic replay output is byte-identical to Base SHA.
- [ ] Telemetry emits, per agent per sample: phase, target, reserved slot id, distance-to-target, stuck-seconds, pairwise overlap, plus linked `ticket_id`/`ticket_complete`; with a per-sample slot-occupancy snapshot.
- [ ] No duplicate active `ReservedBy`/`OccupiedBy` for any slot at any sample (immediate-fail invariant).
- [ ] No overlap < 0.45 m sustained > 2.0 s; no stuck (< 0.10 m moved while target > 0.75 m) > 4.0 s.
- [ ] No outside-with-food > 5.0 s; ordering ≤ 12 s counter / 18 s kiosk; pickup ≤ 20 s when ticket complete.
- [ ] Waiting > 180 s flagged unless the linked sim ticket is still incomplete.
- [ ] Automated parser produces machine-readable pass/fail over the above.
- [ ] Human visual smoke confirms queue behavior is believable.

## Allowed Paths

* `game/scripts/world/CrowdCoordinator.cs` (new)
* `game/scripts/world/AgentManager.cs`
* `game/scripts/world/CustomerAgent.cs`
* `game/scripts/world/EmployeeAgent.cs`
* `game/scripts/world/CharacterRig.cs`
* `test-artifacts/visual-smoke/visual_smoke_runner.gd`
* `test-artifacts/movement-smoke/` (new harness + parser) — directory existence `PENDING_CODEX_VERIFICATION`

## Prohibited Changes

* Project doctrine without Michael approval.
* Schemas, contracts, or exports without Michael approval.
* Ingredient/catalog systems; deterministic sim logic.
* Unrelated systems; production integrations.
* Generated caches and editor artifacts.
* Layout changes (equipment, walls/partitions/counters, navmesh geometry, physical access routes, aesthetics) without separate approval (Gate E).
* Destructive Git operations (reset/rebase/discard) without approval.
* Silent architecture changes; scope expansion beyond this task.

## Non-Goals

* No event-schema, export, ledger, receipt, or ASC-contract changes; movement telemetry stays diagnostic and isolated from all of them.
* No write-back to `SimRunState`; no influence on deterministic order production.
* No layout changes by default (debug-only/invisible slot positions and telemetry markers are allowed).
* No parking-arrival feature work under this task.

## Assumptions

* A1 — Live `CustomerAgent` phases include Dining and Busing (tray return). Confidence: Medium. Verify: inspect `game/scripts/world/CustomerAgent.cs` at Base SHA. If false: Decision D2 may be moot or the baseline differs.
* A2 — `AgentManager.Separate` (push-apart) and `CountQueued` are the redundant mechanisms to retire. Confidence: Medium. Verify: inspect `AgentManager.cs`. If false: removal targets change.
* A3 — Engine self-test lives at `tools/engine-selftest` and is byte-stable per (config, scenario, seed). Confidence: High (doctrine). Verify: run it. If false: byte-identical gate needs a different harness path.
* A4 — Base SHA `4a29f4b` has a genuinely clean worktree. Confidence: Low/Conflicting (recent delivered `world/` work). Verify: `git status` / `git log`. If false: merge collisions; reconcile before branching.
* A5 — Delivered roster/stats/staff-UI/tray work and in-progress parking are either merged at Base or on a separate branch. Confidence: Unknown. Verify: Codex locates. If unmerged: this task collides on `CustomerAgent`, `AgentManager`, `CharacterRig`, `WorldLayout`.

## Decisions Requiring Michael

* D1 — Baseline reconciliation. Options: (a) Base includes delivered tray/roster work; (b) Base excludes it (separate branch); (c) merge/abandon parking first. Recommendation: confirm Base includes the tray/roster work and explicitly pause parking under Gate E. Consequence: wrong baseline → incomplete slot coverage or merge conflicts. Status: OPEN (blocking).
* D2 — Add dining table/seat and tray-return slot types to the Gate A model. Recommendation: yes — tables and the tray-return counter are top clipping/lingering spots and core to the objective. Consequence: omission leaves the worst offenders un-reserved. Status: OPEN.
* D3 — Kiosk vs counter. Options: add a counter/kiosk attribute at spawn, or collapse the two ordering thresholds. Recommendation: add the attribute (keeps Gates A/B as written). Consequence: otherwise the kiosk threshold is unreachable. Status: OPEN.
* D4 — `EmployeeStation` sub-slot capacity sized to roster max concurrency (expo: crew + lead; office: MOD + patrolling managers); patrol/greet roaming exempt from stuck/overlap assertions. Recommendation: size from roster; exempt roaming. Consequence: under-sizing reproduces the pile-ups being fixed. Status: OPEN.
* D5 — Unspecified parameters: `ExpiresAfterSeconds` default; telemetry sampling cadence; outside-storefront threshold line; `carrying_food` semantics (bag counts; cleared when tray is bussed); employee `agent_id` scheme. Recommendation: Codex proposes; Michael ratifies. Status: OPEN.

## Claude Specification

### Intended Behavior
All customer and employee destinations are assigned by `CrowdCoordinator` as reservations: an agent requests a slot, reserves it, occupies it on arrival within radius, and releases on phase change, completion, abandonment, or despawn. Queue advancement is coordinator-driven. Movement telemetry is recorded for machine assertion. Customer-visible queueing becomes orderly.

### Architecture and Boundaries
`CrowdCoordinator` is presentation-layer only. It may read sim ticket state to gate departures; it must never write `SimRunState` or influence deterministic order production. `CharacterRig` is reduced to local steering. `AgentManager` loses redundant queue counting and `Separate` — only after telemetry is green. Telemetry is a diagnostic artifact isolated from event/export/ledger/receipt/ASC contracts.

### Contracts and Invariants
* Single-occupancy: no slot has two active `ReservedBy`/`OccupiedBy` (immediate-fail).
* Determinism: engine self-test output byte-identical to Base SHA.
* Read-only sim boundary: no `world/`-layer write to `SimRunState`.
* Telemetry isolation: movement output never enters the deterministic stream, exports, ledgers, receipts, or ASC contract.

### Edge Cases
Abandonment releases reservation; despawn without release reclaimed via `ExpiresAfterSeconds`; Dining/Busing destinations need slots (D2); expo/office multi-occupancy (D4); patrol/greet roaming exempt from stuck/overlap; to-go bag vs dine-in tray for `carrying_food` (D5); legitimately-incomplete tickets exempt from the waiting violation.

### Tests Required
* Build (`dotnet build`, zero errors).
* Engine self-test — byte-identical deterministic replay vs Base SHA.
* Godot movement smoke with telemetry enabled.
* Automated parser/assertions over telemetry (Gate B + reservation-uniqueness).
* Manual visual smoke screenshots for human review.

### Known Risks
* Removing `Separate`/`CharacterRig` avoidance before telemetry is green can regress movement — sequence: coordinator + telemetry + green assertions first; remove one mechanism at a time, re-validating after each.
* Baseline mismatch → merge collisions on `world/` files (D1).
* Slot-coverage gaps (Dining/Busing) leave the worst clipping spots un-reserved (D2).
* Telemetry artifact bloat if sampling cadence is unbounded (D5).
* O(n²) cost if `Separate` is retained alongside the coordinator.

## Codex Sync

### Verified Repository State
Repository:
Branch:
HEAD SHA:
Upstream:
Working Tree:

### Current Task Reconciliation

### Existing Changed Files

### Differences From Claude Handoff

### Codex Questions or Blockers

## Codex Implementation

### Starting State

### Implementation Plan

### Changed Files

### Scope Deviations

### Remaining Work

## Validation Evidence

### Commands Run

### Test Results

### Acceptance Criteria Results

### Failed or Missing Checks

### Runtime Evidence

## Claude Review

Verdict: PENDING

Allowed verdicts: APPROVED_TO_CONTINUE, APPROVED_WITH_CORRECTIONS, REWORK_REQUIRED, HUMAN_DECISION_REQUIRED

### Findings

### Required Corrections

### Scope Compliance

### Risk Assessment

## Michael Approval

Specification Approved: PENDING
Material Decisions Approved: PENDING
Merge Approved: PENDING

## Rollback

Working tree is believed clean at Base SHA (`PENDING_CODEX_VERIFICATION`, Assumption A4). Safest rollback is a new revert commit of the single feature commit/branch, or a targeted follow-up commit restoring prior behavior. Do not reset, rebase, or discard without explicit approval. Exact Git commands: `PENDING_CODEX_VERIFICATION`.

## Next Authorized Action

Codex must reconcile this handoff with the actual repository and current task. Codex must not continue implementation until synchronization is complete and the status is reviewed.
