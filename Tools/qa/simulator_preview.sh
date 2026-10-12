#!/bin/bash
# Development iOS Simulator preview of this checkout on its own QA simulator, under its own QA app identity, with
# genuine runtime screenshots, console output, the performance report and the QA save. Fresh export folder every run.
# Never installs on another simulator, never touches com.flutterly.gravitile or its saves, never uploads.
#
#   Tools/qa/simulator_preview.sh [output-directory]
#
# Options (environment): LIFELINE_QA_BUNDLE (default com.flutterly.gravitile.qa), LIFELINE_QA_UDID (an owned QA
# simulator, for example the reviewer's C5FFBFF1-F652-4F82-A3B8-FA453B16610A), LIFELINE_QA_DEVICE (name used when no UDID
# is given; default "Little Lifeline QA 4.3 (23) Review"), LIFELINE_QA_DEVICE_TYPE (simctl device type id, only for a
# new device), LIFELINE_QA_FRESH=1 to start this QA app's save from scratch (needed for the opening checkpoints; the QA
# save is kept otherwise), LIFELINE_QA_SKIP_EXPORT=1 with LIFELINE_QA_APP=<path.app> to reuse a built app,
# LIFELINE_QA_PERF_SECONDS (default 95).
set -uo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PROJECT="$ROOT/Unity/OrbitOrchard"
UNITY="${LIFELINE_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity}"
BUNDLE="${LIFELINE_QA_BUNDLE:-com.flutterly.gravitile.qa}"
DEVICE_NAME="${LIFELINE_QA_DEVICE:-Little Lifeline QA 4.3 (23) Review}"
COMMIT="$(git -C "$ROOT" rev-parse --short HEAD 2>/dev/null || echo unknown)"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
if [ -d "$ROOT/../claude-work" ]; then BASE="$(cd "$ROOT/../claude-work" && pwd)/runs"; else BASE="$ROOT/build/qa-runs"; fi
OUT="${1:-$BASE/$STAMP-$COMMIT-simulator}"
mkdir -p "$OUT/shots" "$OUT/console" "$OUT/crashes"
log() { echo "[$(date -u +%H:%M:%S)] $*" | tee -a "$OUT/steps.log"; }
fail() { log "STOPPED: $*"; exit 1; }

case "$BUNDLE" in com.flutterly.gravitile) fail "Refusing to use the App Store identity for QA";; esac
case "$DEVICE_NAME" in *QA*) ;; *) fail "The QA simulator name must contain QA";; esac
git -C "$ROOT" rev-parse HEAD > "$OUT/source-commit.txt" 2>/dev/null

# 1. Unity export into a fresh folder (overwriting an export has crashed Unity's Xcode bridge before).
EXPORT="$OUT/xcode"
if [ "${LIFELINE_QA_SKIP_EXPORT:-0}" != 1 ]; then
  [ -e "$EXPORT" ] && fail "Export folder already exists: $EXPORT"
  if [ -f "$PROJECT/Temp/UnityLockfile" ] && lsof "$PROJECT/Temp/UnityLockfile" >/dev/null 2>&1; then fail "Another Unity Editor has this project open"; fi
  log "Unity export → $EXPORT"
  ORCHARD_IOS_EXPORT="$EXPORT" ORCHARD_BUNDLE_ID="$BUNDLE" "$UNITY" -batchmode -nographics -quit -projectPath "$PROJECT" -buildTarget iOS \
    -executeMethod OrbitOrchard.Editor.OrchardBuild.BuildIOSSimulator -logFile "$OUT/unity-export.log"
  CODE=$?; echo "$CODE" > "$OUT/unity-export.exit"; [ "$CODE" = 0 ] || fail "Unity export exit $CODE (see unity-export.log)"
  log "xcodebuild (Debug, iphonesimulator)"
  xcodebuild -project "$EXPORT/Unity-iPhone.xcodeproj" -scheme Unity-iPhone -configuration Debug -sdk iphonesimulator \
    -destination "generic/platform=iOS Simulator" -derivedDataPath "$OUT/DerivedData" \
    CODE_SIGNING_ALLOWED=NO COMPILER_INDEX_STORE_ENABLE=NO -jobs 4 build > "$OUT/xcodebuild.log" 2>&1
  CODE=$?; echo "$CODE" > "$OUT/xcodebuild.exit"; [ "$CODE" = 0 ] || fail "xcodebuild exit $CODE (see xcodebuild.log)"
  APP=""; for candidate in "$OUT"/DerivedData/Build/Products/Debug-iphonesimulator/*.app; do [ -d "$candidate" ] && { APP="$candidate"; break; }; done
else
  APP="${LIFELINE_QA_APP:-}"
fi
[ -d "$APP" ] || fail "No built app"
/usr/libexec/PlistBuddy -c "Print :CFBundleIdentifier" -c "Print :CFBundleShortVersionString" -c "Print :CFBundleVersion" "$APP/Info.plist" > "$OUT/app-identity.txt"
[ "$(head -1 "$OUT/app-identity.txt")" = "$BUNDLE" ] || fail "Built app identity $(head -1 "$OUT/app-identity.txt") is not $BUNDLE"
log "App $(tr '\n' ' ' < "$OUT/app-identity.txt")"

# 2. The dedicated QA simulator: the given UDID, else found by name, else created. Its name must say QA. No other device
#    is booted, shut down or changed.
UDID="${LIFELINE_QA_UDID:-}"
if [ -n "$UDID" ]; then
  NAME_OF_UDID="$(xcrun simctl list devices -j | /usr/bin/python3 -c 'import json,sys
u=sys.argv[1]
for runtime in json.load(sys.stdin)["devices"].values():
    for d in runtime:
        if d["udid"]==u: print(d["name"]); sys.exit()' "$UDID")"
  case "$NAME_OF_UDID" in *QA*) log "Using owned QA simulator $UDID ($NAME_OF_UDID)";; *) fail "Simulator $UDID is not a QA device (${NAME_OF_UDID:-not found})";; esac
else
UDID="$(xcrun simctl list devices -j | /usr/bin/python3 -c 'import json,sys
name=sys.argv[1]
for runtime in json.load(sys.stdin)["devices"].values():
    for d in runtime:
        if d["name"]==name and d.get("isAvailable",True): print(d["udid"]); sys.exit()' "$DEVICE_NAME")"
fi
if [ -z "$UDID" ]; then
  RUNTIME="$(xcrun simctl list runtimes -j | /usr/bin/python3 -c 'import json,sys
rs=[r for r in json.load(sys.stdin)["runtimes"] if r.get("platform")=="iOS" and r.get("isAvailable")]
print(sorted(rs,key=lambda r:[int(x) for x in r["version"].split(".")])[-1]["identifier"])')"
  TYPE="${LIFELINE_QA_DEVICE_TYPE:-$(xcrun simctl list devicetypes -j | /usr/bin/python3 -c 'import json,sys
ts=[t for t in json.load(sys.stdin)["devicetypes"] if t["name"].startswith("iPhone") and t["name"].endswith(" Pro")]
print(ts[-1]["identifier"])')}"
  UDID="$(xcrun simctl create "$DEVICE_NAME" "$TYPE" "$RUNTIME")" || fail "Could not create the QA simulator"
  log "Created QA simulator $UDID ($TYPE, $RUNTIME)"
fi
echo "$UDID" > "$OUT/device-udid.txt"
xcrun simctl boot "$UDID" >/dev/null 2>&1
xcrun simctl bootstatus "$UDID" -b > "$OUT/bootstatus.txt" 2>&1 || fail "QA simulator did not boot"
if [ "${LIFELINE_QA_FRESH:-0}" = 1 ]; then xcrun simctl uninstall "$UDID" "$BUNDLE" >/dev/null 2>&1; log "Fresh QA save: removed only $BUNDLE from this QA device"
else log "Keeping the existing $BUNDLE save on this QA device (LIFELINE_QA_FRESH=1 starts from the opening)"; fi
xcrun simctl install "$UDID" "$APP" || fail "Install failed"
CRASH_MARK="$OUT/.started"; touch "$CRASH_MARK"

# 3. Checkpoints. Each launch is the real app; QA switches only exist in development builds.
shot() {
  local name="$1" wait="$2"; shift 2
  xcrun simctl terminate "$UDID" "$BUNDLE" >/dev/null 2>&1; sleep 1
  local envs=(); for pair in "$@"; do envs+=("SIMCTL_CHILD_$pair"); done
  # macOS bash 3.2 treats an empty array as unbound under set -u, hence the ${envs[@]+...} form.
  env ${envs[@]+"${envs[@]}"} xcrun simctl launch --stdout="$OUT/console/$name.out" --stderr="$OUT/console/$name.err" "$UDID" "$BUNDLE" > "$OUT/console/$name.launch" 2>&1
  sleep "$wait"
  # Capture the whole list first: with pipefail, grep -q closing the pipe early made launchctl exit 141 (SIGPIPE) and a
  # running app was reported as not running.
  local services; services="$(xcrun simctl spawn "$UDID" launchctl list 2>/dev/null)"
  if [[ "$services" == *"UIKitApplication:$BUNDLE"* ]]; then
    xcrun simctl io "$UDID" screenshot --type=png "$OUT/shots/$name.png" >/dev/null 2>&1 && log "shot $name ($*)"
  else
    log "NOT RUNNING after ${wait}s: $name ($*); see console/$name.err and crashes/"
  fi
}
# A fresh save opens with Maya's story: her first line, the name question, then where to start (named "Sam" here).
shot 01-opening-fresh 14
shot 01a-story-name 10 CLINIC_QA_STORY=name
shot 01b-story-where-to-start 10 CLINIC_QA_STORY_NAME=Sam CLINIC_QA_STORY=reply
shot 01c-story-office-together 10 CLINIC_QA_OPENING=SetUpOffice
shot 02-opening-first-check-in 10 CLINIC_QA_OPENING=FirstCheckIn
shot 03-opening-hire-nurse 10 CLINIC_QA_OPENING=HireNurse
shot 03a-story-hire-maya 10 CLINIC_QA_OPEN=opening
shot 04-clinic-open 12 CLINIC_QA_OPENING=Complete CLINIC_QA_ADVANCE=45
shot 04a-story-so-far 8 CLINIC_QA_OPEN=settings:story
shot 05-first-upgrade-panel 10 CLINIC_QA_OPEN=FirstAid
shot 07-free-rewards-today 8 CLINIC_QA_OPEN=gems:today
shot 08-shop 8 CLINIC_QA_OPEN=gems:shop
shot 09-shop-extras 8 CLINIC_QA_OPEN=gems:extras
shot 10-confirm-short-of-gems 8 CLINIC_QA_CONFIRM=Boost:boost.4h
shot 11-confirm-boost 8 CLINIC_QA_GEMS=120 CLINIC_QA_CONFIRM=Boost:boost.1h
shot 12-confirm-decor 8 CLINIC_QA_CONFIRM=Decoration:Reception
shot 13-goals-tab 8 CLINIC_QA_OPEN=gems:goals
shot 14-progressed-clinic 20 CLINIC_QA_PLAY=6
shot 15-guide-optional-lesson 10 CLINIC_QA_GUIDE=1
shot 16-confirm-finish-now 10 CLINIC_QA_COINS=5000000 CLINIC_QA_RENOVATE=Waiting CLINIC_QA_CONFIRM=FinishWork:first
shot 17-relaunch-same-clinic 12
shot 18-welcome-back 12 CLINIC_QA_AWAY=7200

# 4. Performance: a loaded, progressed clinic left running; the recorder writes every 30 active seconds.
PERF="${LIFELINE_QA_PERF_SECONDS:-95}"
shot 19-performance-sample "$PERF"
xcrun simctl terminate "$UDID" "$BUNDLE" >/dev/null 2>&1; sleep 2
DATA="$(xcrun simctl get_app_container "$UDID" "$BUNDLE" data 2>/dev/null)"
if [ -n "$DATA" ]; then
  if cp "$DATA/Documents/clinic-performance-v1.json" "$OUT/simulator-performance.json" 2>/dev/null; then
    log "Performance report copied (simulator, not device evidence)"
    /usr/bin/python3 - "$OUT/simulator-performance.json" <<'PY' | tee "$OUT/simulator-performance-summary.txt"
import json, sys
r = json.load(open(sys.argv[1]))
mb = lambda b: "%.0f MB" % (b / 1048576.0)
print("SIMULATOR ONLY, not device evidence. app %s, Unity %s, %dx%d, target %s fps" % (r.get("appVersion"), r.get("unityVersion"),
      r.get("screenWidth", 0), r.get("screenHeight", 0), r.get("targetFrameRate")))
for s in r.get("samples") or []:
    print("t=%5.0fs frames=%4d median %.2f ms  p95 %.2f ms  max %.2f ms  unity %s  managed %s  thermal %s" % (
        s["elapsedActiveSeconds"], s["windowFrameCount"], s["medianFrameMs"], s["p95FrameMs"], s["maximumFrameMs"],
        mb(s["unityAllocatedBytes"]), mb(s["managedUsedBytes"]), s["thermalState"]))
print("peak sampled: unity %s, managed %s; max thermal state %s" % (mb(r.get("peakSampledUnityAllocatedBytes", 0)),
      mb(r.get("peakSampledManagedUsedBytes", 0)), r.get("maximumThermalState")))
PY
  fi
  cp "$DATA/Documents/idle-clinic-profile-v1.json" "$OUT/qa-save.json" 2>/dev/null && log "QA save copied"
fi
# 5. The doctors clinic, last because it moves this QA save there: the starter clinic is completed through its real rules
#    (QA coins, building time skipped) and the shared wallet moves across; then a plain relaunch must return to it.
shot 20-doctors-clinic 14 CLINIC_QA_DOCTORS=1
shot 21-doctors-relaunch 12
xcrun simctl terminate "$UDID" "$BUNDLE" >/dev/null 2>&1; sleep 2
[ -n "$DATA" ] && cp "$DATA/Documents/idle-clinic-profile-v1.json" "$OUT/qa-save-doctors.json" 2>/dev/null && log "Doctors clinic QA save copied"
# 6. Crash reports written during this run that name the app.
find "$HOME/Library/Logs/DiagnosticReports" -newer "$CRASH_MARK" \( -iname "*LittleLifeline*" -o -iname "*Little Lifeline*" -o -iname "*UnityFramework*" \) -exec cp {} "$OUT/crashes/" \; 2>/dev/null
log "Crash reports from this run: $(ls "$OUT/crashes" | wc -l | tr -d ' ')"
log "Done. Evidence: $OUT"
