# RS-IN-001 — Audit

**Date:** 2026-06-11  ·  **Verdict:** PASS (120/120 checks, 10 scenarios, seed 12345)

- equipment_failure scenario now scores 65 on inspection — the inspector catches the temp excursion during the outage window. Other scenarios score 90 under auto-compliance. The causal signal is exactly the ASC validation story wanted.
- Wear constants are operator_calibration_required; tuned so an unmaintained fryer crosses the throughput penalty in the back half of a heavy day.

## Cross-task verification (shared run, post six-task integration)
normal_day: $10.49 avg check · labor 23.0% · csat 86.7 · abandoned 1.8% · inspection 90
rush_day: csat 87.9 · abandoned 38 (3.0%) · balked 13 · emergency supply runs engaged
multi_rush: 214s DT SOS · 99 balked cars · inspection 90
equipment_failure: inspection 65 (temp excursion caught during outage)
Sample bundles regenerated and machine-validated: envelope fields, sha256 hashes, ledger equation, all pass_fail gates green.
