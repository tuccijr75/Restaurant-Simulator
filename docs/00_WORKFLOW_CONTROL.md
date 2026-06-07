# 00 Workflow Control

## Purpose

This document is the first file to read before starting any Restaurant Simulator task. It controls task routing, source authority, approval boundaries, and completion rules.

## Locked product direction

Restaurant Simulator is a standalone Windows desktop restaurant-operations simulation game and AI Shift Commander test harness.

Official stack:

- Godot 4
- C# / .NET for simulation and game logic
- Windows desktop as the primary target
- JSON for editable scenario/config contracts
- JSONL for replayable event streams
- SQLite for local saves, run receipts, and scenario results when persistence is needed
- Local HTTP/WebSocket adapter for optional AI Shift Commander integration

The game must function fully without AI Shift Commander. ASC is an optional observer/recommender/test subject.

## Start-of-task rule

Before starting any task, read these docs in order:

1. `docs/00_WORKFLOW_CONTROL.md`
2. `docs/01_PRODUCT_SPEC.md`
3. `docs/02_TECHNICAL_ARCHITECTURE.md`
4. `docs/03_VARIABLE_CATALOG.md`
5. `docs/04_ASC_INTEGRATION_SPEC.md`
6. `docs/05_BUILD_PLAN.md`

If the task touches code, also inspect the current Godot project files and tests before editing.

## Source authority

1. Current explicit user instruction
2. These workflow docs
3. Existing repo files and tests
4. Prior simulator source doctrine
5. AI Shift Commander ingestion contracts when provided
6. Generated receipts and validation reports
7. Assistant memory/inference

If sources conflict, stop and report the conflict before changing code or doctrine.

## Non-negotiables

- Standalone Windows desktop game.
- Works without ASC.
- ASC integration must be optional.
- Realistic fast-food variables and conditions.
- Adjustable scenario variables.
- Working in-game POS and KDS systems.
- Deterministic seed support for replay and ASC testing.
- Synthetic data by default.
- No real customer data.
- No real employee data.
- No employee scoring, ranking, disciplinary analytics, or HR-style evaluation.
- No brand-specific hardcoding.
- No random-only behavior.

## Employee simulation boundary

Allowed: synthetic worker operational traits such as training level, role familiarity, fatigue, speed variance, accuracy variance, attendance events, and food-safety compliance probability.

Forbidden: employee score, employee rank, disciplinary recommendation, real employee identity, productivity score, weak-worker labels, or HR claims.

## Approval required before

- Changing these workflow docs.
- Changing official stack.
- Connecting to live ASC systems.
- Importing real restaurant/POS/KDS/staffing/customer data.
- Publishing or customer-facing claims.
- Adding external APIs, credentials, or hosted services.
- Implementing employee analytics beyond synthetic role-capacity simulation.
- Deleting source files or generated receipts.

## Task receipt format

Every material task must finish with:

```yaml
receipt:
  task:
  files_changed:
  decisions:
  validation:
  risks:
  next_step:
```

## Completion rule

A task is complete only when:

- Required files are changed or explicitly left unchanged.
- Project direction remains Godot 4 + C# Windows desktop.
- The game remains standalone from ASC.
- Security and employee-boundary rules are preserved.
- Build/test status is reported honestly.
- Follow-up risks are listed.
