# Restaurant Daily Flow Simulator — Diagnostics, Debugging, and Correction

## Purpose

This file defines how Restaurant Daily Flow Simulator detects errors, validates outputs, debugs failed simulations, corrects issues, prevents regressions, enforces workflow gates, and proves readiness for AI Shift Commander testing.

Diagnostics are first-class. A simulation or build task that cannot be explained, replayed, validated, audited, and handed off is not complete.

## Core Rule

No simulator output or build artifact is valid until diagnostics pass or are explicitly marked `review_required` with a receipt.

## Diagnostic Lifecycle

```text
Preflight
→ Security Gate
→ Source Packet Check
→ Path Scope Check
→ Schema Validation
→ Runtime Monitoring
→ Ledger Reconciliation
→ Deterministic Replay
→ Realism Evaluation
→ ASC Compatibility Check
→ Audit
→ Handoff
→ Correction
→ Regression Capture
→ Receipt
```

## Preflight Checks

```yaml
preflight_checks:
  - goal_defined
  - runtime_class_set
  - task_id_present_when_repo_work
  - current_task_loaded_when_repo_work
  - task_brief_loaded_when_repo_work
  - source_packets_identified
  - source_of_truth_identified
  - allowed_paths_defined_when_editing
  - prohibited_paths_defined_when_editing
  - approval_boundary_checked
  - security_policy_loaded
  - output_contracts_known
  - rollback_possible_or_noted
  - scenario_config_present_when_running_simulation
  - seed_present_when_running_simulation
  - schema_version_present
  - generator_version_present
  - synthetic_data_flag_present
  - data_classification_present
  - restaurant_archetype_defined_when_running_simulation
  - menu_catalog_defined_when_running_simulation
  - station_model_defined_when_running_simulation
  - equipment_model_defined_when_running_simulation
  - staffing_plan_defined_when_running_simulation
  - prep_inventory_model_defined_when_running_simulation
  - dayparts_defined_when_running_simulation
  - required_event_types_enabled
```

## Security Checks

```yaml
security_checks:
  - no_secrets_present
  - no_real_customer_pii
  - no_real_employee_identifiers
  - no_employee_scores
  - no_unapproved_external_connectors
  - no_production_endpoint_without_approval
  - no_unapproved_asc_modification
  - synthetic_data_declared
  - data_classification_declared
  - prompt_injection_patterns_rejected
  - path_scope_enforced
```

Failure of any security check blocks the run or task.

## Source Packet Checks

```yaml
source_packet_checks:
  - manifest_loaded_when_available
  - required_packets_loaded
  - task_specific_packets_loaded
  - deprecated_packet_data_absent
  - source_pack_versions_recorded
  - source_conflicts_reported
  - missing_source_stops_task
```

Do not invent missing source. Stop and produce a blocker record.

## Path Scope Checks

```yaml
path_scope_checks:
  - changed_files_within_allowed_paths
  - prohibited_paths_unchanged
  - control_pack_changes_approved
  - schema_changes_approved_when_contract_breaking
  - generated_artifacts_in_expected_episode_folder
  - no_unrelated_repo_files_changed
```

A path-scope failure requires rollback or failure receipt.

## Schema Checks

```yaml
schema_checks:
  - scenario_config_schema_valid
  - event_envelope_schema_valid
  - event_payload_schema_valid
  - item_lifecycle_schema_valid
  - inventory_ledger_schema_valid
  - staffing_ledger_schema_valid
  - recommendation_validation_dataset_schema_valid
  - alert_validation_dataset_schema_valid
  - end_of_shift_summary_schema_valid
  - receipt_schema_valid
  - task_brief_schema_valid_when_applicable
  - workflow_state_schema_valid_when_applicable
  - unknown_required_fields_absent_or_rejected
  - timestamp_format_valid
  - ids_consistent
```

Schema repair may be attempted once only when meaning does not change.

## Runtime Checks

```yaml
runtime_checks:
  - simulation_clock_monotonic
  - event_order_valid
  - seed_locked
  - daypart_transition_valid
  - demand_curve_generated
  - channel_mix_within_bounds
  - station_capacity_nonnegative
  - equipment_capacity_nonnegative
  - inventory_nonnegative_unless_shortage_modeled
  - staffing_capacity_nonnegative
  - item_taken_before_item_completed
  - ticket_completed_after_items_completed
  - overload_thresholds_enforced
  - recovery_thresholds_enforced
  - waste_reason_present
  - ticket_state_transitions_valid
  - retry_limit_not_exceeded
  - runtime_cost_within_budget
```

## Ledger Reconciliation Checks

Inventory ledger must reconcile:

```text
opening + prep_confirmed - consumption_from_item_taken - waste + approved_adjustments = closing
```

Staffing ledger must reconcile:

```text
scheduled_roles - call_offs + reassignments + breaks/returns = active_role_coverage_by_interval
```

Ticket state must reconcile:

```text
order.created → item.taken → item.completed → ticket.updated completed
```

`item.sold` is deprecated. Any active generated event stream containing `item.sold` fails contract validation unless a specific backwards-compatibility alias test was approved.

## Deterministic Replay Checks

A run passes replay only when scenario ID, seed, schema version, generator version, source-pack version, input models, event count, event hash, ledger hashes, validation dataset hashes, and end summary hash match.

Any mismatch must be classified as `DETERMINISM_FAILURE` unless a versioned input changed.

## Realism Checks

Realism checks are plausibility gates, not proof of reality.

Required realism categories:

- daypart arrival curves are distinct and scenario-appropriate
- order mix changes by daypart and channel
- item lifecycle follows `item.taken -> item.completed -> ticket.updated completed`
- severe weather has directional demand or channel effect
- traffic affects drive-thru or lobby demand where configured
- local event surge has start/peak/decay pattern
- overloads arise from workload-capacity relationship
- recovery follows demand drop, capacity increase, or time clearance
- prep depletes from item-taken events
- prep-confirmed increases usable inventory
- waste has reason and timing
- call-off reduces capacity or forces reassignment
- multi-rush conditions interact without random chaos

## ASC Compatibility Checks

```yaml
asc_compatibility_checks:
  - approved_asc_contract_loaded_when_available
  - required_event_types_present_when_triggered
  - event_envelope_matches_contract
  - item_lifecycle_mapping_declared
  - item_ids_consistent
  - station_ids_consistent
  - channel_ids_consistent
  - inventory_ids_consistent
  - staffing_role_ids_consistent
  - recommendation_validation_rows_have_expected_labels
  - alert_validation_rows_have_expected_labels
  - no_manual_schema_repair_needed_for_ingestion
```

If ASC contracts are unavailable, validate against `03_schema.json` and mark status `simulator_contract_valid_asc_contract_pending`.

## Output Checks

```yaml
output_checks:
  - operational_event_stream_exists
  - inventory_ledger_exists
  - staffing_ledger_exists
  - recommendation_validation_dataset_exists
  - alert_validation_dataset_exists
  - end_of_shift_summary_exists
  - run_receipt_exists
  - schema_valid
  - security_valid
  - deterministic_replay_valid
  - ledgers_reconcile
  - event_chronology_valid
  - item_lifecycle_valid
  - causality_explainable
  - no_unresolved_placeholders
  - no_unapproved_side_effects
  - no_hardcoded_brand_assumptions
  - no_employee_scoring
```

## Build Artifact Checks

```yaml
build_artifact_checks:
  - task_brief_exists
  - current_task_pointer_updated_if_required
  - source_packets_used_recorded
  - repository_paths_inspected_recorded
  - allowed_paths_respected
  - prohibited_paths_unchanged
  - audit_exists
  - handoff_exists
  - episode_task_exists
  - episode_trace_exists
  - episode_checks_exists
  - episode_audit_exists
  - episode_handoff_exists
  - episode_receipt_exists
  - tests_or_not_run_reason_recorded
  - next_action_recorded
```

## Error Classes

```yaml
error_classes:
  - INPUT_ERROR
  - SCENARIO_CONFIG_ERROR
  - SECURITY_ERROR
  - SCHEMA_ERROR
  - CONTRACT_MISMATCH
  - DETERMINISM_FAILURE
  - RUNTIME_ERROR
  - LEDGER_RECONCILIATION_ERROR
  - EVENT_CHRONOLOGY_ERROR
  - ITEM_LIFECYCLE_ERROR
  - REALISM_FAILURE
  - ASC_COMPATIBILITY_ERROR
  - PATH_SCOPE_ERROR
  - SOURCE_PACKET_ERROR
  - WORKFLOW_STATE_ERROR
  - TOOL_ERROR
  - MODEL_ERROR
  - SOURCE_CONFLICT
  - VERIFICATION_ERROR
  - UNKNOWN_ERROR
```

## Debugging Protocol

When an error occurs:

1. Stop the current step if continuing would corrupt outputs.
2. Preserve trace metadata and failed artifact references.
3. Classify the error.
4. Identify the failed contract, path rule, source packet rule, or realism rule.
5. Determine whether retry is safe.
6. Correct the smallest failed layer.
7. Re-run the failing check.
8. Re-run dependent checks.
9. Add a regression case if the failure can recur.
10. Record the result in the receipt.

## Regression Capture

Create a regression case for deterministic replay mismatch, invalid event envelope, impossible inventory ledger, `item.completed` without `item.taken`, ticket completed before all items complete, deprecated `item.sold`, invalid overload/recovery, waste without reason, daypart flatline, ignored modifiers, hardcoded brand assumption, employee scoring, missing synthetic flag, path-scope violation, or missing task receipt.

## Plausibility Review Rubric

Manager-facing realism categories are daypart demand, channel mix, order mix, item lifecycle and ticket timing, station bottlenecks, equipment constraints, prep depletion, waste behavior, staffing pressure, external factor influence, summary usefulness, and overall plausibility.

A run passes manager plausibility when average score is at least 4 and no critical category scores below 3.

## Completion Gate

```yaml
completion_gate:
  - goal_satisfied
  - required_outputs_exist_or_not_applicable
  - required_episode_artifacts_exist_when_T2_to_T5
  - security_gate_passed
  - path_scope_passed
  - schema_valid
  - deterministic_replay_passed_when_applicable
  - ledger_reconciliation_passed_when_applicable
  - asc_compatibility_checked_when_applicable
  - realism_checked_when_applicable
  - diagnostics_passed
  - approvals_recorded
  - audit_created
  - handoff_created
  - receipt_created
  - next_actions_identified
```

If any completion gate fails, the workflow is not complete.

## Release Readiness Gate

The simulator is not ready for ASC validation use until required event contracts validate, item lifecycle validates, deprecated `item.sold` is absent from active generated streams, ledgers reconcile, deterministic replay passes, required scenarios pass automated checks, manager-style plausibility review is complete or accepted pending, Monte Carlo validates, stress testing is explainable, security gate passes, and the build workflow has task briefs, audits, handoffs, receipts, and regression suite.
