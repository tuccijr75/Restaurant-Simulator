# Restaurant Daily Flow Simulator — Workflow Runtime

## Purpose

This file defines how Restaurant Daily Flow Simulator work moves from intent to verified simulator outputs and verified repository artifacts.

The workflow mirrors the AI Shift Commander build discipline while using Restaurant Simulator-specific packets, tasks, contracts, diagnostics, and approvals.

## Workflow Lifecycle

```text
Intent
→ Classify
→ Load Current Task
→ Load Required Source Packets
→ Contract Check
→ Security Check
→ Path Scope Check
→ Simulation / Build Plan
→ Generate / Execute / Modify
→ Validate Contracts
→ Validate Determinism
→ Validate Realism
→ Produce Outputs
→ Audit
→ Handoff
→ Receipt
→ Regression Capture
→ Next Action
```

## Runtime Classes

| Class | Name | Meaning | Approval |
|---|---|---|---|
| T0 | Answer | Informational response only | None |
| T1 | Draft | Scenario design, spec writing, task brief drafting, validation rubric, or non-executing analysis | None unless canon-changing or customer-visible |
| T2 | Artifact | Creates source files, schemas, task briefs, handoffs, audits, scenario configs, sample datasets, reports, or workflow docs | Required if source-changing or customer-visible |
| T3 | Tool | Runs validators, generators, replay tests, simulations, dataset builders, or regression suites | Depends on data and side effects |
| T4 | External | Connects to GitHub, ASC sandbox, external APIs, hosted systems, or shared accounts | Required when writing or integrating |
| T5 | High Risk | Handles real data, secrets, compliance claims, production systems, destructive actions, public release claims, or ASC production systems | Required with detailed receipt |

If classification is uncertain, choose the safer class.

## Workbench Lane

Default lane: `ChatGPT -> Codex/GitHub`.

Use ChatGPT for workflow design, contract design, scenario design, task brief creation, acceptance criteria, eval design, output review, security review, ASC compatibility review, audit, and handoff drafting.

Use Codex, local tools, or GitHub for simulator implementation, repository edits, schema validation, replay tests, dataset generation, performance tests, regression execution, and artifact export.

Do not blur planning and code execution. Code tasks require bounded task packets, acceptance criteria, allowed paths, prohibited paths, tests, and return receipts.

## Source Packets

```yaml
source_packets:
  00_security:
    path: control-pack/active/00_security/00_security.md
    purpose: security, data classification, trust boundaries, employee-scoring ban
  01_system:
    path: control-pack/active/01_system/01_system.md
    purpose: identity, product boundary, domains, event doctrine, build path
  02_workflow:
    path: control-pack/active/02_workflow/02_workflow.md
    purpose: runtime classes, task brief rules, workflow lifecycle, receipt rules
  03_schema:
    path: control-pack/active/03_schema/03_schema.json
    purpose: scenario, event, ledger, validation, workflow, receipt, and error schemas
  04_context:
    path: control-pack/active/04_context/04_context.md
    purpose: context loading, active task context, memory, provenance, stale-context rules
  05_diagnostics:
    path: control-pack/active/05_diagnostics/05_diagnostics.md
    purpose: preflight, checks, debugging, correction, regression, release gates
```

## Build Area Packet Loading Matrix

Load the smallest packet set required by task area.

| Build Area | Required Packets |
|---|---|
| Governance and Workflow | 00, 01, 02, 04, 05 |
| Source Pack / Doctrine | 00, 01, 02, 03, 04, 05 |
| Schema and Contracts | 00, 01, 02, 03, 04, 05 |
| Simulation Engine | 00, 01, 02, 03, 04, 05 |
| Event Stream / Ledgers | 00, 01, 02, 03, 04, 05 |
| Scenario Modeling | 00, 01, 02, 03, 04, 05 |
| Station / Equipment Mechanics | 00, 01, 02, 03, 04, 05 |
| Staffing Mechanics | 00, 01, 02, 03, 04, 05 |
| Validation Datasets | 00, 01, 02, 03, 04, 05 |
| UI / Dashboard | 00, 01, 02, 03, 05 |
| Export / Artifacts | 00, 01, 02, 03, 04, 05 |
| Monte Carlo / Stress Testing | 00, 01, 02, 03, 04, 05 |
| ASC Compatibility | 00, 01, 02, 03, 04, 05 plus approved ASC ingestion contracts only |
| External Connectors | 00, 01, 02, 03, 04, 05 plus explicit approval record |
| Deployment | 00, 01, 02, 03, 04, 05 plus explicit approval record |

Do not load unrelated packets or full generated-output history.

## Required Workflow Fields

```yaml
workflow_id:
version:
name:
project: Restaurant Daily Flow Simulator
owner: Michael Robertucci
runtime_class:
lane:
status:
goal:
inputs:
source_of_truth:
source_packets_loaded:
repository_paths_inspected:
constraints:
assumptions:
scenario:
contracts:
allowed_paths:
prohibited_paths:
steps:
approvals:
tools:
models:
artifacts:
verification:
audit:
handoff:
receipt:
rollback:
next_actions:
```

## Current Task Pointer

`build-workflows/task-briefs/current-task.md` must identify task ID, task brief, branch, lane, status, previous task, closure, active lane tasks, and next task. The pointer must not be updated without a task brief, audit, handoff, episode package, and receipt.

## Task Brief Template

Every build task must include task metadata, task objective, source packets to load, implementation sources to inspect, source-of-truth rules, requirements, non-goals, data contracts, approval boundaries, implementation constraints, required tests, acceptance criteria, audit checklist, handoff output, and stop rule.

Mandatory implementation constraints:

- preserve standalone simulator behavior
- preserve synthetic-data-only default
- preserve no employee scoring
- preserve deterministic replay
- preserve causal, non-random-only simulation
- preserve item lifecycle semantics: `item.taken -> item.completed`
- preserve output provenance
- preserve receipt/audit/handoff requirements

## Episode Package Rule

Every T2-T5 task must produce:

```text
build-workflows/episodes/RS-XXX/task.md
build-workflows/episodes/RS-XXX/trace.jsonl
build-workflows/episodes/RS-XXX/checks.jsonl
build-workflows/episodes/RS-XXX/audit.md
build-workflows/episodes/RS-XXX/handoff.md
build-workflows/episodes/RS-XXX/receipt.json
```

## Core Workflow: Generate Simulated Business Day

```yaml
workflow_name: generate_simulated_business_day
runtime_class: T3
trigger: user requests a simulated restaurant day, scenario dataset, ASC validation stream, or stress-test run
inputs:
  - restaurant_archetype
  - operating_hours
  - menu_catalog
  - station_model
  - equipment_model
  - staffing_plan
  - prep_inventory_model
  - scenario_type
  - seed
  - schema_version
  - generator_version
context_loaded:
  - 00_security.md
  - 01_system.md
  - 02_workflow.md
  - 03_schema.json
  - 04_context.md when previous state or provenance matters
  - 05_diagnostics.md
  - approved scenario config
  - ASC ingestion contracts only when compatibility review is explicitly required
steps:
  - validate_inputs
  - classify_security_risk
  - initialize_seeded_run
  - generate_daypart_baselines
  - apply_weather_traffic_event_modifiers
  - generate_arrival_curves
  - generate_orders_by_channel
  - emit_order_created
  - emit_item_taken
  - simulate_station_workload
  - simulate_task_dependencies
  - emit_item_completed
  - simulate_prep_inventory
  - simulate_staffing_assignments
  - simulate_waste
  - emit_ticket_updates
  - build_inventory_ledger
  - build_staffing_ledger
  - build_recommendation_validation_dataset
  - build_alert_validation_dataset
  - build_end_of_shift_summary
  - validate_schema
  - validate_deterministic_replay
  - validate_realism
  - produce_receipt
outputs:
  - operational_event_stream
  - inventory_ledger
  - staffing_ledger
  - recommendation_validation_dataset
  - alert_validation_dataset
  - end_of_shift_summary
  - run_receipt
completion_criteria:
  - all required outputs exist
  - all schemas validate
  - replay with same seed matches
  - security gate passes
  - event chronology is valid
  - item lifecycle reconciles
  - ledgers reconcile with event stream
  - realism checks pass or are flagged for manager review
receipt_required: true
```

## Scenario Workflow

Every scenario must be represented as structured configuration, not ad hoc prompt prose. Required fields include scenario ID/name/type, seed, restaurant archetype, operating profile, dayparts, channels, menu catalog, station model, equipment model, staffing plan, prep inventory model, weather, traffic, local event profile, stressors, output contracts, validation profile, data classification, and synthetic flag.

## Event Generation Rules

| Event Type | Trigger |
|---|---|
| `shift.started` | Simulation starts a business day or shift window |
| `order.created` | Customer/channel arrival converts into an order |
| `item.taken` | Order line item is accepted and inventory draw is recorded |
| `item.completed` | Item production path completes and item is ready for ticket completion |
| `ticket.updated` | Ticket status, queue state, completion state, or delay state changes |
| `staff.assignment.updated` | Role/station assignment changes or call-off-driven reassignment occurs |
| `prep.confirmed` | Prep batch is completed and inventory ledger increases |
| `waste.recorded` | Expired, overheld, damaged, or discarded prep/menu inventory is logged |
| `station.overloaded` | Station workload exceeds configured threshold for configured duration |
| `station.recovered` | Station returns under recovery threshold for configured duration |
| `shift.ended` | Simulation closes business day or shift window and summary is generated |

Do not emit overload, recovery, item completion, or ticket completion events as cosmetic noise.

## Inventory Rules

```text
opening_quantity + prep_confirmed - item_taken_consumption - waste_recorded + approved_adjustments = closing_quantity
```

`item.sold` is deprecated and must not be used in new active reconciliation logic.

## Completion Rule

A workflow can be marked complete only when required steps are complete, security gate passes, path scope passes, schema validation passes, deterministic replay passes when applicable, ledgers reconcile when applicable, realism checks pass or are flagged, audit exists, handoff exists, approvals are recorded, receipt exists, and next action is identified.

## Receipt Rule

Every T2-T5 workflow must produce a receipt containing task/workflow IDs, runtime class, lane, branch, status, timestamps, source packets, inspected paths, allowed/prohibited paths, created/modified/deleted files, tests and results, validation results, path scope result, approvals, conflicts, assumptions, blockers, security impact, rollback, and next required action.
