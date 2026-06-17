# Fusion Audit Manifest

## Repository identity

Repository: tuccijr75/Restaurant-Simulator
Source branch: main
Source commit: b0e416339be7329212e804754779ceac7a0683a4
Source working tree: clean
Audit branch: fusion-audit-b0e41633-20260617153549
Generated at UTC: 2026-06-17T19:35:48Z

## Active workflow state

Current task ID: RS-IM-001
Lane: Engine / Godot
Status: ready_for_review

## Authority

This manifest and the evidence files below represent the tracked repository at the exact source commit above.

Fusion must use these files as the primary source of truth. Search-engine snippets, cached summaries, similarly named repositories, and generic restaurant-simulator assumptions are not repository evidence.

If a required file is inaccessible, truncated, or missing its ending marker, Fusion must return ACCESS_VERIFICATION_FAILED and stop.

## Evidence files

| File | Purpose | SHA-256 | Required ending marker |
|---|---|---|---|
| core.xml | README, manifests, source doctrine, workflow state, current task, documentation, and CI definitions | 0d3ecba8a095d5ef2f6515a61712f87a9befbc97d38b296de102a7abcdc099b5 | END_OF_CORE_PACK |
| game.xml | Godot project, C# or GDScript implementation, scenes, resources, and game configuration | daac8a57442a67f445aab458f3422a1e99fa0e3e6d0bcb013747c694c7587427 | END_OF_GAME_PACK |
| python.xml | Python runtime, validators, profiles, tests, packaging, and CI definitions | 433e03a68b19b11a503ed577f221bbab08c6dd083548837abf45ebcee80b7ced | END_OF_PYTHON_PACK |
| tree.txt | Complete tracked path list at the source commit | f80d2c948a184924fd0537b03c3008ebfe7cd0ef3ca83e6a3367b3fb5e04798d | END_OF_TREE_PACK |
| status.txt | Snapshot identity and active workflow state | ee8d74563c3d321cf6c80fb927337ff5234cc3adfa92c66d4ca0d114b05e5e4d | END_OF_STATUS_PACK |

## Mandatory access verification

Before analyzing the repository, Fusion must report:

1. The exact repository name.
2. The exact source commit SHA.
3. The current task ID, lane, and status.
4. The actual languages and runtime frameworks, with repository-path evidence.
5. Three exact C# or Godot implementation paths.
6. Three exact Python or test paths.
7. The ending marker from every required evidence file.
8. Any file that could not be retrieved completely.

Every answer must identify the evidence file and exact repository path.

## Evidence policy

Every repository claim must be labeled:

- OBSERVED: directly supported by an evidence file.
- INFERRED: reasoned from observed evidence.
- RECOMMENDED: proposed future work.
- UNKNOWN: insufficient evidence.

An OBSERVED claim requires an exact repository path and, when applicable, a class, method, test, schema property, or configuration key.

A negative claim such as "no tests," "no CI," or "no deterministic support" is valid only when tree.txt and all relevant evidence packs were searched and the search scope is reported.

Model consensus is not verification. Repetition of an unsupported claim remains unsupported.

## Stop rule

If repository access cannot be proven, output exactly:

ACCESS_VERIFICATION_FAILED

Then list the missing or truncated evidence. Do not continue with an audit.

END_OF_FUSION_AUDIT_MANIFEST
