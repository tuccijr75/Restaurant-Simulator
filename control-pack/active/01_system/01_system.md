# Restaurant Daily Flow Simulator — System Doctrine

## Identity

Restaurant Daily Flow Simulator is a standalone simulation platform for generating realistic fast-food restaurant operations data across full simulated business days.

It exists to support AI Shift Commander development, testing, validation, training, and stress testing without requiring a live restaurant.

## Project Diagnosis

```yaml
project_name: Restaurant Daily Flow Simulator
project_type: standalone synthetic operations simulation platform
primary_user: Michael Robertucci
primary_use: AI Shift Commander validation-data generation
end_user: AI Shift Commander developers, QA reviewers, restaurant-operations testers, and training/demo users
current_problem: AI Shift Commander needs realistic operational data before live restaurant deployment, but live restaurant data is unavailable, risky, expensive, inconsistent, or too narrow for stress testing.
desired_outcome: generate full-day, repeatable, ASC-compatible event streams, ledgers, validation datasets, and receipts that model plausible fast-food operations under normal, slow, rush, disrupted, and multi-rush conditions.
success_metric: experienced restaurant managers judge single-day simulations as plausible and operationally useful; ASC validation runs consume the output without manual schema repair.
runtime_class: T3
workbench_lane: ChatGPT -> Codex/GitHub
source_pack_version: rs_source_pack_v1.1
repository: https://github.com/tuccijr75/Restaurant-Simulator
workflow_pattern_reference: AI Shift Commander build-workflow pattern, adapted only for Restaurant Simulator
risk_level: medium by default; high when live external data, ASC endpoints, hosted deployment, or public datasets are involved
final_deliverable: governed workflow source pack for designing, building, validating, and operating Restaurant Daily Flow Simulator
```

## Prime Directive

Generate realistic, deterministic, ASC-compatible restaurant operations simulations without depending on live restaurant access.

The simulator must model operational causality, not random noise. Arrival curves, order mix, item lifecycle, station load, task dependencies, inventory depletion, prep replenishment, waste, staffing capacity, drive-thru pressure, lobby pressure, mobile demand, delivery demand, weather, traffic, local events, equipment limits, and daypart transitions must interact through explicit rules and calibrated seeded distributions.

## Product Boundary

Restaurant Daily Flow Simulator is independent from AI Shift Commander.

It may:

- emit ASC-compatible operational events
- emit ASC-compatible ledgers and validation datasets
- replay scenarios against approved ASC ingestion tests
- create stress, regression, and training datasets
- summarize simulated end-of-shift operations
- inspect AI Shift Commander workflow files as a pattern reference when requested
- inspect AI Shift Commander ingestion contracts when compatibility work is explicitly requested

It must not:

- become AI Shift Commander
- modify AI Shift Commander
- change ASC runtime logic
- silently write to ASC sandbox or production systems
- depend on ASC being online for core simulation
- require real restaurant data for V1 operation
- score employees
- hardcode Wendy's or any specific brand assumptions
- treat synthetic results as real operational evidence

## Authority Hierarchy

When instructions conflict, follow this order:

1. Explicit current user approval within the current task scope
2. `00_security.md`
3. `01_system.md`
4. `02_workflow.md`
5. `03_schema.json`
6. `04_context.md`
7. `05_diagnostics.md`
8. Approved Restaurant Simulator task brief
9. Restaurant Simulator repository files and tests
10. AI Shift Commander ingestion contracts supplied or approved for compatibility review
11. Approved simulator scenario configs
12. Approved menu, station, staffing, equipment, and inventory models
13. Simulation receipts and validation reports
14. Memory
15. Assistant inference

Security outranks convenience. Schema contracts outrank prose preferences. Current explicit approval outranks prior assumptions only within the approved scope.

## Repository Workflow Model

Restaurant Simulator adopts the same build-workflow discipline used by AI Shift Commander, but with simulator-specific source packets and artifact gates.

Required structure:

```text
control-pack/manifest.json
control-pack/active/00_security/00_security.md
control-pack/active/01_system/01_system.md
control-pack/active/02_workflow/02_workflow.md
control-pack/active/03_schema/03_schema.json
control-pack/active/04_context/04_context.md
control-pack/active/05_diagnostics/05_diagnostics.md
build-workflows/state/state.json
build-workflows/task-briefs/current-task.md
build-workflows/task-briefs/RS-*.md
build-workflows/prompts/RS-*-builder-prompt.md
build-workflows/handoffs/RS-*-handoff.md
build-workflows/audits/RS-*-audit.md
build-workflows/episodes/RS-*/task.md
build-workflows/episodes/RS-*/trace.jsonl
build-workflows/episodes/RS-*/checks.jsonl
build-workflows/episodes/RS-*/audit.md
build-workflows/episodes/RS-*/handoff.md
build-workflows/episodes/RS-*/receipt.json
build-workflows/locks/*.lock.json
```

No build task may modify files outside its allowed paths.

## Runtime Classes

Default runtime class: `T3`.

| Runtime | Use |
|---|---|
| T0 | Answering a question about the simulator |
| T1 | Drafting a scenario, spec, task brief, rubric, or review note |
| T2 | Creating source files, schemas, task briefs, handoffs, audits, datasets, or reports |
| T3 | Running validators, scenario generators, deterministic replay tests, simulations, or regression suites |
| T4 | Connecting to GitHub, ASC sandbox, external APIs, hosted services, or shared accounts |
| T5 | Handling real restaurant data, secrets, compliance claims, production changes, destructive actions, public release claims, or ASC production endpoints |

Choose the lowest sufficient runtime class. If uncertain, choose the safer class.

## Required Behavior

The system must:

- diagnose before building
- read the task brief before editing
- load only required source packets
- confirm authority hierarchy
- identify dependencies, approval boundaries, missing canon, and path scope
- generate or modify only assigned artifacts
- keep the simulator standalone from ASC
- preserve ASC-compatible output contracts
- generate deterministic runs when seed, scenario, schema, generator version, and source-pack version match
- model causal relationships between demand, capacity, item lifecycle, inventory, staffing, waste, equipment, and station overload
- separate configuration from generated results
- separate synthetic data from real data
- validate every event and ledger against schema
- produce receipts for T2-T5 work
- provide provenance for replay, debugging, and regression testing
- reject stub logic, random-only logic, and brand-specific assumptions unless explicitly configured
- state assumptions and unknowns when calibration is uncertain
- prefer configurable restaurant archetypes over hardcoded brands

## Forbidden Behavior

The system must not:

- use stub logic as if it were complete
- rely on pure random generation without operational structure
- hardcode Wendy's assumptions, menu names, staffing patterns, station layout, or service logic
- generate employee scoring, rankings, disciplinary recommendations, or individual performance labels
- emit synthetic data without labeling it synthetic
- claim a simulated run is real
- bypass schema validation
- treat manager plausibility as proven without review or eval evidence
- write to ASC production without explicit approval
- introduce live APIs, real data, credentials, or hosted deployment without approval
- silently change event contracts
- silently weaken security, privacy, workflow, or diagnostic gates
- modify AI Shift Commander while performing Restaurant Simulator work
- use stale source files over active repository/source-pack files

## Simulation Domains

The simulator must support customers, orders, item lifecycle, menu items, tickets, task dependencies, stations, equipment units, prep inventory, staffing, waste, drive-thru, lobby, mobile orders, delivery orders, weather effects, local events, traffic patterns, and daypart changes as first-class interacting systems.

## Required Dayparts

```yaml
dayparts:
  - breakfast
  - mid_morning
  - lunch
  - afternoon
  - dinner
  - late_night
```

Each daypart must define time window, arrival curve, channel mix, menu mix, basket-size distribution, station load profile, prep drawdown profile, waste risk profile, staffing coverage assumptions, equipment pressure assumptions, and disruption sensitivity.

## Required Event Types

Restaurant Simulator currently uses the split item lifecycle contract:

```yaml
event_types:
  - order.created
  - item.taken
  - item.completed
  - ticket.updated
  - staff.assignment.updated
  - prep.confirmed
  - waste.recorded
  - station.overloaded
  - station.recovered
  - shift.started
  - shift.ended
```

`item.taken` means the order line has been accepted and inventory draw has been recorded. `item.completed` means the item has passed its required production path and is ready for ticket completion. `item.sold` is deprecated and must not be emitted by active generator code unless an explicit backwards-compatibility alias task is approved.

## Scenario Requirements

The simulator must support normal days, slow days, rush days, weather disruptions, staffing call-offs, equipment failures, local event surges, school event surges, holiday patterns, and multi-rush conditions.

Scenario generation must combine structured parameters with deterministic seeds. A scenario must be explainable: the system must be able to state why demand rose, why a station overloaded, why inventory depleted, why a ticket delayed, or why waste occurred.

## Realism Doctrine

```text
calendar + daypart + weather + traffic + local events
→ customer arrival intensity by channel
→ order composition by daypart and customer segment
→ item.taken and inventory draw
→ task dependencies across equipment and stations
→ station workload and ticket state changes
→ prep inventory depletion and replenishment
→ equipment/staff capacity constraints
→ item.completed and ticket completion
→ overload/recovery events
→ waste events
→ end-of-shift operational summary
```

Bounded stochastic variation is allowed only inside configured seeded distributions. It must not be the primary behavior engine.

## ASC Compatibility Doctrine

ASC compatibility means generated outputs are consumable by approved ASC ingestion contracts without manual repair. If ASC contracts conflict with simulator contracts, stop and report the mismatch. Do not silently reshape simulator output.

## Approval Boundaries

Approval is required before changing source-pack doctrine, required event contracts, security policy, real data handling, live APIs, hosted deployment, ASC runtime endpoints, public datasets, customer-visible claims, credentials, artifact deletion, validated-status claims, or AI Shift Commander files.

## Done Means Proven

A simulator run is complete only when required outputs exist, schema validation passes, deterministic replay passes, security gate passes, realism checks pass or are marked for review, and receipt exists.

A build task is complete only when task brief exists, allowed/prohibited paths were enforced, source packets are recorded, changed files are listed, checks are recorded, audit exists, handoff exists, episode package exists, receipt exists, and next action is identified.

## One Best Path

1. Lock source-pack workflow and contracts.
2. Align schema with current event lifecycle.
3. Implement deterministic scenario configuration.
4. Generate demand curves by daypart and scenario.
5. Generate orders and item mix from demand.
6. Simulate item lifecycle, station load, equipment, prep inventory, staffing, waste, and channel queues.
7. Emit ASC-compatible events and ledgers.
8. Validate schema, replay determinism, causality, and plausibility.
9. Add Monte Carlo and stress testing after the single-day run is trustworthy.

Do not add advanced AI, live integrations, hosted deployment, or production claims before the deterministic core is proven.
