#if UNITY_EDITOR || (UNITY_ANDROID && DEVELOPMENT_BUILD)
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

// Development APK only: controlled render comparisons without rebuilding or
// changing the device globally. Each request lasts for one process launch.
public sealed class QuestRenderDiagnostics : MonoBehaviour
{
    private const string Prefix = "CHESS_CGI_QUEST_RENDER";
    private readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
    private readonly List<ProfilerRecorder> recorders = new List<ProfilerRecorder>();
    private readonly List<string> counterNames = new List<string>();
    private UniversalRenderPipelineAsset runtimePipeline;
    private RenderPipelineAsset previousQualityPipeline;
    private XRDisplaySubsystem configuredDisplay;
    private float requestedFoveation;
    private float nextSample;
    private bool countersInitialized;
    private bool ownsPipeline;

#if UNITY_ANDROID && DEVELOPMENT_BUILD && !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var diagnostics = new GameObject("Quest Render Diagnostics");
        DontDestroyOnLoad(diagnostics);
        diagnostics.AddComponent<QuestRenderDiagnostics>();
    }
#endif

    private void Awake()
    {
        var source = (QualitySettings.renderPipeline != null
            ? QualitySettings.renderPipeline : GraphicsSettings.defaultRenderPipeline) as UniversalRenderPipelineAsset;
        if (source == null) { enabled = false; return; }
        float scale = source.renderScale;
        int msaa = source.msaaSampleCount;
        float shadowDistance = source.shadowDistance;
        int additionalLights = source.maxAdditionalLightsCount;
        requestedFoveation = QuestRenderProfile.DefaultFoveation;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var intent = activity.Call<AndroidJavaObject>("getIntent");
            scale = intent.Call<float>("getFloatExtra", "chesscgi.qa.renderScale", scale);
            msaa = intent.Call<int>("getIntExtra", "chesscgi.qa.msaa", msaa);
            requestedFoveation = intent.Call<float>("getFloatExtra", "chesscgi.qa.foveation", requestedFoveation);
            shadowDistance = intent.Call<float>("getFloatExtra", "chesscgi.qa.shadowDistance", shadowDistance);
            additionalLights = intent.Call<int>("getIntExtra", "chesscgi.qa.additionalLights", additionalLights);
        }
        catch (AndroidJavaException exception)
        {
            Debug.LogWarning($"{Prefix} intent_unavailable={exception.GetType().Name}");
        }
#endif
        if (!FiniteInRange(requestedFoveation, 0f, .75f) ||
            !TryCreatePipelineOverride(source, scale, msaa, out RenderPipelineAsset candidate,
                shadowDistance, additionalLights))
        {
            Debug.LogWarning($"{Prefix} rejected_override using_project_settings=true");
            requestedFoveation = 0f;
            runtimePipeline = source;
            return;
        }
        previousQualityPipeline = QualitySettings.renderPipeline;
        runtimePipeline = (UniversalRenderPipelineAsset)candidate;
        ownsPipeline = true;
        QualitySettings.renderPipeline = runtimePipeline;
        Debug.Log(FormattableString.Invariant($"{Prefix} requested scale={scale:F2} msaa={msaa} foveation={requestedFoveation:F2} shadowDistance={shadowDistance:F2} additionalLights={additionalLights}"));
    }

    // Clone only the pipeline asset: renderer data, materials and source assets
    // remain shared and untouched by the experiment.
    public static bool TryCreatePipelineOverride(RenderPipelineAsset source, float scale, int msaa,
        out RenderPipelineAsset candidate, float? shadowDistance = null, int? additionalLights = null)
    {
        candidate = null;
        if (!(source is UniversalRenderPipelineAsset urp) || !FiniteInRange(scale, .75f, 1.5f) ||
            (msaa != 1 && msaa != 2 && msaa != 4)) return false;
        float shadows = shadowDistance ?? urp.shadowDistance;
        int lights = additionalLights ?? urp.maxAdditionalLightsCount;
        if (!FiniteInRange(shadows, 0f, 50f) || lights < 0 || lights > 4) return false;
        var copy = Instantiate(urp);
        copy.name = urp.name + " (Quest diagnostic instance)";
        copy.renderScale = scale;
        copy.msaaSampleCount = msaa;
        copy.shadowDistance = shadows;
        copy.maxAdditionalLightsCount = lights;
        candidate = copy;
        return true;
    }

    private static bool FiniteInRange(float value, float min, float max) =>
        !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;

    private void Update()
    {
        if (Time.unscaledTime < nextSample || runtimePipeline == null) return;
        nextSample = Time.unscaledTime + 2f;
        if (RenderPipelineManager.currentPipeline == null) return;
        SubsystemManager.GetSubsystems(displays);
        XRDisplaySubsystem display = displays.Find(item => item.running && item.GetRenderPassCount() > 0);
        if (display == null) return;
        if (configuredDisplay != display)
        {
            display.foveatedRenderingFlags = XRDisplaySubsystem.FoveatedRenderingFlags.None;
            display.foveatedRenderingLevel = SystemInfo.foveatedRenderingCaps == FoveatedRenderingCaps.None
                ? 0f : requestedFoveation;
            configuredDisplay = display;
        }
        if (!countersInitialized) InitializeCounters();
        display.GetRenderPass(0, out XRDisplaySubsystem.XRRenderPass pass);
        var counters = new StringBuilder();
        for (int i = 0; i < recorders.Count; i++)
            counters.Append(' ').Append(counterNames[i]).Append('=')
                .Append(recorders[i].Count > 0 ? recorders[i].LastValue : -1);
        Debug.Log(FormattableString.Invariant($"{Prefix} sample scale={runtimePipeline.renderScale:F2} msaaRequested={runtimePipeline.msaaSampleCount} hdr={runtimePipeline.supportsHDR} xrTarget={pass.renderTargetDesc.width}x{pass.renderTargetDesc.height} swapchainSamples={pass.renderTargetDesc.msaaSamples} foveationRequested={requestedFoveation:F2} foveationReported={display.foveatedRenderingLevel:F2} shadowDistance={runtimePipeline.shadowDistance:F2} additionalLights={runtimePipeline.maxAdditionalLightsCount} additionalMode={runtimePipeline.additionalLightsRenderingMode} caps={SystemInfo.foveatedRenderingCaps} focused={Application.isFocused}{counters}"));
    }

    private void InitializeCounters()
    {
        countersInitialized = true;
        var available = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(available);
        foreach (string name in new[] { "Draw Calls Count", "SetPass Calls Count", "Triangles Count", "Vertices Count" })
        {
            bool found = false;
            foreach (ProfilerRecorderHandle handle in available)
            {
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                if (description.Name != name) continue;
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(description.Category, name, 1);
                if (recorder.Valid)
                {
                    recorders.Add(recorder);
                    counterNames.Add(name.Replace(" ", ""));
                    found = true;
                }
                else recorder.Dispose();
                break;
            }
            if (!found) Debug.Log($"{Prefix} counter_unavailable={name}");
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused) configuredDisplay = null;
    }

    private void OnDestroy()
    {
        foreach (ProfilerRecorder recorder in recorders) recorder.Dispose();
        // A rejected override retains the source asset and must never destroy it.
        if (runtimePipeline == null || !ownsPipeline) return;
        if (QualitySettings.renderPipeline == runtimePipeline)
            QualitySettings.renderPipeline = previousQualityPipeline;
        Destroy(runtimePipeline);
    }
}
#endif
