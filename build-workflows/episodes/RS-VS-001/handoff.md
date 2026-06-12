# RS-VS-001 — Handoff

**Date:** 2026-06-11

## What landed
- Main.UpdateDayNight: sun elevation/azimuth/energy/color over the 06:00-24:00 arc; sky color lerps to night after ~20:30; lot pole lights and four interior ceiling lights carry the scene after dark.
- VitalsAndFx node: Label3D KDS boards above grill/fryer/assembly/expo/beverage showing backlog minutes + hold levels with oldest-batch age; boards turn amber on strain; blinking red beacon over the active overloaded station; CPU-particle steam over a busy fryer.
- CustomerAgent carries a tray + drink after pickup (couriers a bag); CarAgent gains 3 body variants and brake lights that glow only while held in queue; CharacterRig height variance 0.92-1.08.
- AgentManager subscribes TicketAbandonedEvt so abandoning guests/cars leave instead of waiting forever.

## How to verify
1. Open game/ in Godot 4.6 (.NET), run, press F9 — expect PASS for the loaded scenario.
2. Headless: tools/engine-selftest (dotnet run) — 10-scenario suite.
3. F5 exports the 8-file contract to user://outputs/sim_{scenario}_{seed}.

## Known limits / next
- Visual layer compiles only in Godot — smoke-test in-editor (day/night sweep, KDS boards, click panels, decision toast).
- Calibration constants marked operator_calibration_required throughout SimConfig.
- Auto-policy hold waste (grilled ~32% in shoulders) is the headroom a Manager Mode player can beat.
