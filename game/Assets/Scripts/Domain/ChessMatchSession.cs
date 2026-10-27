using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Version 1 always starts from the standard initial position. A FEN alone would
// lose repetition history, castling rights and the pieces captured along the way.
public static class ChessMatchSession
{
    public const string StorageKey = "cicochess.match.v1";
    public const int MaxBytes = 65536;
    public const int MaxMoves = 2048;

    [Serializable]
    private sealed class SaveData
    {
        public int version = 1;
        public string mode;
        public string humanSide;
        public string difficulty;
        public string[] moves;
    }

    public readonly struct ReplayedMove
    {
        public ChessMove Move { get; }
        public ChessSide Side { get; }
        public MoveResult Result { get; }
        public ReplayedMove(ChessMove move, ChessSide side, MoveResult result)
        { Move = move; Side = side; Result = result; }
    }

    public sealed class Replay
    {
        public ChessRulesAdapter Rules { get; }
        public bool AgainstComputer { get; }
        public ChessSide HumanSide { get; }
        public ComputerDifficulty Difficulty { get; }
        public IReadOnlyList<ReplayedMove> Moves { get; }
        public MatchOutcome Outcome => Moves.Count == 0 ? MatchOutcome.InProgress : Moves[Moves.Count - 1].Result.Outcome;

        internal Replay(ChessRulesAdapter rules, bool computer, ChessSide humanSide,
            ComputerDifficulty difficulty, List<ReplayedMove> moves)
        { Rules = rules; AgainstComputer = computer; HumanSide = humanSide; Difficulty = difficulty; Moves = moves; }
    }

    public static bool TrySerialize(PositionSnapshot position, bool againstComputer,
        ChessSide humanSide, ComputerDifficulty difficulty, out string json, out string reason)
    {
        json = null;
        reason = "invalid-session";
        if ((humanSide != ChessSide.White && humanSide != ChessSide.Black) ||
            !Enum.IsDefined(typeof(ComputerDifficulty), difficulty) ||
            position.InitialFen != new ChessRulesAdapter().GetSnapshot().InitialFen)
            return false;
        string[] moves = string.IsNullOrEmpty(position.Moves) ? Array.Empty<string>() : position.Moves.Split(' ');
        if (moves.Length > MaxMoves) { reason = "save-too-large"; return false; }
        json = JsonUtility.ToJson(new SaveData {
            mode = againstComputer ? "computer" : "local",
            humanSide = humanSide == ChessSide.White ? "white" : "black",
            difficulty = difficulty == ComputerDifficulty.Beginner ? "beginner" :
                difficulty == ComputerDifficulty.Intermediate ? "intermediate" : "hard",
            moves = moves
        });
        if (Encoding.UTF8.GetByteCount(json) > MaxBytes)
        { json = null; reason = "save-too-large"; return false; }
        reason = "";
        return true;
    }

    // All parsing and replay happen on a separate adapter. No caller's live
    // position or AI search is touched when any byte or move is rejected.
    public static bool TryReplay(string json, out Replay replay, out string reason)
    {
        replay = null;
        reason = "invalid-save";
        if (string.IsNullOrEmpty(json)) { reason = "no-save"; return false; }
        if (json.Length > MaxBytes || Encoding.UTF8.GetByteCount(json) > MaxBytes)
        { reason = "save-too-large"; return false; }
        try
        {
            SaveData data = new Reader(json).Read();
            if (data.version != 1) { reason = "unsupported-version"; return false; }
            if ((data.mode != "local" && data.mode != "computer") ||
                (data.humanSide != "white" && data.humanSide != "black") ||
                (data.difficulty != "beginner" && data.difficulty != "intermediate" && data.difficulty != "hard"))
                return false;

            var rules = new ChessRulesAdapter();
            var moves = new List<ReplayedMove>();
            foreach (string uci in data.moves)
            {
                if (moves.Count > 0 && moves[moves.Count - 1].Result.Outcome != MatchOutcome.InProgress)
                { reason = "moves-after-result"; return false; }
                if (!ChessMove.TryParseUci(uci, out ChessMove move) || move.ToUci() != uci)
                { reason = "invalid-move"; return false; }
                ChessSide side = rules.CurrentTurn;
                VisualPieceState? mover = rules.GetPieceAt(move.From);
                bool promotion = mover.HasValue && mover.Value.Kind == ChessPieceKind.Pawn &&
                    move.To.Rank == (side == ChessSide.White ? 8 : 1);
                if (promotion != move.Promotion.HasValue)
                { reason = "invalid-move"; return false; }
                MoveResult result = rules.TryMove(move);
                if (!result.Success) { reason = "illegal-move"; return false; }
                moves.Add(new ReplayedMove(move, side, result));
            }
            replay = new Replay(rules, data.mode == "computer",
                data.humanSide == "white" ? ChessSide.White : ChessSide.Black,
                data.difficulty == "beginner" ? ComputerDifficulty.Beginner :
                    data.difficulty == "intermediate" ? ComputerDifficulty.Intermediate : ComputerDifficulty.Hard,
                moves);
            reason = "";
            return true;
        }
        catch (FormatException) { return false; }
        catch (ArgumentException) { return false; }
    }

    // A bounded reader for this small schema: reject missing/duplicate/unknown
    // fields, malformed JSON and trailing bytes instead of accepting a partial
    // JsonUtility object with silently defaulted settings.
    private sealed class Reader
    {
        private readonly string text;
        private int index;
        public Reader(string text) { this.text = text; }
        public SaveData Read()
        {
            var data = new SaveData();
            var fields = new HashSet<string>();
            Take('{');
            do
            {
                string key = ReadString(32);
                if (!fields.Add(key)) throw new FormatException();
                Take(':');
                switch (key)
                {
                    case "version": data.version = ReadVersion(); break;
                    case "mode": data.mode = ReadString(16); break;
                    case "humanSide": data.humanSide = ReadString(16); break;
                    case "difficulty": data.difficulty = ReadString(16); break;
                    case "moves": data.moves = ReadMoves(); break;
                    default: throw new FormatException();
                }
            } while (Optional(','));
            Take('}');
            Space();
            if (index != text.Length || fields.Count != 5 || data.moves == null) throw new FormatException();
            return data;
        }
        private int ReadVersion()
        {
            Space();
            int start = index;
            while (index < text.Length && text[index] >= '0' && text[index] <= '9') index++;
            string number = text.Substring(start, index - start);
            if (number.Length == 0 || (number.Length > 1 && number[0] == '0') ||
                !int.TryParse(number, out int value)) throw new FormatException();
            return value;
        }
        private string[] ReadMoves()
        {
            var moves = new List<string>();
            Take('[');
            if (!Optional(']'))
            {
                do {
                    if (moves.Count == MaxMoves) throw new FormatException();
                    moves.Add(ReadString(5));
                } while (Optional(','));
                Take(']');
            }
            return moves.ToArray();
        }
        private string ReadString(int maxLength)
        {
            Take('"');
            var value = new StringBuilder();
            while (index < text.Length)
            {
                char character = text[index++];
                if (character == '"') return value.ToString();
                if (character < 32) throw new FormatException();
                if (character == '\\')
                {
                    if (index == text.Length) throw new FormatException();
                    character = text[index++];
                    switch (character)
                    {
                        case '"': case '\\': case '/': break;
                        case 'b': character = '\b'; break;
                        case 'f': character = '\f'; break;
                        case 'n': character = '\n'; break;
                        case 'r': character = '\r'; break;
                        case 't': character = '\t'; break;
                        case 'u':
                            if (index + 4 > text.Length || !ushort.TryParse(text.Substring(index, 4),
                                System.Globalization.NumberStyles.AllowHexSpecifier,
                                System.Globalization.CultureInfo.InvariantCulture, out ushort code))
                                throw new FormatException();
                            character = (char)code;
                            index += 4;
                            break;
                        default: throw new FormatException();
                    }
                }
                value.Append(character);
                if (value.Length > maxLength) throw new FormatException();
            }
            throw new FormatException();
        }
        private void Space()
        { while (index < text.Length && (text[index] == ' ' || text[index] == '\n' || text[index] == '\r' || text[index] == '\t')) index++; }
        private bool Optional(char value)
        { Space(); if (index == text.Length || text[index] != value) return false; index++; return true; }
        private void Take(char value)
        { if (!Optional(value)) throw new FormatException(); }
    }
}
