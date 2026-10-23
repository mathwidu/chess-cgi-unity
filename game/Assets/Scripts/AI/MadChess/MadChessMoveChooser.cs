using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ErikTheCoder.MadChess.Core;
using ErikTheCoder.MadChess.Core.Game;
using ErikTheCoder.MadChess.Core.Moves;
using ErikTheCoder.MadChess.Engine.Config;
using ErikTheCoder.MadChess.Engine.Hashtable;
using ErikTheCoder.MadChess.Engine.Heuristics;
using ErikTheCoder.MadChess.Engine.Intelligence;

public sealed class MadChessMoveChooser : IMoveChooser
{
    private const int CacheMegabytes = 16;

    private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
    private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
    private Board board;
    private TimeManagement timeManagement;
    private Stats stats;
    private KillerMoves killerMoves;
    private Search search;
    private int disposed;

    private void CreateEngine()
    {
        AdvancedConfig config = AdvancedConfigDefaults.Create();
        var messenger = new Messenger(Stream.Null, Stream.Null);
        board = new Board(messenger);
        timeManagement = new TimeManagement(messenger);
        stats = new Stats();
        killerMoves = new KillerMoves();
        var cache = new Cache(stats, CacheMegabytes);
        var evaluation = new Evaluation(config.LimitStrength.Evaluation, messenger, stats);
        search = new Search(config.LimitStrength.Search, messenger, timeManagement, stats, cache,
            killerMoves, new MoveHistory(), evaluation);
        search.LimitedStrength = true;
    }

    public async Task<ChessMove> ChooseMoveAsync(PositionSnapshot position, MoveSearchSettings settings,
        CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(MadChessMoveChooser));
        }
        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token))
        {
            await gate.WaitAsync(linked.Token).ConfigureAwait(false);
            try
            {
                return await Task.Run(() => FindBestMove(position, settings, linked.Token), linked.Token).ConfigureAwait(false);
            }
            finally
            {
                if (Volatile.Read(ref disposed) != 0)
                {
                    search?.Dispose();
                }
                gate.Release();
            }
        }
    }

    private ChessMove FindBestMove(PositionSnapshot position, MoveSearchSettings settings, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (search == null)
        {
            CreateEngine();
        }
        SetPosition(position);
        search.Elo = settings.Elo;
        timeManagement.Reset();
        stats.Reset();
        search.Reset();
        search.PvInfoUpdate = false;
        killerMoves.Shift(2);
        timeManagement.MoveTimeSoftLimit = TimeSpan.FromMilliseconds(settings.MoveTimeMilliseconds);
        timeManagement.MoveTimeHardLimit = timeManagement.MoveTimeSoftLimit;
        timeManagement.CanIncreaseMoveTime = false;
        ulong bestMove;
        using (token.Register(() => search.Continue = false))
        {
            bestMove = search.FindBestMove(board);
        }
        token.ThrowIfCancellationRequested();
        if (bestMove == Move.Null || !ChessMove.TryParseUci(Move.ToLongAlgebraic(bestMove), out ChessMove move))
            throw new InvalidDataException("O motor devolveu uma jogada malformada ou sem movimento.");
        return move;
    }

    private void SetPosition(PositionSnapshot position)
    {
        board.SetPosition(position.InitialFen);
        if (string.IsNullOrEmpty(position.Moves))
        {
            return;
        }
        foreach (string text in position.Moves.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            ulong move = Move.ParseLongAlgebraic(text, board.CurrentPosition.ColorToMove);
            if (!board.CurrentPosition.ValidateMove(ref move) || !board.PlayMove(move).legalMove)
                throw new InvalidDataException("Histórico de jogadas inválido para o motor: " + text);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }
        lifetime.Cancel();
        if (gate.Wait(0))
        {
            search?.Dispose();
        }
    }
}
