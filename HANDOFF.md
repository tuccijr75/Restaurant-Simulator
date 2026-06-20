# Restaurant Simulator Handoff

## Control

Project: Restaurant Simulator
Repository: `E:\GitHub Projects\Restaurant Simulator\Restaurant Simulator`
Branch: `main`
Base SHA: `2d07332b88cf823d3d6fbbca9af9daadab6fc5d5`
Current HEAD: `408b471d4dea4097b57a3fc30176d67b20a8def7`
Task ID: `layout-interaction-functional-pass`
Task Name: Seating, object collision, POS location, employee activity, kitchen/office layout, lobby furniture, walk-in scale, model scale
Status: `CLAUDE_REVIEW_CLEARED_AWAITING_OV2_DSTALL_MERGE`
Handoff Version: 10.1
Last Updated: 2026-06-20
Updated By: Codex

## Workflow Contract

HANDOFF.md is the permanent, revolving coordination channel between Claude Opus and Codex. It describes only the current task state; stale detail is removed each update; Git history preserves prior versions.

- Michael owns product intent, material decisions, merge/release approval, final acceptance.
- Claude Opus owns task definition, architecture/risk review, acceptance criteria, packet/implementation review.
- Codex owns repository inspection, source implementation, test execution, implementation evidence.
- Codex must not edit source until Claude approves the packet and Michael approves the spec.
- Codex must not author or sign the "Claude Review" section; if a fresh review is needed, leave it `PENDING_CLAUDE_REVIEW`. The "Updated By" field must name the actor who actually produced the update.
- Repository files are the source of truth; model memory is secondary.
- Neither model may merge, discard, reset, rebase, or delete work without Michael approval.
- Verification is evidence-based: Codex summaries are claims, not proof. A check passes only when its command completes with verifiable output tied to the current commit SHA. A green machine check that coexists with a defect Michael can see by eye means the check is under-specified and must be tightened. A prior `APPROVED_*` verdict is void once HEAD or a required check changes.

Prior task `movement-runtime-authority-followup` is CLOSED/ACCEPTED at Base SHA. Do not reopen unless a new direct symptom appears.

## Objective

In runtime smoke, layout and visible interactions read correctly: customers sit aligned in chairs/booths; characters route around furniture/equipment/walls; POS at right-end registers; employees visibly work; office/fryer/walk-in/break-room/drive-thru match Michael's direction; staff/customer apparent heights match. All changes presentation/layout only — deterministic sim outputs unchanged.

## OV2 Result (round 1) + Correction Status

Michael's binding OV2 at `5bc0e66` found four defects (D1–D4). Codex implemented the scoped correction pass at HEAD `5781877`; machine verification is green. D2 and D4 are now machine-proven; D1's assertion is tightened and exercised; D3 has no machine evidence and rests on OV2. OV2 re-smoke is required and binding.

## Corrections Packet (D1–D4) — implementation + what OV2 must confirm

D1 — Customer seating pose.
Implemented: seat metadata carries visual seated offsets; customers apply the seated offset on arrival; telemetry records seated yaw, target yaw, seat kind, seated offset. `assert_movement.py` now fails on wrong seated yaw or missing/wrong seated offset for booth/chair seats; exercised on 107 post-arrival Dining samples (parser PASS).
Machine limit: the assertion confirms the offset is applied, matches seat metadata, and yaw is square — it cannot vouch that the offset VALUE lands the body on the cushion. That correctness is OV2's call.
OV2 must confirm: seated customer rests in the seat (not perched/floating), back to backrest, facing the table; AND the dark block behind the customer in Image 1 is the intended booth divider, not a stray object (Codex did not explicitly close this).

D2 — Expo station aisle. PROVEN. `layout_metrics.json` `expo_counter_gap_m=1.0499…` ≥ locked 1.0m; position-only move; smoke re-ran with no new stall. OV2: confirm the aisle reads open.

D3 — Menu board front/back. Implemented as explicit front/back quads using the same `lobby_menu.png` material with a thin backing board. NO machine evidence — the dedicated screenshot runner failed (gameplay camera owned the viewport); prior menu render is from the old box mesh and is stale. OV2 is the SOLE confirmation: full image fits the front face (no seam/split/stretch); identical image present on the back.

D4 — Drive-thru window alignment. PROVEN. `layout_metrics.json` center/size errors ~`3.8e-7m`; station opening aligned to the west-wall aperture; drive-thru work anchor moved with the station; crew slot reachable. OV2: confirm proportions read correctly in the wall.

## Required Before Merge

R-DET — CLOSED. Explicit `AllJsonl` / `event_stream.jsonl` hash for `normal_day` seed `12345` is `05464c886b33616332885744d215ee7fc7ef8ec0f18384c67b05db17e83d18b6`, matching the base/cardinal invariant. Evidence source: `test-artifacts\compat-bundles\current-head-normal-day-csharp\hashes.json`; command rerun at current HEAD `408b471`: `dotnet run --project tools\engine-selftest\harness.csproj`, exit 0, RESULT PASS.

## Decisions Reserved for Michael

- D2-AISLE — RESOLVED: ≥1.0m (met at 1.05m).
- D3-BACK — RESOLVED: identical texture on the back face.
- D-STALL — `cust_ord_000002` mobile-entry stall: non-reproduced, recorded as non-reproduced not resolved. Options: accept-as-is, 2–3 confirmation smokes, or split a `mobile-entry-stall-watch` follow-up. Status: OPEN.

## Claude Review

Verdict: `APPROVED_TO_CONTINUE` (Claude review of the D1–D4 implementation clears; advances to R-DET + Michael OV2 re-smoke + D-STALL + merge). Fresh review of HEAD `5781877`; no prior verdict inherited.

The gate is green at HEAD with concrete commands/exit codes/report paths/SHA. D2 (1.05m gap) and D4 (sub-micron wall alignment) are machine-proven via `layout_metrics.json` — the recorded-metric approach is exactly right. D1's assertion is now real and exercised (107 Dining samples), closing the "green while the pose is wrong" gap for yaw and offset-consistency; the residual — whether the offset value lands the body on the cushion — is correctly left to OV2. OV1 office round-trip still passes at HEAD (`emp_28` depart 41 / return 71). Codex left the Claude Review section PENDING as required.

Two honest gaps, neither a code-rework blocker: (1) R-DET — the determinism hash is not transcribed at this HEAD (above); record it before merge. (2) D3 has no machine/screenshot evidence; it rests entirely on OV2 this round, and the dark-block confirmation for D1 was not closed — both go on Michael's re-smoke checklist.

Reviewed By: Claude Opus — 2026-06-20.

## Codex Implementation Summary (source state at `5781877`; handoff evidence state at `408b471`)

Changed files (allowed paths): `WorldBuilder.cs`, `CrowdCoordinator.cs`, `CustomerAgent.cs`, `CharacterRig.cs`, `assert_movement.py`, `movement_smoke_runner.gd`, `HANDOFF.md`. New committed evidence: `test-artifacts/movement-smoke/20260620_083601/` (screenshots, `layout_metrics.json`, `movement_samples.jsonl`, `movement_summary.json`, `manager_office_roundtrip.json`).

## Verification Gate (source run at HEAD `57818772f8b73a3f18eb66f5c5ae3b9b73bd27e7`; R-DET rerun at handoff HEAD `408b471d4dea4097b57a3fc30176d67b20a8def7`)

Build: PASSED · Unit: PASSED · Integration: PASSED · Static Analysis: PASSED (py_compile of parser only) · Schema: PASSED · Deterministic Replay: PASSED (`normal_day` seed `12345` `AllJsonl`/`event_stream.jsonl` hash `05464c886b33616332885744d215ee7fc7ef8ec0f18384c67b05db17e83d18b6`) · Save/Load Compat: PASSED (14 OK) · Runtime Smoke: PASSED · Packaging: PASSED · Generated File Check: PASSED.

Gate Result @ `5781877`: PASSED. Evidence:
- Build: `dotnet build game\RestaurantSimulator.csproj --nologo`; exit 0; 0/0; SHA `5781877`.
- Static analysis: `python -m py_compile …\assert_movement.py`; exit 0; SHA `5781877`.
- Runtime parser: `python …\assert_movement.py …\20260620_083601`; exit 0; PASS 323 / 1959 / 0; SHA `5781877`.
- Runtime smoke: committed `…\20260620_083601\`; `manager_office_roundtrip.json` PASS (`emp_28` depart 41/max 2.036m/return 71); `layout_metrics.json` status ok (expo gap 1.05m; drive-thru align ~3.8e-7m); SHA `5781877`.
- Unit/integration/schema: `python -m unittest discover -s tests -v`; exit 0; 35 OK, 14 skipped (no bundle in broad run); SHA `5781877`.
- Save/load compat: `RS_COMPAT_BUNDLES=<…csharp>; python -m unittest tests.test_compatibility -v`; exit 0; 14 OK; SHA `5781877`.
- Engine self-test/deterministic replay: `dotnet run --project tools\engine-selftest\harness.csproj`; exit 0; PASS 120/120, 10/10, 11/11; deterministic replay checks passed; `normal_day` seed `12345` `AllJsonl`/`event_stream.jsonl` hash `05464c886b33616332885744d215ee7fc7ef8ec0f18384c67b05db17e83d18b6`; command rerun at SHA `408b471`.
- Packaging: `dotnet publish … -c Debug --nologo --no-restore -o …\RestaurantSimulator-debug`; exit 0; SHA `5781877`.
- Generated file check: `git status --short --branch --ignored`; exit 0; clean tree post-commit; ignored build/cache only; SHA `5781877`.

### Gate Rule
Task may not move to READY_FOR_CLAUDE_REVIEW unless all required checks pass. Task may not move to APPROVED_FOR_MERGE unless: gate passes at HEAD; Claude completes review; unresolved blocking risks are zero; the binding OV2 visual smoke is clean; Michael approves the merge. Do not weaken/skip/delete failing tests to obtain a pass; if a check cannot run, mark BLOCKED with the reason.

## Known Risks (residual)

- D1: machine assertion guards yaw + offset-consistency, not absolute offset correctness — OV2 confirms the body lands on the cushion; dark-block confirmation still open.
- D3: no machine evidence (screenshot capture failed); OV2 is the sole confirmation of front/back image.
- Mobile-entry stall (`cust_ord_000002`) — non-reproduced, not resolved (D-STALL).

## Rollback

Targeted restoring commit or revert of implementation commit(s) up to `5781877`. No reset/rebase/discard/delete without Michael approval.

## Michael Approval

Specification Approved: `APPROVED 2026-06-19` (D1–D4 within approved scope)
Material Decisions: `M1–M4 APPROVED`; height `RATIFIED 1.72m`; `D2-AISLE` RESOLVED (≥1.0m); `D3-BACK` RESOLVED (identical texture); `D-STALL` OPEN
Human Visual Smoke (OV2): `PENDING RE-SMOKE` — confirm D1 seated pose + dark block, D2 aisle, D3 front+back image, D4 drive-thru proportions
Merge Approved: `PENDING` (awaits OV2 re-smoke clean, D-STALL decision, Michael approval)

## Next Authorized Action

Michael re-runs OV2 on the four views (D1 seated pose + dark block, D2 aisle, D3 front/back menu image, D4 drive-thru proportions) and decides D-STALL. On OV2 clean + D-STALL settled, Michael grants merge. No further source edits unless OV2 surfaces a defect, which would scope a new corrections item.
