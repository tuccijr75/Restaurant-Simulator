# RS-CX-001 handoff

New files: profiles/compatibility.json, tests/test_compatibility.py.
Run locally: `python -m unittest discover -s tests` (expects prior 21 + 14 new).
Point at any export: set RS_COMPAT_BUNDLES to one or more bundle dirs
(user://outputs/sim_<scenario>_<seed> globalized path after an F5 export).
Defect trail: RS-CX-001-D1 -> RS-EV-001 (SimEvent generator_version sync).
IMPORTANT: repo game/ predates RS-FIELD-001 — sync from the delivered bundle,
or the TAB focus bug and the breakfast waste/raw-starvation behavior will
persist in field runs and exports will stamp game-0.2.0.
Next recommended task: RS-RM-001 (multi-day career mode: reputation carryover,
persistent seeded weeks) for ASC longitudinal validation.
