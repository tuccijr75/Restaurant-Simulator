# RS-GP-001 — Audit

**Date:** 2026-06-11  ·  **Verdict:** PASS (120/120 checks, 10 scenarios, seed 12345)

- Full-auto runs remain deterministic and pass all gates: decisions auto-resolve with defaults that reproduce competent baseline behavior.
- Auto-compliance (temps every 55 min, sanitizer every 110) runs only when ManagerMode is off; in Manager Mode the inspector punishes neglect — verified by equipment_failure scoring 65 (temp excursion caught) vs 90 elsewhere.
- Player inputs route through the same evented, seeded engine -> replayable human-vs-ASC comparison runs come free.

## Cross-task verification (shared run, post six-task integration)
normal_day: $10.49 avg check · labor 23.0% · csat 86.7 · abandoned 1.8% · inspection 90
rush_day: csat 87.9 · abandoned 38 (3.0%) · balked 13 · emergency supply runs engaged
multi_rush: 214s DT SOS · 99 balked cars · inspection 90
equipment_failure: inspection 65 (temp excursion caught during outage)
Sample bundles regenerated and machine-validated: envelope fields, sha256 hashes, ledger equation, all pass_fail gates green.
