using UnityEngine;

public readonly struct CaptureAnimationStyle
{
    public CaptureAnimationStyle(string displayName, float duration, float lungeDistance, float impactScale, Color impactColor)
        : this(displayName, displayName, duration, lungeDistance, impactScale, impactColor, 0.5f, string.Empty)
    {
    }

    public CaptureAnimationStyle(
        string displayName,
        string contractName,
        float duration,
        float lungeDistance,
        float impactScale,
        Color impactColor,
        float impactAtNormalizedTime,
        string futureClipName)
    {
        DisplayName = displayName;
        ContractName = contractName;
        Duration = duration;
        LungeDistance = lungeDistance;
        ImpactScale = impactScale;
        ImpactColor = impactColor;
        ImpactAtNormalizedTime = impactAtNormalizedTime;
        FutureClipName = futureClipName;
    }

    public string Name => ContractName;

    public string DisplayName { get; }

    public string ContractName { get; }

    public float Duration { get; }

    public float LungeDistance { get; }

    public float ImpactScale { get; }

    public Color ImpactColor { get; }

    public float ImpactAtNormalizedTime { get; }

    public string FutureClipName { get; }
}
