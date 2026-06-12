# RS-HQ-001 — Hold pans, quality decay, satisfaction, abandonment

**Date:** 2026-06-11  ·  **Status:** complete  ·  **Workflow:** build-workflows v1

## Objective
Replace per-item cook abstraction with cook-to-hold physics: crew cooks full batch
cycles into FIFO hold pans, orders draw in seconds, aged product expires to waste.
Add guest patience/abandonment, DT balking, and per-ticket satisfaction.

## Scope of change
- HoldPan model per family: fried_main (330s cycle, batch 8, cap 16, 20-min limit), grilled_main (150s/6/12/15m), fries (180s/4/12/7m per docs/06). Cooked goods are first-class inventory components (cooked_*).
- Ledger chain: raw drawn at batch cook, cooked received via prep.confirmed, consumed at item.taken, expiry wasted as holding_time_exceeded. Equation verified per component.
- item.taken defers until the pan can supply the item (stockout = guest waits; lifecycle stays truthful).
- Freshness-capped par policy: never pre-cook more than sells within the hold window; below half a batch the pan runs cook-to-order. Reactive batch on a waiting guest.
- Patience: DT 540s, lobby 600s, mobile 900s, delivery 1200s (operator_calibration_required) -> ticket.updated status=abandoned, sales reversed, lost_sales tracked. DT balk at 9-deep lane.
- Quality = freshness at draw (100 -> 60 across the hold window). Satisfaction = 100 - speed overage - freshness penalty - remake penalty; 1% expo-caught remakes (production_error waste + redo chain).
- Emergency supply runs (<=2/day, $120, 45-min arrival) when an unforecast surge will outrun the day's order.

## Acceptance
- Headless self-test suite passes for all 10 scenarios, seed 12345 (F9 gate in-game).
- Schema contract intact: envelope fields, split item lifecycle, staff/waste reason enums, no item.sold.
- Inventory ledger equation reconciles for every component.
- Determinism: identical replay for identical (config, scenario, seed).
