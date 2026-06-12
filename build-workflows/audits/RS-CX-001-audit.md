# RS-CX-001 audit — 2026-06-12

## Validation of prior (ChatGPT/Jarvis) work — sandbox limits stated honestly
The RS-PL-001 artifacts (profiles/plausibility.json, tests/test_plausibility.py,
restaurant_simulator/*.py) are not present in this build sandbox and were not
uploaded, so the "21 tests OK" claim could not be independently re-run here.
What WAS validated: the full game/ source set Michael pasted from the repo.
Finding: **the repo is running the pre-RS-FIELD-001 engine** — none of the ten
field-fix markers are present (TAB _Input interceptor + KillFocus, sell-through
pan par + cycle-aware batch floor, prep FIFO sell-through, fries bagging at
assembly, supply runs carrying Raw, game-0.3.0/RS-IN-001 stamps). This explains
the recurring TAB focus report and the game-0.2.0 export receipts. Action: sync
game/ from the delivered bundle before further field runs.

## RS-CX-001 execution
Created profiles/compatibility.json (declarative contract) and
tests/test_compatibility.py (stdlib-only, multi-bundle). Ran against four real
bundles: three freshly generated full-day exports (normal_day,
staffing_call_off, equipment_failure; seed 12345; 8,613–9,188 events each) and
Michael's actual field export (partial day, 1,958 events, game-0.2.0).

## Defect found and recorded (not silently patched in-scope)
**RS-CX-001-D1:** generator_version was dual-sourced — Exports.cs said
game-0.3.0 while SimEvent.cs still hardcoded game-0.2.0 in every event
envelope, so event lines disagreed with bundle provenance blocks. ASC's
provenance-consistency gate would quarantine such runs. The compatibility suite
caught it on first execution (exactly its job). Fixed under follow-up task
RS-EV-001 (one-line SimEvent.cs version sync); bundles regenerated.

## Results
- Python: 14/14 OK across all four bundles (the 0.2.0 field bundle passes —
  it is internally consistent; consistency, not a specific version, is the contract).
- C# gate: full 10-scenario SelfTest suite rerun after RS-EV-001 — see
  episodes/RS-CX-001/selftest-final.txt.
- Test-harness note: initial run used subTest inside a generator, which
  misreports failures under Python 3.12; refactored to plain iteration with
  bundle-named assertion messages.

## Status: READY FOR REVIEW (sandbox evidence complete; on-machine
`python -m unittest discover -s tests` + `git status --short` still owed).
