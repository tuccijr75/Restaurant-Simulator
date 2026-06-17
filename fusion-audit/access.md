# Fusion Repository Access Verification

Do not audit or recommend changes yet.

Use only these immutable, commit-pinned evidence URLs:

Manifest:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/manifest.md

Core evidence:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/core.xml

Godot and C# evidence:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/game.xml

Python and test evidence:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/python.xml

Tracked repository tree:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/tree.txt

Snapshot status:
https://raw.githubusercontent.com/tuccijr75/Restaurant-Simulator/a9927212d5e1a3e54138f302fc223af6d1f7d963/fusion-audit/status.txt

Open the manifest first, then retrieve every required evidence file.

Return:

1. Exact repository.
2. Exact source commit SHA.
3. Every evidence URL successfully retrieved.
4. Current task ID, lane, and status.
5. Actual runtime frameworks and languages.
6. Three exact C# or Godot repository paths.
7. Three exact Python or test repository paths.
8. The ending marker from every evidence file.
9. Any inaccessible or truncated file.

Every answer must cite the exact evidence URL and repository path.

If any required file cannot be opened completely, or any ending marker is missing, return exactly:

ACCESS_VERIFICATION_FAILED

List the missing evidence and stop. Do not infer, estimate, use cached summaries, or substitute a typical repository structure.

END_OF_ACCESS_PROMPT
