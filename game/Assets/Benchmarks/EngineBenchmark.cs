using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErikTheCoder.MadChess.Core;
using ErikTheCoder.MadChess.Core.Game;
using ErikTheCoder.MadChess.Engine.Config;
using ErikTheCoder.MadChess.Engine.Heuristics;
using ErikTheCoder.MadChess.Engine.Intelligence;
using UnityEngine;
using Cache = ErikTheCoder.MadChess.Engine.Hashtable.Cache;
using Debug = UnityEngine.Debug;

public sealed class EngineBenchmark : MonoBehaviour
{
    private const int PliesPerDifficulty = 16;
    private const int IdleFrames = 600;
    private const int RawSpeedMilliseconds = 2000;
    private const int TargetFrameRate = 90;

    private static readonly string[] RawSpeedPositions =
    {
        "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
        "r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10",
        "8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1"
    };

    [Serializable]
    public sealed class FrameStats
    {
        public int frames;
        public double p50Ms;
        public double p95Ms;
        public double p99Ms;
        public double maxMs;
    }

    [Serializable]
    public sealed class DifficultyResult
    {
        public string engine;
        public string difficulty;
        public int moveTimeMs;
        public int elo;
        public int skillLevel;
        public double coldStartMs;
        public double thinkMeanMs;
        public double thinkMaxMs;
        public double processCpuPerMoveMs;
        public double childCpuPerMoveMs;
        public int gen0Collections;
        public long managedHeapDeltaBytes;
        public FrameStats framesWhileThinking;
    }

    [Serializable]
    public sealed class Report
    {
        public string backend;
        public string platform;
        public string device;
        public int processorCount;
        public double madChessManagedBytes;
        public double stockfishWorkingSetBytes;
        public double madChessFullStrengthNps;
        public FrameStats idleFrames;
        public List<string> failures = new List<string>();
        public List<DifficultyResult> results = new List<DifficultyResult>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Launch()
    {
        new GameObject(nameof(EngineBenchmark)).AddComponent<EngineBenchmark>();
    }

    private IEnumerator Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
        var report = new Report
        {
            backend = ScriptingBackend(),
            platform = Application.platform.ToString(),
            device = SystemInfo.processorType,
            processorCount = SystemInfo.processorCount
        };
        var idle = new List<double>();
        yield return RecordFrames(IdleFrames, idle);
        report.idleFrames = Stats(idle);
        report.madChessFullStrengthNps = MeasureFullStrengthNps();
        report.madChessManagedBytes = MeasureManagedFootprint();

        string stockfish = ComputerOpponentFactory.FindExecutable();
        foreach (ComputerDifficulty difficulty in Enum.GetValues(typeof(ComputerDifficulty)))
        {
            yield return PlayGame("MadChess", () => new MadChessMoveChooser(), difficulty, report);
            if (File.Exists(stockfish))
                yield return PlayGame("Stockfish", () => new StockfishUciMoveChooser(stockfish), difficulty, report);
        }
        if (File.Exists(stockfish))
            report.stockfishWorkingSetBytes = MeasureStockfishWorkingSet(stockfish);

        string path = Path.Combine(Application.persistentDataPath, $"engine-benchmark-{report.backend}.json");
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        Debug.Log("ENGINE_BENCHMARK_REPORT " + path);
        Application.Quit();
    }

    private static IEnumerator RecordFrames(int count, List<double> frames)
    {
        for (int i = 0; i < count; i++)
        {
            yield return null;
            frames.Add(Time.unscaledDeltaTime * 1000.0);
        }
    }

    private static IEnumerator PlayGame(string engine, Func<IMoveChooser> create, ComputerDifficulty difficulty, Report report)
    {
        yield return null;
        MoveSearchSettings settings = MoveSearchSettings.ForDifficulty(difficulty);
        var rules = new ChessRulesAdapter();
        var thinkTimes = new List<double>();
        var frames = new List<double>();
        double coldStart = 0;
        TimeSpan processCpuBefore = ProcessCpu();
        TimeSpan childCpuBefore = ChildCpu();
        int collectionsBefore = GC.CollectionCount(0);
        long heapBefore = GC.GetTotalMemory(false);

        var clock = Stopwatch.StartNew();
        IMoveChooser chooser = create();
        try
        {
            for (int ply = 0; ply < PliesPerDifficulty; ply++)
            {
                if (ply > 0) clock.Restart();
                Task<ChessMove> search = chooser.ChooseMoveAsync(rules.GetSnapshot(), settings, CancellationToken.None);
                while (!search.IsCompleted)
                {
                    yield return null;
                    frames.Add(Time.unscaledDeltaTime * 1000.0);
                }
                if (search.IsFaulted)
                {
                    report.failures.Add($"{engine} {difficulty}: {search.Exception.GetBaseException().Message}");
                    yield break;
                }
                double elapsed = clock.Elapsed.TotalMilliseconds;
                if (ply == 0) coldStart = elapsed;
                else thinkTimes.Add(elapsed);
                MoveResult result = rules.TryMove(search.Result);
                if (!result.Success || result.IsCheckmate || result.IsDraw) rules.Reset();
            }
        }
        finally
        {
            chooser.Dispose();
        }

        report.results.Add(new DifficultyResult
        {
            engine = engine,
            difficulty = difficulty.ToString(),
            moveTimeMs = settings.MoveTimeMilliseconds,
            elo = settings.Elo,
            skillLevel = settings.SkillLevel,
            coldStartMs = coldStart,
            thinkMeanMs = thinkTimes.Average(),
            thinkMaxMs = thinkTimes.Max(),
            processCpuPerMoveMs = PerMove(processCpuBefore, ProcessCpu()),
            childCpuPerMoveMs = PerMove(childCpuBefore, ChildCpu()),
            gen0Collections = GC.CollectionCount(0) - collectionsBefore,
            managedHeapDeltaBytes = GC.GetTotalMemory(false) - heapBefore,
            framesWhileThinking = Stats(frames)
        });
    }

    private static double PerMove(TimeSpan before, TimeSpan after) =>
        before == TimeSpan.MinValue || after == TimeSpan.MinValue ? -1 : (after - before).TotalMilliseconds / PliesPerDifficulty;

    private static double MeasureFullStrengthNps()
    {
        var messenger = new Messenger(Stream.Null, Stream.Null);
        AdvancedConfig config = AdvancedConfigDefaults.Create();
        var board = new Board(messenger);
        var timeManagement = new TimeManagement(messenger);
        var stats = new Stats();
        var killerMoves = new KillerMoves();
        var search = new Search(config.LimitStrength.Search, messenger, timeManagement, stats, new Cache(stats, 16),
            killerMoves, new MoveHistory(), new Evaluation(config.LimitStrength.Evaluation, messenger, stats));
        search.LimitedStrength = false;
        long nodes = 0;
        double seconds = 0;
        foreach (string fen in RawSpeedPositions)
        {
            board.SetPosition(fen);
            timeManagement.Reset();
            stats.Reset();
            search.Reset();
            search.PvInfoUpdate = false;
            timeManagement.MoveTimeHardLimit = TimeSpan.FromMilliseconds(RawSpeedMilliseconds);
            timeManagement.CanIncreaseMoveTime = false;
            board.Nodes = 0;
            var clock = Stopwatch.StartNew();
            search.FindBestMove(board);
            seconds += clock.Elapsed.TotalSeconds;
            nodes += board.Nodes;
        }
        search.Dispose();
        return nodes / seconds;
    }

    private static double MeasureManagedFootprint()
    {
        long before = GC.GetTotalMemory(true);
        var chooser = new MadChessMoveChooser();
        long after = GC.GetTotalMemory(true);
        GC.KeepAlive(chooser);
        chooser.Dispose();
        return after - before;
    }

    private static double MeasureStockfishWorkingSet(string stockfish)
    {
        using (var chooser = new StockfishUciMoveChooser(stockfish))
        {
            try
            {
                chooser.ChooseMoveAsync(new ChessRulesAdapter().GetSnapshot(), new MoveSearchSettings(20, 500, 5000), CancellationToken.None).Wait();
                return ChildProcesses().Sum(process => (double)process.WorkingSet64);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Stockfish working set unavailable: " + exception.Message);
                return -1;
            }
        }
    }

    private static TimeSpan ProcessCpu()
    {
        try
        {
            using (Process current = Process.GetCurrentProcess()) return current.TotalProcessorTime;
        }
        catch (Exception)
        {
            return TimeSpan.MinValue;
        }
    }

    private static TimeSpan ChildCpu()
    {
        try
        {
            return ChildProcesses().Aggregate(TimeSpan.Zero, (sum, process) => sum + process.TotalProcessorTime);
        }
        catch (Exception)
        {
            return TimeSpan.MinValue;
        }
    }

    private static IEnumerable<Process> ChildProcesses() => Process.GetProcessesByName("stockfish");

    private static FrameStats Stats(List<double> frames)
    {
        if (frames.Count == 0) return new FrameStats();
        List<double> sorted = frames.OrderBy(frame => frame).ToList();
        return new FrameStats
        {
            frames = sorted.Count,
            p50Ms = Percentile(sorted, 0.50),
            p95Ms = Percentile(sorted, 0.95),
            p99Ms = Percentile(sorted, 0.99),
            maxMs = sorted[sorted.Count - 1]
        };
    }

    private static double Percentile(List<double> sorted, double fraction) => sorted[Math.Min(sorted.Count - 1, (int)(fraction * sorted.Count))];

    private static string ScriptingBackend()
    {
#if ENABLE_IL2CPP
        return "il2cpp";
#else
        return "mono";
#endif
    }
}
