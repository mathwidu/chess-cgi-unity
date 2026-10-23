# MadChess (vendored)

- Upstream: https://github.com/ekmadsen/MadChess
- Tag: v3.4 (5448d8e710744d58a99dea64d9ab0698a97e80b3)
- License: MIT, see `LICENSE.md`
- Regenerate with `python3 tools/port_madchess.py`; do not edit these files by hand.

Port to Unity's C# 9 / .NET Standard 2.1, without changing search or evaluation logic:

- Dropped `Program.cs`, `UciStream.cs`, `Tuning/`, PGN and `PositionSolution` (console, tuning and test tooling).
- `Engine/UciStream.cs` keeps only the two node-interval constants `Search` reads.
- `MadChess.AdvancedConfig.json` became `Engine/Config/AdvancedConfigDefaults.cs`, generated from the same values.
- File-scoped namespaces, primary constructors, collection expressions and raw string literals rewritten in C# 9 syntax.
- `System.Threading.Lock` replaced by `object`; JetBrains annotations removed.
- `BitOperations`, `Math.Log2` and tuple `Math.DivRem` replaced by .NET Standard 2.1 equivalents.
