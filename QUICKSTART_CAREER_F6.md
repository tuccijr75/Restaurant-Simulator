# Career Mode — why F6 did nothing, and the one-step fix

## What went wrong
Your Godot logs contained no `[CareerHook]` lines and your F5 export was
`sim_normal_day_12345` (the default seed). That means the career hook never
loaded — the autoload was not registered, so F6 had nothing listening for it.
(The previous build also asked you to hand-edit Main.cs; that's no longer needed.)

## The fix — register ONE autoload (no code edits)
1. Merge this drop-in over your game/ tree (overwrites CareerHook.cs + SimRunState.cs).
2. In Godot 4.6: **Project ▸ Project Settings ▸ Autoload**.
3. Path: `res://game/scripts/sim/CareerHook.cs`
4. Node Name: `CareerHook`  →  click **Add**  →  ensure **Enable** is checked.
5. Run the project. The Output panel should print:

       [CareerHook] ready — week_seed=777001 day=0/7 reputation=70.0 ...
       [CareerHook] configured day 0: scenario=normal_day seed=132195522 demand_mult=0.900 ...

## Use it
- **SPACE** starts the shift (as before). Let the day run (or run partial).
- **F6** = advance the day: folds the day's csat/inspection/abandonment into
  reputation, saves user://career/career_state.json, reloads for the next day.
- **F7** = reset the week.
- **F5** = export. After registering the autoload the bundle's `simulation_id`
  will read `sim_normal_day_132195522` for day 0 — NOT `..._12345`. That changed
  seed is your confirmation the career day was configured.

## Why F6 works now (it didn't before)
CareerHook is an autoload and reads input via `_Input`, which runs ahead of
Main's focus-interceptor in tree order — so F6 reaches the hook before Main can
consume it. It also locates the SimRunState by reflection, so no Main.cs changes
are required.

## To turn career mode off
Remove the `CareerHook` autoload entry. Runs revert to Main's default seed.

---

## Update: real ingredients + crash-proof export (RS-IM-001)
With the CareerHook autoload registered, every run now uses the **real
per-ingredient model** (34 ingredients, each with its own hold time/temp/cost) —
no setup needed. Waste is now realistic and per-item (fries, proteins age out;
buns/packaging/frozen/cheese don't), and exports include a new
`ingredient_ledger.json`.

New key:
- **F8** = hardened export. Writes all nine contract files to
  `user://outputs/sim_<scenario>_<seed>/`, creating the folder first and
  null-checking every write — so it can't crash the way F5 did. Use F8 if F5
  still fails. The Output panel prints the exact folder path.

If F5 still crashes: it's almost certainly `Main`'s own export writer hitting a
missing `user://outputs` directory (Godot returns a null FileAccess, then NREs).
Paste the Godot console output at the crash and it's a ~2-line fix in Main. F8
works regardless.
