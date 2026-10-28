using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Keeps URP's global XR scale unchanged while drawing the HUD at its own size.
/// Preview cameras cannot do this: URP also sets XR display scale to 1 for them.
/// </summary>
public sealed class QuestHudCaptureFeature : ScriptableRendererFeature
{
    public override void Create() { }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) { }

    public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
    {
        if (!cameraData.camera.TryGetComponent<QuestHudCaptureCamera>(out var capture) ||
            cameraData.camera.targetTexture == null) return;

        // This hook is after URP applies the scene scale to XR displays, but
        // before it allocates the camera's targets. The override is local.
        // Compatibility NativeRenderPass also caches scaledWidth/Height before
        // this hook, so this path deliberately requires RenderGraph.
        var graph = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
        if (graph == null || graph.enableRenderCompatibilityMode) return;
        capture.SceneRenderScale = cameraData.renderScale;
        cameraData.renderScale = 1f;
        cameraData.cameraTargetDescriptor = cameraData.camera.targetTexture.descriptor;
        capture.TargetPrepared = true;
    }
}
