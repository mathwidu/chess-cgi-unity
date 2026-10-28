using UnityEngine;

/// <summary>Marks only the offscreen HUD camera for its local URP target override.</summary>
[DisallowMultipleComponent]
public sealed class QuestHudCaptureCamera : MonoBehaviour
{
    public bool TargetPrepared { get; internal set; }
    public float SceneRenderScale { get; internal set; }
}
