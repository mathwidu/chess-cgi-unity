namespace ErikTheCoder.MadChess.Engine.Config
{
public static class AdvancedConfigDefaults
{
    public static AdvancedConfig Create() => new AdvancedConfig
    {
        LimitStrength = new LimitStrengthConfig
        {
            Evaluation = new LimitStrengthEvalConfig
            {
                UndervaluePawnsMaxElo = 800,
                ValueKnightBishopEquallyMaxElo = 1000,
                UndervalueRookOvervalueQueenMaxElo = 1000,
                MisjudgePassedPawnsMaxElo = 1200,
                InattentiveKingDefenseMaxElo = 1400,
                MisplacePiecesMaxElo = 1600,
                UnderestimateMobilePiecesMaxElo = 1800,
                AllowPawnStructureDamageMaxElo = 2000,
                UnderestimateThreatsMaxElo = 2200,
                PoorManeuveringMinorPiecesMaxElo = 2300,
                PoorManeuveringMajorPiecesMaxElo = 2400
            },
            Search = new LimitStrengthSearchConfig
            {
                NpsScale = 0.125d,
                NpsPower = 6.5d,
                NpsConstant = 243,
                NpsEndgamePer128 = 32,
                MoveErrorScale = 0.07d,
                MoveErrorPower = 3,
                MoveErrorConstant = 7,
                BlunderErrorScale = 0.16d,
                BlunderErrorPower = 3,
                BlunderErrorConstant = 38,
                BlunderPer1024Scale = 0.25d,
                BlunderPer1024Power = 2.5d,
                BlunderPer1024Constant = 35
            }
        }
    };
}
}
