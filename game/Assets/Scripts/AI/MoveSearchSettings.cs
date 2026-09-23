using System;

public readonly struct MoveSearchSettings
{
    public int SkillLevel { get; }
    public int MoveTimeMilliseconds { get; }
    public int TimeoutMilliseconds { get; }

    public MoveSearchSettings(int skillLevel, int moveTimeMilliseconds, int timeoutMilliseconds)
    {
        if (skillLevel < 0 || skillLevel > 20 || moveTimeMilliseconds < 1 ||
            timeoutMilliseconds <= moveTimeMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(skillLevel), "Invalid search limits.");
        SkillLevel = skillLevel;
        MoveTimeMilliseconds = moveTimeMilliseconds;
        TimeoutMilliseconds = timeoutMilliseconds;
    }

    public static MoveSearchSettings ForDifficulty(ComputerDifficulty difficulty)
    {
        switch (difficulty)
        {
            case ComputerDifficulty.Beginner: return new MoveSearchSettings(0, 150, 6000);
            case ComputerDifficulty.Intermediate: return new MoveSearchSettings(6, 500, 6500);
            case ComputerDifficulty.Hard: return new MoveSearchSettings(14, 1200, 7200);
            default: throw new ArgumentOutOfRangeException(nameof(difficulty));
        }
    }
}
