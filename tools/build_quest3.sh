#!/bin/bash
set -euo pipefail

usage() {
  echo "Uso: bash tools/build_quest3.sh [prepare|validate|build|demo]"
  echo "Com o Editor fechado, prepara/valida Android ou gera APK de desenvolvimento/demonstração."
  echo "UNITY_EDITOR pode apontar para o executável do Unity da versão do projeto."
  echo "O comando não instala o APK no headset."
}

if [ "$#" -gt 1 ]; then usage >&2; exit 2; fi
action="${1:-build}"
case "$action" in
  prepare) method="ChessCgiQuestBuild.Prepare" ;;
  validate) method="ChessCgiQuestBuild.Validate" ;;
  build) method="ChessCgiQuestBuild.Build" ;;
  demo) method="ChessCgiQuestBuild.BuildDemo" ;;
  -h|--help) usage; exit 0 ;;
  *) usage >&2; exit 2 ;;
esac

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(awk '/^m_EditorVersion:/ {print $2}' "$repo_root/game/ProjectSettings/ProjectVersion.txt")"
editor="${UNITY_EDITOR:-/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity}"
if [ ! -x "$editor" ]; then
  echo "Defina UNITY_EDITOR com o executável do Unity $version." >&2
  exit 1
fi

lock_file="$repo_root/game/Temp/UnityLockfile"
if command -v lsof >/dev/null 2>&1 && [ -f "$lock_file" ] && lsof -t "$lock_file" >/dev/null 2>&1; then
  echo "Feche o Editor que está usando game/ antes de executar o build em batch." >&2
  exit 1
fi

# The game is offline; dependency downloads belong to the explicit setup step.
python3 "$repo_root/tools/setup_stockfish_android.py" --verify-only --offline

mkdir -p "$repo_root/.local/quest3"
log="$repo_root/.local/quest3/$action-$(date +%Y%m%d-%H%M%S)-$$.log"
echo "Unity $version — Quest 3 — $action"
echo "Log: $log"
if "$editor" -batchmode -quit -buildTarget Android \
  -projectPath "$repo_root/game" -executeMethod "$method" -logFile "$log"; then
  echo "Unity concluiu $action."
else
  result=$?
  tail -60 "$log" >&2
  exit "$result"
fi

if [ "$action" = build ] || [ "$action" = demo ]; then
  apk="$repo_root/Builds/Quest3/XadrezCGI-Quest3-dev.apk"
  if [ "$action" = demo ]; then apk="$repo_root/Builds/Quest3/XadrezCGI-Quest3-demo.apk"; fi
  if [ ! -s "$apk" ] || ! grep -Fq 'CHESS_CGI_QUEST_BUILD_RESULT result=Succeeded' "$log"; then
    echo "O Unity não confirmou um novo APK bem-sucedido. Confira $log." >&2
    exit 1
  fi
  echo "APK: $apk"
  shasum -a 256 "$apk"
  python3 - "$repo_root" <<'PY'
import json
from pathlib import Path
import shutil
import sys

root = Path(sys.argv[1])
notices = root / 'game/Assets/StreamingAssets/ThirdParty/Stockfish'
manifest = json.loads((notices / 'manifest.json').read_text())
bundle = root / '.local/stockfish/android' / manifest['source_bundle']['file']
destination = root / 'Builds/Quest3'
shutil.copy2(bundle, destination / bundle.name)
shutil.copy2(notices / 'manifest.json', destination / 'stockfish-manifest.json')
shutil.copy2(notices / 'Copying.txt', destination / 'Stockfish-Copying.txt')
print('Fonte correspondente ao motor: ' + str(destination / bundle.name))
PY
  python3 "$repo_root/tools/verify_quest3_apk.py" "$apk"
fi
