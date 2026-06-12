# RS-CF-001 — Checks
- [x] 120/120 self-test PASS (10 scenarios, seed 12345)
- [x] Envelope contract (business_day, ISO occurred_at, source const)
- [x] item.taken -> item.completed lifecycle; item.sold absent
- [x] Staff reason enum + waste reason enum respected
- [x] Inventory ledger equation reconciles (7 components incl cooked_*)
- [x] Tickets reconcile: active + completed + abandoned == orders
- [x] Determinism: repeat run hash-identical
- [x] Sample bundles validated (envelope, hashes, gates)
