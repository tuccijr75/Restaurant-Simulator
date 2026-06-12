# RS-HQ-001 — Audit

**Date:** 2026-06-11  ·  **Verdict:** PASS (120/120 checks, 10 scenarios, seed 12345)

- 120/120 PASS. Normal day: csat 86.7, abandonment 1.8%, balked 0, avg check $10.49.
- Diagnostic history (preserved in episode): naive fixed par starved the store by 18:30 — the 7-min fry hold turned the par floor into a waste pump that exhausted side_base/protein (160 abandoned). Freshness-capped par + reactive batches + honest safety stock (15% over forecast burn) fixed it causally: 160 -> 38 -> 16.
- rush_day passes via emergency supply decisions (abandoned 214 -> 38) — an unforecast surge against a fixed supply order is authentically dangerous.
- Honest finding: auto-policy grilled hold waste runs ~32% of production in shoulder periods (15-min limit vs batch 6). Left as-is deliberately: it is the inefficiency a Manager Mode player can beat, and waste cost is fully booked.

## Cross-task verification (shared run, post six-task integration)
normal_day: $10.49 avg check · labor 23.0% · csat 86.7 · abandoned 1.8% · inspection 90
rush_day: csat 87.9 · abandoned 38 (3.0%) · balked 13 · emergency supply runs engaged
multi_rush: 214s DT SOS · 99 balked cars · inspection 90
equipment_failure: inspection 65 (temp excursion caught during outage)
Sample bundles regenerated and machine-validated: envelope fields, sha256 hashes, ledger equation, all pass_fail gates green.
