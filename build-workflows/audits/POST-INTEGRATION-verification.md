# Post-integration verification pass — 2026-06-11

Independent checks run after the six-task integration (RS-FE/HQ/GP/VS/CF/IN-001),
covering the surfaces the headless suite cannot reach.

## 1. Determinism replay proof
Two independent full-day runs per scenario, SHA-256 over the complete event
stream + headline metrics:

| scenario | run1 == run2 | different seed differs |
|---|---|---|
| normal_day | yes (EAB1B830…) | yes |
| rush_day | yes (80A51858…) | yes |
| staffing_call_off | yes (C2600604…) | yes |

Same (config, scenario, seed) → byte-identical stream. Replay contract holds
with all new systems (hold pans, abandonment, decisions, wear, inspections) live.

## 2. Godot-layer member resolution audit
The visual/UI layer cannot compile in the build container, so every
`sim.<member>` reference in the 31 Godot-side files was mechanically extracted
and resolved against the compiled SimRunState public surface: **82/82 resolve**
(206 public members). SimConfig references: 30/30 resolve. This eliminates the
largest class of in-editor compile failure for code written blind.

## 3. Wiring spot-checks
- CameraDirector free-cam field (`_free`) verified present before IsFreeHigh use.
- config/*.json confirmed inside the Godot project root (res:// resolves).
- Brace/paren balance verified on the two new uncompiled files.

## Remaining (requires Godot editor)
In-editor smoke test: day/night sweep at 10x, station click → panel, decision
toast answer, KDS boards updating, F9 PASS in-game.
