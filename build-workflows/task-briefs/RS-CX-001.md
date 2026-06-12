# RS-CX-001 — AI Shift Commander compatibility profile

Goal: prove simulator output bundles are ASC-ingestion-ready via a declarative
contract profile and an automated test suite, without integrating ASC runtime.

Scope (as briefed): profiles/compatibility.json, tests/test_compatibility.py,
workflow records. No runtime engine or Godot changes inside this task; any
compatibility defect found is recorded and fixed under its own follow-up task.

Checks implemented (1:1 with brief): bundle file contract + hash verification,
JSON/JSONL parseability, event envelope completeness (14 fields), event-type
contract incl. item.sold ban and conditional overload events, strict sequence
monotonicity + non-regressive timestamps, item lifecycle (completed never
exceeds taken; ticket completion gated on items; abandonment exception),
inventory equation re-verified independently of the reconciles flag, staffing
aggregate-only with reason enum, provenance consistency across all files AND
every event line, summary internal consistency, PII/secret/scoring deny-list,
synthetic declaration everywhere.

Bundle discovery: RS_COMPAT_BUNDLES env override or default globs; tests skip
with instructions when no bundle is present, so CI on a clean clone is safe.
