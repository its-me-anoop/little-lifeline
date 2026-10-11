# Inherited baseline (not Claude's work)

This commit preserves the uncommitted work that existed in the original checkout
`/Users/anoopjose/Projects/little-lifeline` on 2026-10-11, so that later commits on
`claude/next-dev-4.3-b23` show only the new implementation.

- Original branch `feature/core-progression`, HEAD `e6044304daa057f1243142f35dc92382b820f755`.
- 37 modified tracked files and 411 individual untracked files (43 summarised status entries).
- Every one of the 448 files was copied byte for byte and checked against
  `baseline-manifest.json` (SHA-256 and size): 448 checked, 0 mismatches.
- The original checkout was only read (`git --no-optional-locks`). It was not reset, cleaned,
  stashed, committed or written to.

## Not committed

`assets/clinic-reference/` (358 files, about 409 MB of `.blend`, `.blend1` and reference
renders) sits outside the Unity project. It is copied into this isolated working tree but
excluded from git, because several files are too large for GitHub and `.blend1` files are
Blender backups. `untracked-reference-assets.sha256` lists each file's SHA-256, size and path.

## Original stash

The original repository has one stash (`refs/stash` = `5dbd64b`, "pre-4.2 reset:
create_reception_desk.py edit"). It was not applied or dropped. Its patch is recorded in
`original-stash0.patch` for reference only.
