# RS-CF-001 — Audit

**Date:** 2026-06-11  ·  **Verdict:** PASS (120/120 checks, 10 scenarios, seed 12345)

- Determinism preserved: config content is part of the input; same (config, scenario, seed) -> identical event stream. Defaults reproduce pre-task behavior exactly.
- Profile aggregation reviewed against doctrine: no per-person attribute is ever exposed, logged, or scored — the blend collapses to one station-level multiplier.

## Cross-task verification (shared run, post six-task integration)
normal_day: $10.49 avg check · labor 23.0% · csat 86.7 · abandoned 1.8% · inspection 90
rush_day: csat 87.9 · abandoned 38 (3.0%) · balked 13 · emergency supply runs engaged
multi_rush: 214s DT SOS · 99 balked cars · inspection 90
equipment_failure: inspection 65 (temp excursion caught during outage)
Sample bundles regenerated and machine-validated: envelope fields, sha256 hashes, ledger equation, all pass_fail gates green.
