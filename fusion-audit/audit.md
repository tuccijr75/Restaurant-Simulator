# Fusion Commit-Pinned Multi-Model Repository Audit

The repository-access verification must pass before this audit begins.

Use only these immutable evidence URLs:

Manifest:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/manifest.md

Core evidence:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/core.xml

Godot and C# evidence:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/game.xml

Python and test evidence:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/python.xml

Tracked repository tree:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/tree.txt

Snapshot status:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/status.txt

## Evidence rules

Every statement must be labeled:

- OBSERVED
- INFERRED
- RECOMMENDED
- UNKNOWN

Every OBSERVED claim must provide:

- exact evidence URL
- exact repository path
- exact class, method, test, schema property, or configuration key when applicable

Do not describe illustrative code as existing repository code.

Do not use CURRENT, verified, or confirmed without path evidence.

Do not make a negative claim unless tree.txt and every relevant evidence pack were searched and the search terms and inspected paths are reported.

Do not substitute search results, cached summaries, or generic restaurant-simulator architecture.

## Independent model roles

Model 1: architecture and runtime implementation.

Model 2: contracts, determinism, validation, tests, security, ledgers, exports, and AI Shift Commander compatibility.

Model 3: adversarial verifier. Reject unsupported claims, invented files, wrong-repository assumptions, stale context, arbitrary scores, and recommendations presented as existing implementation.

Consensus is not evidence. A finding survives only when the adversarial verifier can locate direct supporting evidence.

## Required scope

Audit:

1. Repository identity and current task.
2. Product purpose and system boundaries.
3. Godot and C# architecture.
4. Python architecture.
5. Deterministic seed and replay behavior.
6. Scenario and daypart modeling.
7. Orders and item lifecycle.
8. Stations and equipment constraints.
9. Per-ingredient inventory, hold-time, temperature, consumption, and waste mechanics.
10. Staffing capacity without employee scoring.
11. Event streams and ledger contracts.
12. Validation datasets and exports.
13. Tests, CI, diagnostics, receipts, and verification coverage.
14. Security and synthetic-data controls.
15. Source-pack compliance.
16. Current-task acceptance status and blockers.
17. Technical debt and production-readiness gaps.

## Finding format

For every finding provide:

- Finding ID
- Classification
- Severity
- Evidence URL
- Repository path
- Symbol or configuration key
- Evidence summary
- Operational impact
- Recommended action
- Confidence
- Verification method

## Final output

Return:

1. Evidence receipt.
2. Exact source commit audited.
3. Evidence URLs retrieved successfully.
4. Paths inspected.
5. Paths not inspected.
6. Actual architecture.
7. Current task and implementation status.
8. Findings ordered by severity.
9. Existing strengths.
10. Contract or doctrine conflicts.
11. Test and validation gaps.
12. Claims rejected by the adversarial verifier.
13. Unknowns and limitations.
14. Prioritized next actions.

Do not provide a numeric quality score unless the rubric and evidence for every scored category are shown.

END_OF_AUDIT_PROMPT
