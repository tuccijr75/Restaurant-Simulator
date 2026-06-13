# RS-IM-001 + RS-RM-001 drop-in (rev 2)

Real per-ingredient inventory/hold/waste model + multi-day career mode.

## rev 2 fixes (from first in-editor F8 export)
- Zero phantom waste (perishables produced just-in-time, not pre-seeded).
- Burgers consume lettuce + tomato.
- With real model on: legacy "prep quality low" alerts suppressed, end_of_shift
  waste headline = real per-item values, inspection driven by real model.

## Setup (one step)
Project > Project Settings > Autoload > add res://game/scripts/sim/CareerHook.cs
as "CareerHook", Enable. Real ingredients then auto-enable every run.
F6 advance day, F7 reset week, F8 hardened export. See QUICKSTART_CAREER_F6.md.

## Verify
    cd tools/engine-selftest && dotnet run -c Release   # 120/120 + 7/7 + 11/11, PASS
