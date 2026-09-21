#!/bin/bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.16f1/Unity.app/Contents/MacOS/Unity}"
if [ ! -x "$editor" ]; then
  echo "Set UNITY_EDITOR to the Unity 6000.3.16f1 executable." >&2
  exit 1
fi
mkdir -p "$repo_root/TestResults"
for mode in EditMode PlayMode; do
  report="$repo_root/TestResults/$mode.xml"
  rm -f "$report"
  "$editor" -batchmode -projectPath "$repo_root/game" \
    -runTests -testPlatform "$mode" -testResults "$report" \
    -logFile "$repo_root/TestResults/$mode.log"
  python3 - "$report" <<'PY'
import sys
import xml.etree.ElementTree as ET
result = ET.parse(sys.argv[1]).getroot()
print(result.attrib)
if result.get('result') != 'Passed' or int(result.get('passed', '0')) == 0:
    raise SystemExit('Unity did not produce a passing test report.')
PY
done
