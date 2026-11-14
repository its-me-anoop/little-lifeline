#!/bin/bash
# Build the development simulator app from this checkout once, then take custom screenshots on a QA simulator.
#   Tools/qa/capture.sh <absolute-output-dir> <QA-simulator-UDID> "<name> <wait-seconds> [ENV=VALUE ...]" ...
# Reuse a built app with LIFELINE_QA_APP=<path.app>. The QA simulator's name must contain QA.
set -uo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"; OUT="$1"; UDID="$2"; shift 2
UNITY=/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity
BUNDLE=com.flutterly.gravitile.qa
mkdir -p "$OUT/shots"
APP="${LIFELINE_QA_APP:-}"
if [ -z "$APP" ]; then
  ORCHARD_IOS_EXPORT="$OUT/xcode" ORCHARD_BUNDLE_ID="$BUNDLE" "$UNITY" -batchmode -nographics -quit -projectPath "$ROOT/Unity/OrbitOrchard" -buildTarget iOS -executeMethod OrbitOrchard.Editor.OrchardBuild.BuildIOSSimulator -logFile "$OUT/unity.log" || { echo "export failed"; exit 1; }
  xcodebuild -project "$OUT/xcode/Unity-iPhone.xcodeproj" -scheme Unity-iPhone -configuration Debug -sdk iphonesimulator -destination "generic/platform=iOS Simulator" -derivedDataPath "$OUT/DerivedData" CODE_SIGNING_ALLOWED=NO COMPILER_INDEX_STORE_ENABLE=NO -jobs 4 build > "$OUT/xcodebuild.log" 2>&1 || { echo "xcodebuild failed"; exit 1; }
  APP="$(ls -d "$OUT"/DerivedData/Build/Products/Debug-iphonesimulator/*.app | head -1)"
fi
echo "$APP" > "$OUT/app-path.txt"
xcrun simctl boot "$UDID" >/dev/null 2>&1; xcrun simctl bootstatus "$UDID" -b >/dev/null 2>&1
xcrun simctl uninstall "$UDID" "$BUNDLE" >/dev/null 2>&1
xcrun simctl install "$UDID" "$APP" || exit 1
for spec in "$@"; do
  set -- $spec; name="$1"; wait="$2"; shift 2
  xcrun simctl terminate "$UDID" "$BUNDLE" >/dev/null 2>&1; sleep 1
  envs=(); for pair in "$@"; do envs+=("SIMCTL_CHILD_$pair"); done
  env ${envs[@]+"${envs[@]}"} xcrun simctl launch "$UDID" "$BUNDLE" >/dev/null 2>&1
  sleep "$wait"
  xcrun simctl io "$UDID" screenshot --type=png "$OUT/shots/$name.png" >/dev/null 2>&1 && echo "shot $name"
done
