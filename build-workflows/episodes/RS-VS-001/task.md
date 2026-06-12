# RS-VS-001 — Day/night, KDS vitals, FX, props, variety

**Date:** 2026-06-11  ·  **Status:** complete  ·  **Workflow:** build-workflows v1

## Objective
Make the 3D world readable and alive: a sun that tracks the simulated clock,
floating KDS vitals above the line, overload beacons, fryer steam, carry props,
brake lights, and body/height variety.

## Scope of change
- Main.UpdateDayNight: sun elevation/azimuth/energy/color over the 06:00-24:00 arc; sky color lerps to night after ~20:30; lot pole lights and four interior ceiling lights carry the scene after dark.
- VitalsAndFx node: Label3D KDS boards above grill/fryer/assembly/expo/beverage showing backlog minutes + hold levels with oldest-batch age; boards turn amber on strain; blinking red beacon over the active overloaded station; CPU-particle steam over a busy fryer.
- CustomerAgent carries a tray + drink after pickup (couriers a bag); CarAgent gains 3 body variants and brake lights that glow only while held in queue; CharacterRig height variance 0.92-1.08.
- AgentManager subscribes TicketAbandonedEvt so abandoning guests/cars leave instead of waiting forever.

## Acceptance
- Headless self-test suite passes for all 10 scenarios, seed 12345 (F9 gate in-game).
- Schema contract intact: envelope fields, split item lifecycle, staff/waste reason enums, no item.sold.
- Inventory ledger equation reconciles for every component.
- Determinism: identical replay for identical (config, scenario, seed).
