#!/usr/bin/env python3
"""Inspect the delivered APK and matching offline-engine source bundle; no device access."""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
import zipfile


def sha256(stream):
    result = hashlib.sha256()
    for chunk in iter(lambda: stream.read(1024 * 1024), b''):
        result.update(chunk)
    return result.hexdigest()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def verify(apk):
    root = Path(__file__).resolve().parents[1]
    evidence = root / '.local/quest3-ai'
    evidence.mkdir(parents=True, exist_ok=True)
    version = re.search(r'^m_EditorVersion: (.+)$',
                        (root / 'game/ProjectSettings/ProjectVersion.txt').read_text(), re.M).group(1)
    android = Path('/Applications/Unity/Hub/Editor') / version / 'PlaybackEngines/AndroidPlayer'
    build_tools = android / 'SDK/build-tools/36.0.0'
    notices = root / 'game/Assets/StreamingAssets/ThirdParty/Stockfish'
    expected = json.loads((notices / 'manifest.json').read_text())
    checks = {}

    def run(name, command):
        result = subprocess.run([str(arg) for arg in command], text=True, capture_output=True, timeout=120)
        (evidence / (name + '.txt')).write_text(result.stdout + result.stderr)
        checks[name] = result.returncode
        require(result.returncode == 0, f'{name} failed; see {evidence / (name + ".txt")}')
        return result.stdout

    badging = run('apk-badging', [build_tools / 'aapt2', 'dump', 'badging', apk])
    manifest = run('apk-manifest', [build_tools / 'aapt2', 'dump', 'xmltree', apk, '--file', 'AndroidManifest.xml'])
    run('apk-signature', [android / 'OpenJDK/bin/java', '-jar', build_tools / 'lib/apksigner.jar',
                          'verify', '--verbose', apk])
    run('apk-alignment', [build_tools / 'zipalign', '-c', '-P', '16', '4', apk])
    for token in ["name='com.mathwidu.chesscgi'", "minSdkVersion:'29'", "targetSdkVersion:'35'",
                  "native-code: 'arm64-v8a'"]:
        require(token in badging, 'Unexpected Android build configuration: ' + token)
    require(re.search(r'android:extractNativeLibs[^\n]*=true', manifest), 'Native executable extraction disabled')
    for token in ['com.oculus.intent.category.VR', 'android.hardware.vr.headtracking', 'eureka',
                  'com.oculus.permission.HAND_TRACKING']:
        require(token in manifest, 'Missing Quest manifest declaration: ' + token)

    with zipfile.ZipFile(apk) as package:
        require(package.testzip() is None, 'APK ZIP integrity failure')
        native = sorted(name for name in package.namelist() if name.startswith('lib/') and name.endswith('.so'))
        require(all(name.startswith('lib/arm64-v8a/') for name in native), 'Unexpected native architecture')
        with package.open('lib/arm64-v8a/libstockfish.so') as stream:
            engine_hash = sha256(stream)
        require(engine_hash == expected['binary_sha256'], 'Packaged engine differs from the verified PIE')
        for name in ['manifest.json', 'Copying.txt', 'AUTHORS', 'SOURCE.txt']:
            require(package.read('assets/ThirdParty/Stockfish/' + name) == (notices / name).read_bytes(),
                    'Packaged engine notice differs: ' + name)
        require(any(b'Lcom/mathwidu/chesscgi/StockfishProcess;' in package.read(name)
                    for name in package.namelist() if re.fullmatch(r'classes\d*\.dex', name)),
                'Java UCI helper absent from compiled DEX')

    source = apk.parent / expected['source_bundle']['file']
    with source.open('rb') as stream:
        require(sha256(stream) == expected['source_bundle']['sha256'], 'Corresponding-source bundle mismatch')
    with apk.open('rb') as stream:
        apk_hash = sha256(stream)
    result = {'passed': True, 'apk': str(apk), 'size_bytes': apk.stat().st_size, 'sha256': apk_hash,
              'engine_sha256': engine_hash, 'native_libraries': native, 'checks': checks,
              'source_bundle': str(source), 'source_sha256': expected['source_bundle']['sha256'],
              'device_execution_tested': False}
    (evidence / 'apk-inspection.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    default = Path(__file__).resolve().parents[1] / 'Builds/Quest3/XadrezCGI-Quest3-dev.apk'
    verify(Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else default)
