# RS-FIELD-001 — First field run feedback episode — 2026-06-12

Michael's first in-editor session produced a mid-day F5 export (221 orders, 11:05)
and one UI bug report. The export ledgers drove five causal engine fixes; this
audit records them plus an honest tooling incident.

## UI: TAB trapped by GUI focus
Clicking any panel button gave it keyboard focus; Godot's focus navigation then
consumed TAB (and SPACE re-presses the focused button) before game input saw it.
Fix is belt-and-braces: every Control in the dashboard and gameplay UI is made
non-focusable at startup (recursive walk + at-creation for dynamic buttons), and
the ui_focus_next/prev actions are unbound. The key never has a GUI consumer.

## Engine findings from the field ledgers
1. **Hold-pan tail waste** (the wall of holding_time_exceeded from 06:07): the
   auto par pre-cooked against the FULL hold window, guaranteeing the tail of
   every batch expired. By 11:05 grilled waste was 43% of production, fries 29%.
   Fix: sell-through par (70% of hold life) + partial batch loads off-peak.
2. **Mini-batch queueing spiral** (found while fixing #1): a small batch still
   costs a full cook cycle, so 2u loads at peak quarter the fryer's throughput;
   the pipeline stays "full" by unit count and every replacement batch is again
   tiny — permanent stockout at lunch with the pan pinned at zero. Fix: batch
   floor scales with demand x cycle time (full batches at peak, minis off-peak).
3. **Prep par over-prepping**: prep batches sized to 36 min of demand against a
   30-minute shelf (floor 40u) torched Raw at ~3x true burn; Raw hit zero by
   09:30 and the store starved until the 14:00 truck. Same sell-through cure:
   70% of shelf life, floor 10u. Michael's own export showed raw=0 at 10:24.
4. **Fries scoops queued behind vat cycles**: bagging competed with 180s batch
   cooks on fryer_fries units, adding up to 3 minutes per fries item exactly at
   peak. Bagging moved to assembly per the original catalog.
5. **Emergency runs missing prep raw**: SupplyWatch monitored protein/side but
   not Raw — the component surge days actually exhaust first. Runs now carry
   +200 Raw and the watch triggers on Raw<60.

## Outcome (normal_day, seed 12345; before -> after)
- Cooked hold expiry: 453u -> 31u (-93%)
- CSAT: 86.7 -> 90.5 ・ abandonment: 1.8% -> 2.2% (band ≤8%)
- rush_day: 149 -> 36 abandoned ・ multi_rush: lane-saturation strain
  (97 balked, 231s DT SOS) instead of supply collapse
- Suite: 120/120 PASS, all 10 scenarios ・ bundles regenerated and validated

## Tooling incident (recorded for trust)
A patch-apply step earlier in the session executed inconsistently (partial
double-apply), leaving the tree in a state the visible log did not describe and
briefly producing untrustworthy suite results. Recovery: md5 verification of
game-vs-harness sources, binary clean (rm bin/obj), and empirical re-derivation
of every change from diagnostics rather than memory. All fixes above were
applied as verified, idempotent patches on the cleaned tree.
