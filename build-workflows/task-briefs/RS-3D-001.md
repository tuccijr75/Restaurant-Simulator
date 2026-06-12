# Task Brief — RS-3D-001

## Task Metadata

Task ID: RS-3D-001
Task Name: Engine Audit/Repair + 3D World, CCTV Coverage, and Animated Characters
Task Type: Engine repair / 3D presentation layer / validation gate
Target Layer: game/ (Godot 4.6 C#)
Runtime Class: T3
Owner: Michael Robertucci
Approval Required: User explicitly requested full audit, fixes, and the 3D build.
Expected Output: Deterministic engine with reconciling ledgers and full output contract; 3D interior/exterior with every station camera-covered; animated employees, customers, and vehicles; in-engine self-test gate.
Repository Branch: main

Files Allowed To Modify: game/**, docs/07_3D_WORLD_AND_CAMERAS.md, build-workflows/** (RS-3D-001 artifacts, state, current-task), tools/engine-selftest/**
Files Prohibited From Modifying: control-pack/active/**, restaurant_simulator/**, README.md, pyproject.toml, AI Shift Commander repository or files

## Objective

Audit the current game/ engine against control-pack doctrine and docs/06 realism acceptance; fix every defect found (determinism, ledgers, throughput, exports, performance); add the 3D restaurant world with per-station CCTV and AI-driven character animation, as a strictly read-only presentation layer over the deterministic core.

## Subsumes

RS-EN-001 (engine lifecycle/provenance verification) — verification is implemented as the in-engine F9 self-test plus the dotnet harness, and was executed for all 10 scenarios.

## Stop Rule

If doctrine conflict between control-pack and docs/ requires a contract change, stop and report (recorded as finding F-16; no contract semantics were changed).
