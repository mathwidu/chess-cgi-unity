using System;

public readonly struct MoveSearchSettings
{
    public const int MinimumElo = 600;
    public const int MaximumElo = 2600;

    public int SkillLevel { get; }
    public int MoveTimeMilliseconds { get; }
    public int TimeoutMilliseconds { get; }
    public int Elo { get; }

    public MoveSearchSettings(int skillLevel, int moveTimeMilliseconds, int timeoutMilliseconds, int elo = MinimumElo)
    {
        if (skillLevel < 0 || skillLevel > 20 || moveTimeMilliseconds < 1 ||
            timeoutMilliseconds <= moveTimeMilliseconds ||
            elo < MinimumElo || elo > MaximumElo)
            throw new ArgumentOutOfRangeException(nameof(skillLevel), "Invalid search limits.");
        SkillLevel = skillLevel;
        MoveTimeMilliseconds = moveTimeMilliseconds;
        TimeoutMilliseconds = timeoutMilliseconds;
        Elo = elo;
    }

    public static MoveSearchSettings ForDifficulty(ComputerDifficulty difficulty)
    {
        switch (difficulty)
        {
            case ComputerDifficulty.Beginner: return new MoveSearchSettings(0, 150, 6000, 600);
            case ComputerDifficulty.Intermediate: return new MoveSearchSettings(6, 500, 6500, 1200);
            case ComputerDifficulty.Hard: return new MoveSearchSettings(14, 1200, 7200, 2000);
            default: throw new ArgumentOutOfRangeException(nameof(difficulty));
        }
    }

    public static MoveSearchSettings ForElo(int elo)
    {
        MoveSearchSettings preset = ForDifficulty(elo < 900 ? ComputerDifficulty.Beginner :
            elo < 1600 ? ComputerDifficulty.Intermediate : ComputerDifficulty.Hard);
        return new MoveSearchSettings(preset.SkillLevel, preset.MoveTimeMilliseconds, preset.TimeoutMilliseconds, elo);
    }
}
