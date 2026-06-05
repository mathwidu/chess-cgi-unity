using UnityEngine;

[System.Serializable]
public readonly struct PieceMotionSettings
{
    public PieceMotionSettings(float walkDuration, float stepHeight, float leanAngle, float captureDuration)
    {
        WalkDuration = walkDuration;
        StepHeight = stepHeight;
        LeanAngle = leanAngle;
        CaptureDuration = captureDuration;
    }

    public float WalkDuration { get; }

    public float StepHeight { get; }

    public float LeanAngle { get; }

    public float CaptureDuration { get; }

    public static PieceMotionSettings Default => new PieceMotionSettings(0.55f, 0.08f, 4.5f, 0.45f);
}
