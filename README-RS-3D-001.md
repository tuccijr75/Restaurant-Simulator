# RS-3D-001 drop-in — what's in this package

Merge this folder over the repo root (it only touches the paths listed in
`build-workflows/episodes/RS-3D-001/receipt.json`).

1. `game/` — patched engine (deterministic fixed timestep, reconciling ledgers, measured
   SOS, batch-cycle cook model, full export contract) + new 3D world, 11-camera CCTV
   system, animated employees/customers/vehicles, CCTV HUD. Open in **Godot 4.6** and run.
   The old 2D dashboard is on **TAB**, sharing the same sim.
2. `tools/engine-selftest/` — `dotnet run` replays all 10 scenarios twice and prints the
   pass/fail gate (90/90 PASS at seed 12345 in CI here).
3. `build-workflows/` — RS-3D-001 brief, audit (full findings F-01..F-18), handoff,
   episode package, receipt; updated `state.json` / `current-task.md`.
4. `docs/07_3D_WORLD_AND_CAMERAS.md` — world/camera/controls spec.

Controls: SPACE start/pause · 1-9,0/O cameras · C next · T tour · F free cam · TAB
dashboard · F9 self-test · F5 export.

Recommended next: delete `restaurant-simulator-(4.3)/` (stale prototype, audit F-14) and
run RS-CF-001 to wire the config JSONs into the engine (F-17/F-18).
