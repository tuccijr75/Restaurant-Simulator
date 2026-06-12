# 07 — 3D World, Camera Coverage, and Character Layer

## Purpose

Defines the 3D presentation layer added in RS-3D-001. It is strictly read-only over the
deterministic simulation core: agents and cameras subscribe to `SimRunState` events and
read public state; nothing in the 3D layer writes back, so deterministic replay is
unaffected by rendering, frame rate, or visual randomness.

## Boot

`scenes/Main.tscn` → `scripts/Main.cs` (Node3D). Main owns the single `SimRunState`
(`ExternallyDriven = true`), advances it on fixed 1-sim-second ticks, builds the world,
cameras, agents, and HUD, and embeds the existing 2D operations dashboard (TAB) sharing
the same sim instance.

## World (procedural, no imported assets)

Interior: grill, fryer bank, prep table, walk-in cooler, assembly rail, beverage tower,
expo with heat-lamp glow, office/break room, front counter with two POS, mobile pickup
shelf, menu board, six dining tables, trash. Exterior: parking lot with striped spaces,
sidewalk, glass storefront, roof/parapet/pole signage, landscaping, lot lighting,
drive-thru lane with order board + canopy and a window cut in the east wall.

## Camera coverage (every station covered)

| Key | Camera | Covers |
|---|---|---|
| 1 | CAM-01 GRILL | grill |
| 2 | CAM-02 FRYER | fryer bank |
| 3 | CAM-03 PREP/WALK-IN | prep table + walk-in cooler |
| 4 | CAM-04 ASSEMBLY | assembly rail |
| 5 | CAM-05 BEV/EXPO | beverage tower + expo |
| 6 | CAM-06 FRONT COUNTER | POS, counter, queue |
| 7 | CAM-07 LOBBY/DINING | dining room, entrance interior |
| 8 | CAM-08 DT WINDOW | drive-thru window handoff |
| 9 | CAM-09 DT LANE/BOARD | lane, order board, canopy |
| 0 | CAM-10 LOT/ENTRANCE | parking, storefront, door |
| O | CAM-11 OVERHEAD | full floorplan |
| F | FREE CAM | WASD + QZ, Shift fast, mouse look, ESC release |

`C` next camera · `T` auto-tour (7 s per camera). Each mounted camera has a visible
wall-mount box; the HUD shows the CCTV tag with blinking REC and the simulated timestamp.

## Characters and vehicles

- Employees: procedural humanoid rigs mirrored from live coverage values
  (kitchen/fryer/drive/counter/prep) plus managers near the office; crew shirts red,
  team-lead black, managers white. They animate "working" while their station has load,
  run occasional walk-in supply trips, and sit out breaks in the break room
  (`CrewOnBreak`).
- Walk-in customers spawn on lobby/mobile `order.created`, queue at the registers or the
  pickup shelf, collect on ticket completion, and either dine (35%) or exit. Delivery
  couriers arrive in orange.
- Drive-thru cars follow the lane with car-following spacing, pause at the order board,
  and hold at the window until their ticket completes. Concurrency caps: 16 walk-ins,
  8 vehicles (sampling only — the sim is unaffected).

## In-engine gates

- `F9` self-test: double-run deterministic replay (event-stream + ledger SHA-256),
  lifecycle ordering, ledger reconciliation, deprecated-event scan.
- `F5` export: full 8-file README output contract to `user://outputs/sim_{scenario}_{seed}/`.
- `tools/engine-selftest/` runs the same gate headless with the .NET 8 SDK and no Godot.

## Boundary notes

All characters are synthetic role visuals; no individual performance is depicted or
recorded. Visual randomness uses a separate generator and never feeds the simulation.
