#!/usr/bin/env python3
"""Vendor a pinned MadChess release into Unity as C# 9 / .NET Standard 2.1 source."""
import json
from pathlib import Path
import re
import shutil
import subprocess

TAG = 'v3.4'
COMMIT = '5448d8e710744d58a99dea64d9ab0698a97e80b3'
REPOSITORY = 'https://github.com/ekmadsen/MadChess'
EXCLUDED = re.compile(r'(Tuning/|Program\.cs|JsonContext\.cs|Properties/|UciStream\.cs|PgnGame|PositionSolution\.cs)')

ROOT = Path(__file__).resolve().parents[1]
CHECKOUT = ROOT / '.local' / 'madchess'
DESTINATION = ROOT / 'game' / 'Assets' / 'ThirdParty' / 'MadChess'


def checkout():
    if not (CHECKOUT / '.git').exists():
        CHECKOUT.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run(['git', '-c', 'advice.detachedHead=false', 'clone', '--quiet', '--depth', '1', '--branch', TAG, REPOSITORY, str(CHECKOUT)], check=True)
    subprocess.run(['git', '-C', str(CHECKOUT), 'fetch', '--quiet', '--depth', '1', 'origin', 'tag', TAG], check=True)
    subprocess.run(['git', '-C', str(CHECKOUT), '-c', 'advice.detachedHead=false', 'checkout', '--quiet', TAG], check=True)
    head = subprocess.run(['git', '-C', str(CHECKOUT), 'rev-parse', 'HEAD'], check=True, capture_output=True, text=True).stdout.strip()
    if head != COMMIT:
        raise SystemExit(f'{TAG} resolves to {head}, expected {COMMIT}.')


def block_namespace(text):
    match = re.search(r'^namespace ([\w.]+);[ \t]*$', text, re.M)
    if not match:
        return text
    return text[:match.start()] + f'namespace {match.group(1)}\n{{' + text[match.end():].rstrip() + '\n}\n'


def previous_token(text, index):
    position = index - 1
    while position >= 0:
        if text[position].isspace():
            position -= 1
            continue
        line_start = text.rfind('\n', 0, position + 1) + 1
        line = text[line_start:position + 1]
        comment = line.find('//')
        if comment != -1 and '"' not in line[:comment]:
            position = line_start + comment - 1
            continue
        return text[position], text[max(0, position - 5):position + 1]
    return '', ''


def skip_literal(text, index):
    quote = text[index] if text[index] != '$' else '"'
    position = index + (2 if text[index] == '$' else 1)
    while text[position] != quote:
        if text[position] == '\\':
            position += 1
        position += 1
    return position + 1


def array_initializers(text):
    output, stack, index = [], [], 0
    while index < len(text):
        character = text[index]
        if text.startswith('//', index):
            end = text.find('\n', index)
            end = len(text) if end == -1 else end
            output.append(text[index:end])
            index = end
            continue
        if text.startswith('/*', index):
            end = text.find('*/', index) + 2
            output.append(text[index:end])
            index = end
            continue
        if character in '"\'' or text.startswith('$"', index):
            end = skip_literal(text, index)
            output.append(text[index:end])
            index = end
            continue
        if character == '[':
            token, context = previous_token(text, index)
            inside_collection = bool(stack) and stack[-1] == 'collection'
            collection = (token in '=(' or context.endswith('return') or context.endswith('=>')
                          or (token in ',[' and inside_collection))
            if collection:
                empty = re.match(r'\[\s*\]', text[index:])
                if empty:
                    output.append('__EMPTY__')
                    index += empty.end()
                    continue
                stack.append('collection')
                output.append('new[] {')
            else:
                stack.append('index')
                output.append('[')
            index += 1
            continue
        if character == ']':
            output.append('}' if stack and stack.pop() == 'collection' else ']')
            index += 1
            continue
        output.append(character)
        index += 1
    return ''.join(output)


def interpolated_strings(text):
    pattern = re.compile(r'(\$\$|\$)"""\n(.*?)\n([ \t]*)"""', re.S)

    def replace(match):
        dollars, body, indent = match.groups()
        value = '\n'.join(line[len(indent):] if line.startswith(indent) else line.lstrip() for line in body.split('\n'))
        if dollars == '$$':
            value = re.sub(r'\{\{(.*?)\}\}', lambda hole: '\x00' + hole.group(1) + '\x01', value)
            value = value.replace('{', '{{').replace('}', '}}').replace('\x00', '{').replace('\x01', '}')
        return '$"' + value.replace('\\', '\\\\').replace('"', '\\"').replace('\n', '\\n') + '"'

    return pattern.sub(replace, text)


def down_level(text):
    text = interpolated_strings(array_initializers(block_namespace(text)))
    text = re.sub(r'\bLock (_\w+) = new\(\);', r'object \1 = new object();', text)
    text = text.replace('private static readonly Lock ', 'private static readonly object ')
    text = re.sub(r'^using JetBrains\.Annotations;\n', '', text, flags=re.M)
    text = re.sub(r'^[ \t]*\[(UsedImplicitly|ContractAnnotation\([^)]*\))\][ \t]*\n', '', text, flags=re.M)
    text = text.replace('[UsedImplicitly] ', '')
    return re.sub(r'Math\.Log2\(([^()]*)\)', r'Math.Log(\1, 2)', text)


PATCHES = {
    'Engine/Score/ScoredMove.cs': [(
        'public struct ScoredMove(ulong move, int score)\n{\n    public ulong Move = move;\n    public int Score = score;\n',
        'public struct ScoredMove\n{\n    public ulong Move;\n    public int Score;\n\n\n'
        '    public ScoredMove(ulong move, int score)\n    {\n        Move = move;\n        Score = score;\n    }\n')],
    'Engine/Hashtable/CachedPosition.cs': [(
        'public struct CachedPosition(ulong key, ulong data)\n{\n    public ulong Key = key;\n    public ulong Data = data;\n',
        'public struct CachedPosition\n{\n    public ulong Key;\n    public ulong Data;\n\n\n'
        '    public CachedPosition(ulong key, ulong data)\n    {\n        Key = key;\n        Data = data;\n    }\n')],
    'Engine/Heuristics/KillerMove.cs': [(
        'public readonly struct KillerMove(Piece piece, Square toSquare) : IEquatable<KillerMove>\n{\n'
        '    public static readonly KillerMove Null;\n    public readonly Piece Piece = piece;\n    public readonly Square ToSquare = toSquare;\n',
        'public readonly struct KillerMove : IEquatable<KillerMove>\n{\n    public static readonly KillerMove Null;\n'
        '    public readonly Piece Piece;\n    public readonly Square ToSquare;\n\n\n'
        '    public KillerMove(Piece piece, Square toSquare)\n    {\n        Piece = piece;\n        ToSquare = toSquare;\n    }\n')],
    'Engine/Intelligence/TimeManagement.cs': [(
        'public sealed class TimeManagement(Messenger messenger)\n{\n',
        'public sealed class TimeManagement\n{\n    private readonly Messenger messenger;\n\n\n'
        '    public TimeManagement(Messenger messenger)\n    {\n        this.messenger = messenger;\n    }\n\n\n')],
    'Core/Messenger.cs': [
        ('using System.IO;\n', 'using System.IO;\nusing System.Text;\n'),
        ('public sealed class Messenger(Stream inputStream, Stream outputStream) : IAsyncDisposable\n{\n',
         'public sealed class Messenger : IAsyncDisposable\n{\n'),
        ('    private StreamReader _inputStreamReader = new(inputStream, leaveOpen: true);\n'
         '    private StreamWriter _outputStreamWriter = new(outputStream, leaveOpen: true) { AutoFlush = true };\n',
         '    private StreamReader _inputStreamReader;\n    private StreamWriter _outputStreamWriter;\n'),
        ('    private StreamWriter _logWriter;\n',
         '    private StreamWriter _logWriter;\n\n\n    public Messenger(Stream inputStream, Stream outputStream)\n    {\n'
         '        _inputStreamReader = new StreamReader(inputStream, new UTF8Encoding(false), true, 1024, true);\n'
         '        _outputStreamWriter = new StreamWriter(outputStream, new UTF8Encoding(false), 1024, true) { AutoFlush = true };\n    }\n'),
        ('new StreamWriter(fileStream, leaveOpen: false)', 'new StreamWriter(fileStream, new UTF8Encoding(false), 1024, false)')],
    'Core/Utilities/SafeRandom.cs': [('_lock = new Lock();', '_lock = new object();')],
    'Engine/Intelligence/Evaluation.cs': [
        ('var (quotient, remainder) = Math.DivRem(plyCount, 2);', 'var quotient = Math.DivRem(plyCount, 2, out var remainder);')],
    'Engine/Intelligence/Search.cs': [('CandidateMoves = __EMPTY__;', 'CandidateMoves = new List<ulong>();')],
    'Core/Utilities/Bitwise.cs': [
        ('using System.Numerics; // Enables CPU intrinsics (popcount and bitscan).  Falls back to software implementation when CPU lacks the intrinsic operation.\n', ''),
        ('BitOperations.PopCount(', 'SoftwareBitOperations.PopCount(', 1),
        ('BitOperations.TrailingZeroCount(', 'SoftwareBitOperations.TrailingZeroCount(', 3)],
}

SOFTWARE_BIT_OPERATIONS = '''

internal static class SoftwareBitOperations
{
    private const ulong _deBruijn = 0x03f79d71b4cb0a89ul;
    private static readonly int[] _deBruijnIndex = CreateDeBruijnIndex();


    private static int[] CreateDeBruijnIndex()
    {
        var index = new int[64];
        for (var bit = 0; bit < 64; bit++) index[((1ul << bit) * _deBruijn) >> 58] = bit;
        return index;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(ulong value)
    {
        value -= (value >> 1) & 0x5555555555555555ul;
        value = (value & 0x3333333333333333ul) + ((value >> 2) & 0x3333333333333333ul);
        value = (value + (value >> 4)) & 0x0F0F0F0F0F0F0F0Ful;
        return (int)((value * 0x0101010101010101ul) >> 56);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeroCount(ulong value) => value == 0 ? 64 : _deBruijnIndex[((value & (0ul - value)) * _deBruijn) >> 58];


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeroCount(uint value) => value == 0 ? 32 : TrailingZeroCount((ulong)value);
}
}
'''

UCI_CONSTANTS = '''namespace ErikTheCoder.MadChess.Engine
{
public static class UciStream
{
    public const long NodesInfoInterval = 1_000_000;
    public const long NodesTimeInterval = 1_000;
}
}
'''

ASSEMBLY_DEFINITION = {
    'name': 'MadChess', 'rootNamespace': 'ErikTheCoder.MadChess', 'references': [],
    'includePlatforms': [], 'excludePlatforms': [], 'allowUnsafeCode': False, 'overrideReferences': False,
    'precompiledReferences': [], 'autoReferenced': False, 'defineConstraints': [], 'versionDefines': [],
    'noEngineReferences': True,
}


def literal(value):
    return f'{value}d' if isinstance(value, float) else str(value)


def config_defaults():
    source = (CHECKOUT / 'src' / 'Engine' / 'MadChess.AdvancedConfig.json').read_text(encoding='utf-8-sig')
    limit = json.loads('\n'.join(line for line in source.split('\n') if not line.strip().startswith('//')))['limitStrength']

    def properties(values):
        return ',\n'.join(f'                {name[0].upper() + name[1:]} = {literal(value)}' for name, value in values.items())

    return f'''namespace ErikTheCoder.MadChess.Engine.Config
{{
public static class AdvancedConfigDefaults
{{
    public static AdvancedConfig Create() => new AdvancedConfig
    {{
        LimitStrength = new LimitStrengthConfig
        {{
            Evaluation = new LimitStrengthEvalConfig
            {{
{properties(limit['evaluation'])}
            }},
            Search = new LimitStrengthSearchConfig
            {{
{properties(limit['search'])}
            }}
        }}
    }};
}}
}}
'''


SOURCE_NOTES = f'''# MadChess (vendored)

- Upstream: {REPOSITORY}
- Tag: {TAG} ({COMMIT})
- License: MIT, see `LICENSE.md`
- Regenerate with `python3 tools/port_madchess.py`; do not edit these files by hand.

Port to Unity's C# 9 / .NET Standard 2.1, without changing search or evaluation logic:

- Dropped `Program.cs`, `UciStream.cs`, `Tuning/`, PGN and `PositionSolution` (console, tuning and test tooling).
- `Engine/UciStream.cs` keeps only the two node-interval constants `Search` reads.
- `MadChess.AdvancedConfig.json` became `Engine/Config/AdvancedConfigDefaults.cs`, generated from the same values.
- File-scoped namespaces, primary constructors, collection expressions and raw string literals rewritten in C# 9 syntax.
- `System.Threading.Lock` replaced by `object`; JetBrains annotations removed.
- `BitOperations`, `Math.Log2` and tuple `Math.DivRem` replaced by .NET Standard 2.1 equivalents.
'''


def main():
    checkout()
    source_root = CHECKOUT / 'src'
    generated = {}
    for path in sorted(source_root.rglob('*.cs')):
        relative = path.relative_to(source_root).as_posix()
        if not EXCLUDED.search(relative):
            generated[relative] = down_level(path.read_text(encoding='utf-8-sig'))
    for relative, patches in PATCHES.items():
        text = generated[relative]
        for patch in patches:
            old, new = patch[0], patch[1]
            expected = patch[2] if len(patch) > 2 else 1
            if text.count(old) != expected:
                raise SystemExit(f'Patch drifted in {relative}: {old[:60]!r}')
            text = text.replace(old, new)
        generated[relative] = text
    generated['Core/Utilities/Bitwise.cs'] = generated['Core/Utilities/Bitwise.cs'].rstrip()[:-1].rstrip() + SOFTWARE_BIT_OPERATIONS
    generated['Engine/Intelligence/Evaluation.cs'] = generated['Engine/Intelligence/Evaluation.cs'].replace('__EMPTY__,', 'new int[0],')
    generated['Engine/UciStream.cs'] = UCI_CONSTANTS
    generated['Engine/Config/AdvancedConfigDefaults.cs'] = config_defaults()
    for marker in ('__EMPTY__', 'Math.Log2(', 'BitOperations.', '"""'):
        leftovers = [name for name, text in generated.items() if marker in text.replace('SoftwareBitOperations.', '')]
        if leftovers:
            raise SystemExit(f'Unconverted {marker!r} in {leftovers}')
    generated['MadChess.asmdef'] = json.dumps(ASSEMBLY_DEFINITION, indent=4) + '\n'
    generated['SOURCE.md'] = SOURCE_NOTES
    generated['LICENSE.md'] = (CHECKOUT / 'docs' / 'LICENSE.md').read_text(encoding='utf-8-sig')

    DESTINATION.mkdir(parents=True, exist_ok=True)
    for existing in DESTINATION.rglob('*'):
        if existing.is_file() and existing.suffix != '.meta':
            if existing.relative_to(DESTINATION).as_posix() not in generated:
                existing.unlink()
    for relative, text in generated.items():
        target = DESTINATION / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text, encoding='utf-8')
    for meta in DESTINATION.rglob('*.meta'):
        if not meta.with_suffix('').exists():
            meta.unlink()
    print(f'MadChess {TAG}: {len(generated)} files in {DESTINATION.relative_to(ROOT)}')


if __name__ == '__main__':
    main()
