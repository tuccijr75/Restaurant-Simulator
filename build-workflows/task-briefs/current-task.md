# Current Task

Task ID: RS-IM-001
Task Brief: `build-workflows/task-briefs/RS-IM-001.md`
Lane: Engine / Godot
Status: ready_for_review
Previous: RS-RM-001 — multi-day career mode (ready_for_review)
Verification: 120/120 self-test + 7/7 ingredient model + 11/11 career = PASS

## Pending (in-editor)
- CareerHook autoload registered (RS-RM-001); real ingredients auto-enable.
- F5 export is the canonical Godot export path. When real ingredients are active,
  `inventory_ledger.json` contains the per-ingredient ledger.

## Next Task
RS-IM-002 — ASC compatibility validation for the per-ingredient `inventory_ledger.json`;
keep the legacy bucket ledger only for real-model-off mode.
