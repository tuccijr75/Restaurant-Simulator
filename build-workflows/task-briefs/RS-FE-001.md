# RS-FE-001 — Front-end window/counter service labor

**Date:** 2026-06-11  ·  **Status:** complete  ·  **Workflow:** build-workflows v1

## Objective
Model order intake and handoff as labor-consuming tasks at the drive-thru window,
front counter, and pickup shelf, so peak-hour strain is causal (service positions
bound throughput) rather than synthetic.

## Scope of change
- New service equipment: dt_window_1/2 (dt_service), counter_pos_1/2 (counter_service), pickup_shelf_1 (pickup_service).
- Per-order intake task (DT 40s, lobby 35s; mobile/delivery skip — ordered remotely). Kitchen tasks depend on intake completion.
- Per-order handoff task queued lazily once all items plate (DT 35s, FC 12s, mobile 10s, delivery 20s). Ticket completes only after handoff.
- CoverageFactor cases for drive_thru/lobby/pickup stations: unmanned window = zero throughput.
- ExpectedTicketSeconds includes service phases per channel.
- Intake/handoff seconds in SimConfig (Intouch DT decomposition; operator_calibration_required).

## Acceptance
- Headless self-test suite passes for all 10 scenarios, seed 12345 (F9 gate in-game).
- Schema contract intact: envelope fields, split item lifecycle, staff/waste reason enums, no item.sold.
- Inventory ledger equation reconciles for every component.
- Determinism: identical replay for identical (config, scenario, seed).
