// Browser transports complete through polling on Unity's player loop, without
// managed timers or worker threads. Poll must never block the calling thread.
public interface IPlayerLoopMoveChooser : IMoveChooser
{
    void Poll();
}
