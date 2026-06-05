using UnityEngine;

public readonly struct CaptureAnimationStyle
{
    public CaptureAnimationStyle(string displayName, float duration, float lungeDistance, float impactScale, Color impactColor)
    {
        DisplayName = displayName;
        Duration = duration;
        LungeDistance = lungeDistance;
        ImpactScale = impactScale;
        ImpactColor = impactColor;
    }

    public string DisplayName { get; }

    public float Duration { get; }

    public float LungeDistance { get; }

    public float ImpactScale { get; }

    public Color ImpactColor { get; }
}
