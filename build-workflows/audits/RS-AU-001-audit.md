# Audit — RS-AU-001

## Task

RS-AU-001 — Post-change Repository Stabilization Audit

## Scope

Audit current `main` after major Godot/game/workflow changes. Confirm ignore-rule protection for generated Godot/cache paths before any later tracked-artifact housekeeping. No generated files were removed in this task.

## Baseline Compared

Baseline checkpoint: `dd6918b560df68086d5272c808d48022e873695c` — RS-VA-001 test pass.

Current state: `main` is 5 commits ahead of the baseline.

## Source Packets / Evidence Used

- control-pack/active/04_context.md
- control-pack/active/05_diagnostics.md
- build-workflows/episodes/RS-VA-001/receipt.json
- build-workflows/episodes/RS-3D-001/receipt.json
- build-workflows/episodes/RS-ST-001/receipt.json
- current task pointer
- workflow state files
- `.gitignore`
- Godot project and C# simulation files
- compare from baseline to current `main`

## Files Modified In This Task

- `.gitignore`
- `build-workflows/task-briefs/current-task.md`
- `build-workflows/state/state.json`

## Files Created In This Task

- `build-workflows/task-briefs/RS-AU-001.md`
- `build-workflows/audits/RS-AU-001-audit.md`
- `build-workflows/handoffs/RS-AU-001-handoff.md`
- `build-workflows/episodes/RS-AU-001/task.md`
- `build-workflows/episodes/RS-AU-001/trace.jsonl`
- `build-workflows/episodes/RS-AU-001/checks.jsonl`
- `build-workflows/episodes/RS-AU-001/audit.md`
- `build-workflows/episodes/RS-AU-001/handoff.md`
- `build-workflows/episodes/RS-AU-001/receipt.json`

## Files Deleted In This Task

None.

## Ignore Rule Status

`.gitignore` already contained Godot/generated path rules. RS-AU-001 added an explicit note that existing tracked generated files must be untracked later with a safe index-only operation, preserving local disk copies.

Relevant ignore coverage:

- `.godot/`
- `.import/`
- `game/.godot/`
- `game/bin/`
- `game/obj/`
- `game/.mono/`
- `restaurant-simulator-(4.3)/.godot/`
- `restaurant-simulator-(4.3)/bin/`
- `restaurant-simulator-(4.3)/obj/`
- `restaurant-simulator-(4.3)/.mono/`

## Changed File Classification

### Keep as source / implementation candidates

- `game/project.godot`
- `game/scenes/Main.tscn`
- `game/scripts/Main.cs`
- `game/scripts/MainDashboard.cs`
- `game/scripts/sim/Exports.cs`
- `game/scripts/sim/SelfTest.cs`
- `game/scripts/sim/SimConfig.cs`
- `game/scripts/sim/SimRunState.cs`
- `game/scripts/ui/*.cs`
- `game/scripts/world/*.cs`
- `tools/engine-selftest/**`
- `docs/07_3D_WORLD_AND_CAMERAS.md`
- `README-RS-3D-001.md`

### Keep as workflow evidence / review artifacts

- `build-workflows/task-briefs/RS-3D-001.md`
- `build-workflows/task-briefs/RS-ST-001.md`
- `build-workflows/task-briefs/RS-FE-001.md`
- `build-workflows/task-briefs/RS-HQ-001.md`
- `build-workflows/task-briefs/RS-GP-001.md`
- `build-workflows/task-briefs/RS-VS-001.md`
- `build-workflows/task-briefs/RS-CF-001.md`
- `build-workflows/task-briefs/RS-IN-001.md`
- matching audits, handoffs, episode task/check/trace/receipt files

### Review for later tracked-artifact housekeeping

- `game/.godot/editor/**`
- `game/.godot/mono/temp/**`
- `game/.godot/shader_cache/**`
- `game/.godot/uid_cache.bin`
- generated `.dll` files under `.godot/mono/temp`
- generated `.pdb` files under `.godot/mono/temp`
- generated `.cache` files under `.godot/**`

These files should not be removed from disk. Later cleanup should only stop tracking them after ignore protection is confirmed.

### Workflow-state conflict

Two state files exist:

- authoritative: `build-workflows/state/state.json`
- duplicate/legacy: `build-workflows/state.json`

Per context doctrine, workflow state belongs under `build-workflows/state/state.json`, current-task pointer, and episode receipts. `build-workflows/state.json` should be treated as duplicate/legacy until removed or archived by a later scoped task.

### Current task conflict observed

Before RS-AU-001:

- `build-workflows/task-briefs/current-task.md` said no task was in flight and six-task integration completed.
- `build-workflows/state/state.json` still pointed at RS-ST-001 / RS-3D-001 review state.
- `build-workflows/state.json` listed the six-task integration as complete.

RS-AU-001 set the active task to this audit and preserved the review-lane statuses for RS-ST-001 and RS-3D-001.

## Contract / Security Findings

### Passed / acceptable

- Python engine was not changed in the major post-VA diff.
- Python side remains previously validated by RS-VA-001.
- C# self-test checks deterministic replay, inventory ledger hash, deprecated `item.sold` absence, lifecycle ordering, monotonic sequence, and ticket reconciliation.
- Search found `item.sold` only in doctrine/test/audit/self-test contexts, not as an active intended event emission.
- Broad search found no obvious employee-scoring or real-data marker phrases.
- New C# staffing ledger uses synthetic worker references such as `crew_shift_pool`, `manager_shift`, and `crew_shift_calloff`.

### Needs review

- C# `InspectionScore` and `Csat` are store/output quality metrics, not employee scoring, but they must remain framed as operational simulation metrics only.
- Godot editor/cache/temp files are tracked despite ignore protection now existing.
- Several episode traces/checks use `.md` rather than the more structured `.jsonl` convention.
- Some task receipts are shallow compared to the earlier RS-SP/SC/EN/VA receipt standard.
- Local Godot editor run remains pending in RS-3D-001 and RS-ST-001 receipts.

## Recommended Next Task

RS-GC-001 — Repository generated-artifact tracking cleanup.

Rules for RS-GC-001:

1. Do not delete local generated files.
2. Use index-only untracking for generated/editor/cache/build artifacts now protected by `.gitignore`.
3. Keep source files, scripts, scenes, docs, tools, and workflow receipts.
4. Re-run Python tests and Godot self-test after cleanup.
5. Preserve authoritative workflow state at `build-workflows/state/state.json`.
6. Decide separately whether `build-workflows/state.json` should be removed as duplicate legacy state.

## Status

Ready for review.
