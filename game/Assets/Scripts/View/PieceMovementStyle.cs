using UnityEngine;

public readonly struct PieceMovementStyle
{
    public PieceMovementStyle(
        string name,
        float duration,
        float stepHeight,
        float bodySway,
        float strideCycles,
        float hopHeight,
        float leanAngle)
    {
        Name = name;
        Duration = duration;
        StepHeight = stepHeight;
        BodySway = bodySway;
        StrideCycles = strideCycles;
        HopHeight = hopHeight;
        LeanAngle = leanAngle;
    }

    public string Name { get; }

    public float Duration { get; }

    public float StepHeight { get; }

    public float BodySway { get; }

    public float StrideCycles { get; }

    public float HopHeight { get; }

    public float LeanAngle { get; }

    public float RootProgressAt(float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}

