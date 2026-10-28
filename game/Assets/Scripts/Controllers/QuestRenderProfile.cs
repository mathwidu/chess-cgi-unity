using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

// The standalone demo uses the saved Mobile pipeline. Foveation is a display
// setting, so apply it again when the headset resumes its XR session.
public sealed class QuestRenderProfile : MonoBehaviour
{
    public const float DefaultFoveation = .5f;
    private readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
    private XRDisplaySubsystem configuredDisplay;
    private float nextCheck;

#if UNITY_ANDROID && !UNITY_EDITOR && !DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var profile = new GameObject("Quest Render Profile");
        DontDestroyOnLoad(profile);
        profile.AddComponent<QuestRenderProfile>();
    }
#endif

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + .5f;
        if (configuredDisplay != null && configuredDisplay.running) return;
        if (RenderPipelineManager.currentPipeline == null) return;
        SubsystemManager.GetSubsystems(displays);
        XRDisplaySubsystem display = displays.Find(item => item.running && item.GetRenderPassCount() > 0);
        if (display == null) return;
        // Capabilities can be unavailable while OpenXR/pipeline initialization
        // is still completing. Retry rather than remembering an inactive level.
        if (SystemInfo.foveatedRenderingCaps == FoveatedRenderingCaps.None) return;
        display.foveatedRenderingFlags = XRDisplaySubsystem.FoveatedRenderingFlags.None;
        display.foveatedRenderingLevel = DefaultFoveation;
        configuredDisplay = display;
        LogDisplayProfile(display);
    }

    private static void LogDisplayProfile(XRDisplaySubsystem display)
    {
        var pipeline = (QualitySettings.renderPipeline != null
            ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
        if (pipeline == null) return;
        display.GetRenderPass(0, out XRDisplaySubsystem.XRRenderPass pass);
        RenderTextureDescriptor target = pass.renderTargetDesc;
        RenderTextureDescriptor requested = target;
        requested.msaaSamples = pipeline.msaaSampleCount;
        int supported = SystemInfo.GetRenderTextureSupportedMSAASampleCount(requested);
        RenderTextureDescriptor eightSamples = target;
        eightSamples.msaaSamples = 8;
        int supportedForEight = SystemInfo.GetRenderTextureSupportedMSAASampleCount(eightSamples);
        // A swapchain may be resolved from an intermediate target. Keep its
        // samples separate from the requested profile and descriptor capability.
        Debug.Log(FormattableString.Invariant($"CHESS_CGI_QUEST_PROFILE scale={pipeline.renderScale:F2} msaaRequested={pipeline.msaaSampleCount} supportedForRequested={supported} supportedFor8x={supportedForEight} swapchainSamples={target.msaaSamples} xrTarget={target.width}x{target.height} color={target.graphicsFormat} depth={target.depthStencilFormat} dimension={target.dimension} slices={target.volumeDepth} hdr={pipeline.supportsHDR} foveationRequested={DefaultFoveation:F2} foveationReported={display.foveatedRenderingLevel:F2} caps={SystemInfo.foveatedRenderingCaps}"));
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused) configuredDisplay = null;
    }
}
