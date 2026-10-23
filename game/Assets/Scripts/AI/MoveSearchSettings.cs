using System;

public readonly struct MoveSearchSettings
{
    public int SkillLevel { get; }
    public int MoveTimeMilliseconds { get; }
    public int TimeoutMilliseconds { get; }
    public int Elo { get; }

    public MoveSearchSettings(int skillLevel, int moveTimeMilliseconds, int timeoutMilliseconds, int elo = 600)
    {
        if (skillLevel < 0 || skillLevel > 20 || moveTimeMilliseconds < 1 ||
            timeoutMilliseconds <= moveTimeMilliseconds ||
            elo < 600 || elo > 2600)
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
}
