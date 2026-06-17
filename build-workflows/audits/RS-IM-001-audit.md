# RS-IM-001 Audit

## Verification (sandbox, .NET 8.0.128)
- Full gate: 120/120 self-test + 7/7 ingredient + 11/11 career = RESULT: PASS.
- Replay neutrality: event-stream hash identical with EnableRealIngredients on vs off
  (the model is pure accounting over the existing item.taken stream).
- Per-ingredient reconciliation: opening + received/prepped - consumed - waste = closing
  holds for all 34 ingredients (asserted; no "reconciles":false).
- Realism: normal_day waste $46.54 — fries_cooked (7-min hold) ~7% tail, cooked proteins
  ~6%, produce a small prepped-lot tail; zero waste on buns/packaging/frozen/cheese/sauces.
  Cooking depletes raw frozen (frozen_beef_patty 1200->684 as 516 patties cooked).

## Decisions
- Additive layer (not a rewrite of the throughput core) to avoid the abandonment
  regressions seen when the bucket prep/pan sizing was tuned directly; keeps the
  stable ~2% abandonment baseline and all existing gates.
- Opt-in flag keeps determinism for legacy callers; CareerHook enables it in Godot.
- Per-category JIT batch sizes (cooked 6, fries 4, produce 40) are
  operator_calibration_required; fries aligned to the real fryer batch of 4.

## Blockers / honesty
- In-editor smoke test pending (no Godot in build env).
- F5 export is the canonical Godot export path. If it fails in editor, capture
  the Godot console trace for the active `Main` export writer.
- When real ingredients are active, `inventory_ledger.json` is the per-ingredient
  ledger; the separate `ingredient_ledger.json` file is no longer emitted.

## Rollback
Safe/additive. Set EnableRealIngredients=false (or remove Catalog) to fully revert to
the bucket model; new files are standalone.
