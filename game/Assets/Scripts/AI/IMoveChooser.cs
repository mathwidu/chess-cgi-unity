using System;
using System.Threading;
using System.Threading.Tasks;

// The only engine contract used by gameplay. No Unity objects cross this boundary.
public interface IMoveChooser : IDisposable
{
    Task<ChessMove> ChooseMoveAsync(PositionSnapshot position, MoveSearchSettings settings,
        CancellationToken cancellationToken);
}
