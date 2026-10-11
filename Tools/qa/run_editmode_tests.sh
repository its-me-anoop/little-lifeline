#!/bin/bash
# Run the Unity EditMode suite for this checkout and keep the evidence together: result XML, Unity log, the exact
# command, source commit/status, duration and exit code. Read-only for every other checkout and save.
#
#   Tools/qa/run_editmode_tests.sh [output-directory]
#
# Default output: <checkout>/../claude-work/runs/<UTC stamp>-<commit>-editmode when that folder exists (the task-2
# workspace), otherwise <checkout>/build/qa-runs/... (ignored by git).
set -u
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PROJECT="$ROOT/Unity/OrbitOrchard"
UNITY="${LIFELINE_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity}"
COMMIT="$(git -C "$ROOT" rev-parse --short HEAD 2>/dev/null || echo unknown)"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
if [ -d "$ROOT/../claude-work" ]; then BASE="$(cd "$ROOT/../claude-work" && pwd)/runs"; else BASE="$ROOT/build/qa-runs"; fi
OUT="${1:-$BASE/$STAMP-$COMMIT-editmode}"
mkdir -p "$OUT"

if [ ! -x "$UNITY" ]; then echo "Unity 6000.3.24f1 not found at $UNITY (set LIFELINE_EDITOR)." | tee "$OUT/blocked.txt"; exit 3; fi
if [ -f "$PROJECT/Temp/UnityLockfile" ] && lsof "$PROJECT/Temp/UnityLockfile" >/dev/null 2>&1; then
  echo "Another Unity Editor has $PROJECT open. Close it, then run again." | tee "$OUT/blocked.txt"; exit 3
fi
# An empty Library can be seeded from another clone with an APFS copy-on-write clone: instant, no extra disk, and the
# source is only read. Example: LIFELINE_LIBRARY_SEED=/path/to/other/Unity/OrbitOrchard/Library
if [ ! -d "$PROJECT/Library" ] && [ -n "${LIFELINE_LIBRARY_SEED:-}" ] && [ -d "$LIFELINE_LIBRARY_SEED" ]; then
  cp -Rc "$LIFELINE_LIBRARY_SEED" "$PROJECT/Library" && echo "Library seeded from $LIFELINE_LIBRARY_SEED" > "$OUT/library-seed.txt"
fi

CMD=("$UNITY" -batchmode -nographics -projectPath "$PROJECT" -buildTarget iOS -runTests -testPlatform EditMode
     -testResults "$OUT/editmode.xml" -logFile "$OUT/editmode.log")
printf '%q ' "${CMD[@]}" > "$OUT/command.txt"; echo >> "$OUT/command.txt"
git -C "$ROOT" rev-parse HEAD > "$OUT/source-commit.txt" 2>/dev/null
git -C "$ROOT" status --porcelain --untracked-files=no > "$OUT/source-status.txt" 2>/dev/null
START=$(date -u +%s)
"${CMD[@]}"; CODE=$?
echo "$CODE" > "$OUT/exit-code.txt"
echo "$(( $(date -u +%s) - START ))" > "$OUT/seconds.txt"
if [ -f "$OUT/editmode.xml" ]; then
  /usr/bin/python3 - "$OUT/editmode.xml" <<'PY' | tee "$OUT/summary.txt"
import sys, xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
print("result=%s total=%s passed=%s failed=%s skipped=%s duration=%s" % tuple(root.get(k) for k in ("result", "total", "passed", "failed", "skipped", "duration")))
for case in root.iter("test-case"):
    if case.get("result") == "Failed":
        message = case.find("./failure/message")
        text = (message.text or "").strip().splitlines() if message is not None and message.text else [""]
        print("FAILED", case.get("fullname"), "::", text[0][:300])
PY
else
  { echo "No result XML (exit $CODE). Compiler errors, if any:"; grep -E "error CS[0-9]+" "$OUT/editmode.log" | sort -u | head -40; } | tee "$OUT/summary.txt"
fi
echo "Evidence: $OUT"
exit $CODE
