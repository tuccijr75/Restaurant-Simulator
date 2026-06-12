# RS-HQ-001 — Handoff

**Date:** 2026-06-11

## What landed
- HoldPan model per family: fried_main (330s cycle, batch 8, cap 16, 20-min limit), grilled_main (150s/6/12/15m), fries (180s/4/12/7m per docs/06). Cooked goods are first-class inventory components (cooked_*).
- Ledger chain: raw drawn at batch cook, cooked received via prep.confirmed, consumed at item.taken, expiry wasted as holding_time_exceeded. Equation verified per component.
- item.taken defers until the pan can supply the item (stockout = guest waits; lifecycle stays truthful).
- Freshness-capped par policy: never pre-cook more than sells within the hold window; below half a batch the pan runs cook-to-order. Reactive batch on a waiting guest.
- Patience: DT 540s, lobby 600s, mobile 900s, delivery 1200s (operator_calibration_required) -> ticket.updated status=abandoned, sales reversed, lost_sales tracked. DT balk at 9-deep lane.
- Quality = freshness at draw (100 -> 60 across the hold window). Satisfaction = 100 - speed overage - freshness penalty - remake penalty; 1% expo-caught remakes (production_error waste + redo chain).
- Emergency supply runs (<=2/day, $120, 45-min arrival) when an unforecast surge will outrun the day's order.

## How to verify
1. Open game/ in Godot 4.6 (.NET), run, press F9 — expect PASS for the loaded scenario.
2. Headless: tools/engine-selftest (dotnet run) — 10-scenario suite.
3. F5 exports the 8-file contract to user://outputs/sim_{scenario}_{seed}.

## Known limits / next
- Visual layer compiles only in Godot — smoke-test in-editor (day/night sweep, KDS boards, click panels, decision toast).
- Calibration constants marked operator_calibration_required throughout SimConfig.
- Auto-policy hold waste (grilled ~32% in shoulders) is the headroom a Manager Mode player can beat.
