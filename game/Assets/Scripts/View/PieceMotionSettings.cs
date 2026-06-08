using UnityEngine;

[System.Serializable]
public readonly struct PieceMotionSettings
{
    public PieceMotionSettings(float walkDuration, float stepHeight, float leanAngle, float captureDuration)
        : this(walkDuration, stepHeight, leanAngle, captureDuration, 1.55f, 0.018f, 0.024f)
    {
    }

    public PieceMotionSettings(
        float walkDuration,
        float stepHeight,
        float leanAngle,
        float captureDuration,
        float strideCycles,
        float bodySway,
        float torsoBobHeight)
    {
        WalkDuration = walkDuration;
        StepHeight = stepHeight;
        LeanAngle = leanAngle;
        CaptureDuration = captureDuration;
        StrideCycles = strideCycles;
        BodySway = bodySway;
        TorsoBobHeight = torsoBobHeight;
    }

    public float WalkDuration { get; }

    public float StepHeight { get; }

    public float LeanAngle { get; }

    public float CaptureDuration { get; }

    public float StrideCycles { get; }

    public float BodySway { get; }

    public float TorsoBobHeight { get; }

    public static PieceMotionSettings Default => new PieceMotionSettings(1.12f, 0.045f, 3.2f, 0.45f, 1.55f, 0.018f, 0.024f);
}
