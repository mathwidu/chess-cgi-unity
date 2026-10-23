using System;
using System.Threading;
using System.Threading.Tasks;

// Poll completion on the main thread; engine continuations never touch the scene.
public sealed class ComputerTurnCoordinator : IDisposable
{
    private readonly IMoveChooser chooser;
    private CancellationTokenSource cancellation;
    private Task<ComputerTurnResult> pending;
    private Task<ChessMove> playerLoopSearch;
    private PositionSnapshot requested;

    public bool IsThinking => pending != null || playerLoopSearch != null;

    public ComputerTurnCoordinator(IMoveChooser chooser)
    {
        this.chooser = chooser ?? throw new ArgumentNullException(nameof(chooser));
    }

    public void Begin(PositionSnapshot position, MoveSearchSettings settings)
    {
        if (IsThinking)
        {
            throw new InvalidOperationException("A computer turn is already pending.");
        }
        requested = position;
        cancellation = new CancellationTokenSource();
        if (chooser is IPlayerLoopMoveChooser)
        {
            // Web has no managed timers/ThreadPool. Its transport owns phase
            // deadlines and is progressed by TryTakeResult on Unity's Update.
            try { playerLoopSearch = chooser.ChooseMoveAsync(position, settings, cancellation.Token); }
            catch (Exception exception) { pending = Task.FromResult(Failure(exception)); }
        }
        else pending = SearchAsync(position, settings, cancellation.Token);
    }

    public bool TryTakeResult(PositionSnapshot current, out ComputerTurnResult result)
    {
        result = default;
        if (playerLoopSearch != null)
        {
            ((IPlayerLoopMoveChooser)chooser).Poll();
            if (!playerLoopSearch.IsCompleted) return false;
            try { result = new ComputerTurnResult(playerLoopSearch.GetAwaiter().GetResult(), null); }
            catch (Exception exception) { result = Failure(exception); }
            playerLoopSearch = null;
            cancellation.Dispose();
            cancellation = null;
            return current.Revision == requested.Revision && current.SideToMove == requested.SideToMove;
        }
        if (pending == null || !pending.IsCompleted)
        {
            return false;
        }
        result = pending.GetAwaiter().GetResult();
        pending = null;
        cancellation.Dispose();
        cancellation = null;
        return current.Revision == requested.Revision && current.SideToMove == requested.SideToMove;
    }

    private static ComputerTurnResult Failure(Exception exception)
    {
        return new ComputerTurnResult(default, exception is OperationCanceledException
            ? "Busca da IA interrompida."
            : "Nao foi possivel obter a jogada da IA. " + exception.Message);
    }

    private async Task<ComputerTurnResult> SearchAsync(PositionSnapshot position,
        MoveSearchSettings settings, CancellationToken token)
    {
        using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            Task<ChessMove> search = null;
            try
            {
                search = chooser.ChooseMoveAsync(position, settings, deadline.Token);
                Task timeout = Task.Delay(settings.TimeoutMilliseconds, deadline.Token);
                if (await Task.WhenAny(search, timeout).ConfigureAwait(false) != search)
                {
                    deadline.Cancel();
                    token.ThrowIfCancellationRequested();
                    return new ComputerTurnResult(default, "A IA demorou demais. Tente novamente.");
                }
                ChessMove move = await search.ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                return new ComputerTurnResult(move, null);
            }
            catch (OperationCanceledException)
            {
                return new ComputerTurnResult(default, "Busca da IA interrompida.");
            }
            catch (Exception exception)
            {
                return new ComputerTurnResult(default, "Nao foi possivel obter a jogada da IA. " + exception.Message);
            }
            finally
            {
                deadline.Cancel();
                // A faulty provider may ignore cancellation and finish after timeout/reset.
                if (search != null)
                {
                    _ = ObserveCompletionAsync(search);
                }
            }
        }
    }

    private static async Task ObserveCompletionAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The outcome is already reported or invalidated; observe late failures.
        }
    }

    public void Cancel()
    {
        pending = null;
        Task<ChessMove> oldSearch = playerLoopSearch;
        playerLoopSearch = null;
        if (oldSearch != null) _ = ObserveCompletionAsync(oldSearch);
        if (cancellation == null)
        {
            return;
        }
        cancellation.Cancel();
        cancellation.Dispose();
        cancellation = null;
    }

    public void Dispose()
    {
        Cancel();
        chooser.Dispose();
    }
}
