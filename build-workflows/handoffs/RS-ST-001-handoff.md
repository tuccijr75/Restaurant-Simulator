# Handoff — RS-ST-001

## Files Modified

- game/scripts/sim/SimRunState.cs — ScheduledCrewAt curve; ApplySchedule (evented adds/drops/breaks/call-off/replacement); AutoPlanCoverage; opening-roster shift_start events; AutoSchedule manual-override flag on all player staffing actions; EnableAutoSchedule(); StaffReason enum pass-through fix; staffing_call_off 0.74 multiplier removed (causal now); Crew opening level 6→4 (matches 06:00 schedule)
- game/scripts/ui/LaborPanel.cs — mode line (AUTO SCHEDULE / MANUAL) + "Auto Schedule" resume button

## Verification

90/90 self-test PASS (10 scenarios, deterministic); staffing ledger now carries 42–43 evented assignments/day; call-off scenario shows 11:00 call_off → 14:00 rush_support replacement; all sample bundles re-validated (JSON, hashes, ledger equation).

## Behavior Notes

- Auto schedule is on by default; any manual staffing/coverage action takes over until "Auto Schedule" is pressed.
- 3D employees follow the schedule automatically (they mirror coverage values).
- slow_day now correctly fails the labor gate (33.2% — overstaffed for volume): expected, it is the gate doing its job.

## UI Playability Pass (post-screenshot)

- Hud3D rebuilt on anchored containers with backdrop strips — fixes the overlapping/clipped top-left text (camera tag was mis-positioned after an anchor preset) and adapts to any window size; pulsing "PRESS SPACE TO START" prompt added; F9 now toggles the report panel closed.
- Menu board moved up/back out of CAM-06's sightline; CAM-06 lowered to look under it; CAM-07 widened to sweep counter + lobby.
- Roof/parapets grouped and hideable: R toggles, and the roof auto-hides for the overhead camera and a high free cam (so CAM-11 OVERHEAD actually sees the floorplan).
- project.godot starts maximized (window/size/mode=2). Note: Godot 4.6 embeds the running game inside the editor by default — for the full play area, run windowed via the Make Floating option or an exported build.

## Next Recommended Task

RS-FE-001 — front-end service work model (order-taking/payment labor per channel) to restore causal peak-hour SOS strain; then RS-CF-001 (wire config JSONs, F-17/F-18).
