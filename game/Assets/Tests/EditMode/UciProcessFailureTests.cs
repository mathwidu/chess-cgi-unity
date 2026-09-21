#if UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

public class UciProcessFailureTests
{
    private string directory;

    private string CreateEngine(string onSearch, bool handshake = true)
    {
        directory = Path.Combine(Path.GetTempPath(), "chess uci " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "fake-engine");
        File.WriteAllText(path, "#!/bin/sh\necho $$ > \"$0.pid\"\nwhile IFS= read -r command; do\ncase \"$command\" in\n" +
            "uci) " + (handshake ? "printf 'uciok\\n'" : ":") + ";;\n" +
            "isready) printf 'readyok\\n';;\ngo*) " + onSearch + ";;\nquit) exit 0;;\nesac\ndone\n");
        using (var chmod = Process.Start(new ProcessStartInfo("/bin/chmod", "+x \"" + path + "\"") { UseShellExecute = false }))
        {
            chmod.WaitForExit();
            Assert.That(chmod.ExitCode, Is.Zero);
        }
        return path;
    }

    [TearDown]
    public void Cleanup()
    {
        if (directory != null) Directory.Delete(directory, true);
    }

    private static async Task<ComputerTurnResult> Wait(ComputerTurnCoordinator coordinator, PositionSnapshot position)
    {
        for (int i = 0; i < 150; i++)
        {
            if (coordinator.TryTakeResult(position, out var result)) return result;
            await Task.Delay(20);
        }
        Assert.Fail("The engine did not complete or time out.");
        return default;
    }

    [TestCase("exit 3", "encerrou")]
    [TestCase("printf 'bestmove nonsense\\n'", "malformada")]
    [TestCase("printf 'bestmove (none)\\n'", "malformada")]
    public async Task ProcessExitAndInvalidProtocolBecomeRecoverableFailures(string reply, string expected)
    {
        var position = new ChessRulesAdapter().GetSnapshot();
        using (var coordinator = new ComputerTurnCoordinator(new StockfishUciMoveChooser(CreateEngine(reply))))
        {
            coordinator.Begin(position, new MoveSearchSettings(0, 1, 1000));
            var result = await Wait(coordinator, position);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain(expected));
        }
    }

    [Test]
    public async Task SilentHandshakeTimesOutAndReleasesItsChildProcess()
    {
        string path = CreateEngine(":", false);
        var position = new ChessRulesAdapter().GetSnapshot();
        using (var coordinator = new ComputerTurnCoordinator(new StockfishUciMoveChooser(path)))
        {
            coordinator.Begin(position, new MoveSearchSettings(0, 1, 250));
            Assert.That((await Wait(coordinator, position)).Success, Is.False);
            int pid = int.Parse(File.ReadAllText(path + ".pid"));
            await Task.Delay(500);
            bool exited;
            try { using (var child = Process.GetProcessById(pid)) exited = child.HasExited; }
            catch (ArgumentException) { exited = true; }
            Assert.That(exited, Is.True, "Only the owned engine child should be closed on timeout.");
        }
    }
}
#endif
