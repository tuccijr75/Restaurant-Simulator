# Audit — RS-3D-001

## Task

RS-3D-001 — Full Engine Audit, Determinism/Ledger Repair, and 3D World + CCTV + Character Layer

## Scope

Full audit of the current `game/` Godot 4.6 build (the evolved equipment-task engine, not the stale prototype), surgical fixes to every defect found, and a complete 3D restaurant (interior + exterior) with per-station CCTV coverage and animated AI characters, driven by — but never writing into — the deterministic simulation core.

## Source Packets Used

- control-pack/manifest.json
- control-pack/active/00_security/00_security.md
- control-pack/active/01_system/01_system.md
- control-pack/active/02_workflow/02_workflow.md
- control-pack/active/03_schema/03_schema.json
- control-pack/active/04_context/04_context.md
- control-pack/active/05_diagnostics/05_diagnostics.md
- docs/00–06 (locked product direction + realism calibration)
- game/config/realism_baseline.json

## Engine Findings and Fixes (all fixed in this task)

| ID | Finding | Fix |
|---|---|---|
| F-01 | Deterministic replay was impossible: `Step(d)` advanced the clock by frame-delta × TimeScale, so event timing/ordering depended on frame rate, violating the core replay doctrine. | Fixed 1-sim-second timestep. `Step` accumulates `real_delta × TimeScale` and runs whole ticks; per-tick logic is identical at any speed/frame rate. Verified: identical event-stream and ledger SHA-256 across replays, all 10 scenarios. |
| F-02 | No C#-side test gate. The Python unittest suite cannot exercise the Godot engine, so "deterministic_replay_valid" could never be proven. | Added `scripts/sim/SelfTest.cs` (F9 in-game) running (scenario, seed) twice headless and checking replay hashes, `item.taken → item.completed` ordering, ticket-after-items, sequence monotonicity, ledger reconciliation, deprecated `item.sold` absence, and envelope fields. Plus `tools/engine-selftest/` console harness (`dotnet run`, no Godot needed). |
| F-03 | `Raw=500` never replenished — prep starves mid-afternoon and every late-day order generates shortage waste. | 14:00 truck receiving (+420 raw, plus component restock), tracked in the ledger as received. |
| F-04 | No hooks for a visual layer. | `OrderCreatedEvt` / `TicketCompletedEvt` events and an `ExternallyDriven` flag; visuals subscribe and read state, never write. |
| F-05 | Sales (`CheckAmount`) were drawn independently of item prices, so revenue could not reconcile against the item ledger. | Order total = Σ item prices, with a 20% second-main attach keeping avg check in the docs/06 $10–12 band (measured $10.70–$10.81 across scenarios). `CheckAmount` removed. |
| F-06 | SOS was synthesized from counters, never measured from actual ticket completion times. | Measured rolling 30-min SOS per channel from completed tickets (`MeasuredSos`), all-day averages in the summary; synthetic projection retained only as the zero-sample fallback. |
| F-07 | `equipment_failure` disabled `fryer_main_2`/`soda_4` for the entire day, so `station.recovered` with `equipment_restored` was unreachable. | Bounded 11:00–13:30 outage window; recovery reason selected contextually (`equipment_restored` / `end_of_shift_queue_clear` / `queue_cleared`), replacing the hardcoded `queue_cleared` (F-12). |
| F-08 | Inventory clamped at 0 with no opening/consumed/wasted tracking — the doctrine ledger equation could not reconcile. | Per-component opening snapshot + received/consumed/wasted tracking; `opening + received − consumed − waste = closing` verified reconciling in every scenario run. |
| F-09 | Export wrote only the event stream + ad-hoc txt files, not the README 8-file output contract. | `scripts/sim/Exports.cs` builds `event_stream.jsonl`, `inventory_ledger.json`, `staffing_ledger.json`, `recommendation_validation_dataset.json`, `alert_validation_dataset.json`, `end_of_shift_summary.json` (with pass/fail gates), `run_receipt.json` (full provenance), `hashes.json` (SHA-256). Wired to the dashboard Export button and F5; written to `user://outputs/sim_{scenario}_{seed}/`. |
| F-10 | Quadratic string accumulation (`AllJsonl +=`, trace/task/item ledgers) — a full-day run ballooned from seconds to minutes/timeout, and unbounded ledger strings were re-rendered in UI labels every frame. | StringBuilder-backed ledgers behind the same public API; per-frame UI ledgers expose a capped recent view with `*Full` accessors for export. Full-day headless run: timeout → ~5 s. |
| F-11 | Throughput physically impossible: cook seconds were treated as serial per-item equipment occupancy. Fried mains alone demanded ~50 fryer work-hours against ~28 available; boards never cleared (DT SOS measured 11,572 s; 680/942 tickets completed) — failing docs/06 `active_board_clears_when_capacity_exceeds_arrivals`. | Batch-cycle model: per-item occupancy = cook_seconds / batch_size (pressure-fryer rack 8, flat-top zone 6, fry basket 4 — `operator_calibration_required`), assembly recalibrated to the baseline's practiced-crew 30 s. Result: every scenario's board clears; normal day 942/942 completed. |
| F-13 | Recommendation/alert validation datasets (README contract) were never produced. | 15-minute checkpoint rows with features + `expected_recommendation` (`prep_more`, `shift_staff`, `address_equipment_issue`, `check_temperature`, `change_sanitizer`, `monitor_queue`, `none`) and alert expectation labels. |

## Repository Findings (flagged, not changed without approval)

- **F-14** `restaurant-simulator-(4.3)/` is a stale divergent prototype (cumulative station load, schema-invalid payloads, no item lifecycle). Recommend deletion; the parenthesised folder name also breaks unquoted tooling.
- **F-15** README/pyproject document `python -m restaurant_simulator.cli` and `tests/`, but no `restaurant_simulator/` package or tests are present in the uploaded tree; RS-SC-001 artifacts reference those paths. Either restore the package or update README/pyproject. (RS-SC-001's claim that the Godot state emits `item.taken`/`item.completed` is true of the current build.)
- **F-16** Two doctrine systems coexist (control-pack authority order vs docs/00 authority order). Per both systems' own stop rules, this conflict should be resolved explicitly; this task treated explicit user instruction > security > control-pack > docs.
- **F-17** `config/realism_baseline.json` and `config/human_behavior_profiles.json` are not loaded by the engine — calibration constants are duplicated inline. Behavior profiles are entirely unused. Recommend a config-loading task (RS-CF-001).
- **F-18** Labor rates are hardcoded (16/18/22/28/35) vs BLS OEWS May 2025 (fast-food & counter ≈ $14.5/hr; supervisors ≈ $17–20/hr; food-prep group median $16.85). Left as gameplay-tuned values; flagged with source for calibration.

## 3D World Layer Added (all new files, presentation-only)

- `scripts/world/WorldBuilder.cs` — procedural full interior (grill, fryer bank, prep, walk-in, assembly, beverage, expo, office/break, counter, POS ×2, mobile shelf, menu board, dining) and exterior (parking lot + stripes, drive-thru lane + order board + canopy, glass storefront, roof/parapet, pole sign, landscaping, lighting, sky).
- `scripts/world/CameraDirector.cs` — 11 mounted CCTV cameras covering **every** station (grill, fryer, prep/walk-in, assembly, beverage/expo, front counter, lobby/dining, DT window, DT lane/board, lot/entrance, overhead) plus a WASD+mouse free camera; keys 1–9/0/O/C, auto-tour (T).
- `scripts/world/CharacterRig.cs` — procedural humanoids with code-driven walk/work/idle animation.
- `scripts/world/CustomerAgent.cs`, `CarAgent.cs`, `EmployeeAgent.cs`, `AgentManager.cs` — lobby guests, mobile pickups, delivery couriers, drive-thru vehicles with car-following and board/window stops; staff mirrored from live coverage values, break-room behavior from `CrewOnBreak`. Spawn/release driven by sim events; strict one-way data flow.
- `scripts/ui/Hud3D.cs`, `scripts/Main.cs` — CCTV overlay (REC tag, timestamp, ops strip, alerts), TAB toggles the existing 2D operations dashboard sharing the same `SimRunState`.

## Tests Run

`tools/engine-selftest` (dotnet 8, engine sources compiled outside Godot), seed 12345, all 10 scenarios:

- 90/90 checks PASS, 0 FAIL (replay hashes ×2, ledger reconciliation, lifecycle, chronology, envelope).
- normal_day: 942 orders (band 680–1230), $10.73 avg check (band $10–12), labor 24.2%, kitchen-ready SOS 99 s against the 330 s total-DT budget; rush_day 358 s; multi_rush 2189 s (intentionally requires player staffing response); equipment_failure shows the midday spike and recovers.

Godot editor compile/run not executed in this environment — open the project once locally to confirm (engine sources verified compiling under .NET 8).

## Security Review

Passed. Synthetic data only; role tokens only; no employee scoring (behavior profiles remain unused); no external connectors; no secrets; provenance on all outputs.

## Result

Ready for review pending one local Godot open/run.
