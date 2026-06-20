# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Current HEAD: `5bc0e6634e6cb6828fee63ee40e6f3cb1cc23dc7`
Task ID: `layout-interaction-functional-pass`
Task Name: Seating, object collision, POS location, employee activity, kitchen/office layout, lobby furniture, walk-in scale, model scale
Status: `REWORK_REQUIRED_OV2_VISUAL_DEFECTS`
Handoff Version: 8.1
Last Updated: 2026-06-20
Updated By: Claude Opus

## Workflow Contract

HANDOFF.md is the permanent, revolving coordination channel between Claude Opus and Codex. It describes only the current task state; stale detail is removed each update; Git history preserves prior versions.

- Michael owns product intent, material decisions, merge/release approval, final acceptance.
- Claude Opus owns task definition, architecture/risk review, acceptance criteria, packet/implementation review.
- Codex owns repository inspection, source implementation, test execution, implementation evidence.
- Codex must not edit source until Claude approves the packet and Michael approves the spec.
- Codex must not author or sign the "Claude Review" section; if a fresh review is needed, leave it `PENDING_CLAUDE_REVIEW`.
- Repository files are the source of truth; model memory is secondary.
- Neither model may merge, discard, reset, rebase, or delete work without Michael approval.
- Verification is evidence-based: Codex summaries are claims, not proof. A check passes only when its command completes with verifiable output tied to the current commit SHA. A green machine check that coexists with a defect Michael can see by eye means the check is under-specified and must be tightened. A prior `APPROVED_*` verdict is void once HEAD or a required check changes.

Prior task `movement-runtime-authority-followup` is CLOSED/ACCEPTED at Base SHA. Do not reopen unless a new direct symptom appears.

## Objective

In runtime smoke, layout and visible interactions read correctly: customers sit aligned in chairs/booths; characters route around furniture/equipment/walls; POS at right-end registers; employees visibly work; office/fryer/walk-in/break-room/drive-thru match Michael's direction; staff/customer apparent heights match. All changes presentation/layout only — deterministic sim outputs unchanged.

## OV2 Result

Michael ran the binding visual smoke at HEAD `5bc0e66` and found FOUR defects (D1–D4 below). The machine gate is green and the architecture review is clear, but OV2 is the binding merge gate and it FAILED. Task returns to Codex for a scoped visual-correction pass. This is corrections, not redesign.

## Corrections Packet (D1–D4) — allowed paths only

D1 — Customer seating pose (booth). Symptom: seated customer perched high/forward on the bench rather than settled onto the cushion with back to the backrest; appears too tall in the booth; possible yaw off.
Acceptance:
- Seated character origin rests at the bench cushion top (seat contact), not floating above it; back toward the backrest; seated yaw square to the table edge (±small tol).
- Verify both booth and non-booth chair seats.
- Tighten the seat assertion in `assert_movement.py`: add a seated-height check (character base within tol of the seat-cushion height per seat metadata) and a seated-yaw check (face the table normal ± tol), so the assertion cannot pass while the pose is visibly wrong. The current seat-reach check validates slot arrival only — that is the gap that let this through green.
- Confirm the dark block behind the seated customer in Image 1 is the intended booth divider and not a stray object/proxy.
Files: `CustomerAgent.cs`, `CrowdCoordinator.cs` (seat metadata: seated height/yaw targets), `assert_movement.py`, `WorldBuilder.cs` (booth geometry if needed).

D2 — Expo station too close to the front counter. Symptom: insufficient aisle between the expo/assembly station and the front service counter.
Acceptance:
- Open the clearance to a real walkable aisle (target: ≥1.0m clear — RESOLVED by Michael).
- Position-only move; station KEYS stay stable.
- Assert the AABB gap between the expo-station proxy and the counter proxy ≥ target; crew serve/approach slots remain reachable; movement smoke re-runs with no new stall from the move.
Files: `WorldBuilder.cs` (expo station position), `CrowdCoordinator.cs` (slots if anchored), `assert_movement.py` (gap assertion).

D3 — Menu board texture mapping. Symptom: the single menu image should fit the front face as one image and the same image should appear on the back face.
Acceptance:
- Full `lobby_menu.png` mapped to the entire FRONT face, correct aspect, no seam/split/stretch/tile.
- The SAME image mapped to the BACK face as an IDENTICAL texture (RESOLVED by Michael) — back reads mirrored when viewed from behind, accepted.
- Presentation-only; no sim coupling.
- Extend the R4 menu-render evidence to capture BOTH front and back faces showing the full image.
Files: `WorldBuilder.cs` (menu board mesh material/UV).

D4 — Drive-thru window station alignment. Symptom: the drive-thru window object (which has its own window opening) sits in the floor area instead of registered to the building's drive-thru wall aperture.
Acceptance:
- Position and proportion the station so its opening is flush and centered in the building's drive-thru wall opening; frame fits the aperture (no overlap/gap).
- Service side faces the drive-thru lane; crew side faces the kitchen.
- Drive-thru serve slot(s) remain reachable and aligned to the window after the move.
- Read the wall opening's coordinates/size from `WorldBuilder` and align the station to them (building opening is the reference); assert the station-opening AABB aligns with the wall opening within tol; crew drive-thru slot reachable in smoke.
Files: `WorldBuilder.cs` (station position/scale + align to wall opening), `CrowdCoordinator.cs` (drive-thru slot).

## Decisions Reserved for Michael

- D2-AISLE — RESOLVED by Michael: ≥1.0m clear walkable gap between expo station and front counter.
- D3-BACK — RESOLVED by Michael: identical texture on the back face (reads mirrored from behind, accepted).
- D-STALL — `cust_ord_000002` mobile-entry stall: non-reproduced in full rendered smoke, recorded as non-reproduced not resolved. Claude recommendation unchanged: accept-as-is, or run 2–3 confirmation smokes, or split a `mobile-entry-stall-watch` follow-up. Status: OPEN (can be folded into the D1–D4 re-verify smokes).

## Claude Review

Verdict: `REWORK_REQUIRED` (scoped to OV2 defects D1–D4). No prior verdict inherited.

The machine gate at `5bc0e66` is green and Codex addressed the canonical-replay auditability note (deterministic replay now via the canonical engine self-test; hash still byte-identical to base) and correctly left the Claude Review section as PENDING. Architecture and determinism are clear. The block is the binding visual gate: OV2 surfaced four concrete layout/pose/texture defects that no machine check caught. D1 in particular exposes a real lesson — the seat assertion passed while the seated pose is visibly wrong, so the assertion was validating arrival, not pose; it must be tightened (D1 acceptance) so green means right.

Path to merge: Codex implements D1–D4 → re-runs the full Verification Gate at the new HEAD with fresh evidence (including front+back menu render, drive-thru alignment, and seated-pose screenshots) → Michael re-runs OV2 on the four affected views → D-STALL decision → merge.

Reviewed By: Claude Opus — 2026-06-20.

## Codex Implementation Summary (state at `5bc0e66`)

Changed files (allowed paths): `WorldBuilder.cs`, `CrowdCoordinator.cs`, `CustomerAgent.cs`, `AgentManager.cs`, `CharacterRig.cs`, `assert_movement.py`, `movement_smoke_runner.gd`, `HANDOFF.md`. Committed evidence after `0d8c259`: `game/assets/lobby_menu.png.import`, `test-artifacts/movement-smoke/20260619_222253/`, `test-artifacts/menu-render-check/`, `test-artifacts/compat-bundles/current-head-normal-day-csharp/`, packaging metadata. Latest gate rerun evidence `test-artifacts/movement-smoke/20260619_232048/`. `CrowdCoordinator.cs.uid` left per repo convention.

## Verification Gate (last green run @ `5bc0e66` — MUST re-run at the D1–D4 HEAD)

Build: PASSED · Unit: PASSED · Integration: PASSED · Static Analysis: PASSED (py_compile of parser only) · Schema: PASSED · Deterministic Replay: PASSED (canonical self-test; hash `05464c88…e83d18b6`, byte-identical to base) · Save/Load Compat: PASSED (14 OK) · Runtime Smoke: PASSED (`20260619_232048`, 323/1958/0; OV1 round-trip PASS) · Packaging: PASSED · Generated File Check: PASSED.

Gate Result @ `5bc0e66`: PASSED. This result is VOID for merge purposes once D1–D4 move HEAD; the gate must be re-run and re-populated at the new commit SHA.

### Gate Rule
Task may not move to READY_FOR_CLAUDE_REVIEW unless all required checks pass. Task may not move to APPROVED_FOR_MERGE unless: gate passes at HEAD; Claude completes review; unresolved blocking risks are zero; the binding OV2 visual smoke is clean; Michael approves the merge. Do not weaken/skip/delete failing tests to obtain a pass; if a check cannot run, mark BLOCKED with the reason.

## Known Risks (residual)

- D1 seat assertion was passing while the pose was visibly wrong — until tightened, machine green does not guarantee correct seating.
- Mobile-entry stall (`cust_ord_000002`) — non-reproduced, not resolved (D-STALL).
- D2/D4 repositions could perturb nav — re-run smoke to confirm no new stall.
- D3/D4 are visual-correctness items — confirmed only by the re-run OV2.

## Rollback

Targeted restoring commit or revert of implementation commit(s) up to the D1–D4 HEAD. No reset/rebase/discard/delete without Michael approval.

## Michael Approval

Specification Approved: `APPROVED 2026-06-19` (D1–D4 corrections within approved scope)
Material Decisions: `M1–M4 APPROVED`; height `RATIFIED 1.72m`; `D2-AISLE` RESOLVED (≥1.0m); `D3-BACK` RESOLVED (identical texture); `D-STALL` OPEN
Human Visual Smoke (OV2): `FAILED 2026-06-20` — defects D1–D4; re-smoke required after corrections
Merge Approved: `PENDING` (blocked by OV2 defects)

## Next Authorized Action

Codex implements D1–D4 in allowed paths (D2-AISLE ≥1.0m and D3-BACK identical-texture are now locked), re-runs the full Verification Gate at the new HEAD with fresh evidence (front+back menu render, drive-thru alignment, seated-pose screenshots, movement smoke), and leaves Claude Review PENDING. Then Claude re-reviews, Michael re-runs OV2 on the four affected views and decides D-STALL, and grants merge only if all close.
