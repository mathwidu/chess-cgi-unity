using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class QuestRenderDiagnosticsTests
{
    [TestCase(50f, 4)]
    [TestCase(0f, 4)]
    [TestCase(50f, 0)]
    public void DiagnosticRenderSettingsDoNotMutateTheSavedMobileProfile(float shadowDistance, int additionalLights)
    {
        var source = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        string before = EditorJsonUtility.ToJson(source);
        RenderPipelineAsset originalQuality = QualitySettings.renderPipeline;
        Assert.That(QuestRenderDiagnostics.TryCreatePipelineOverride(source, 1.25f, 2, out var candidate,
            shadowDistance, additionalLights), Is.True);
        try
        {
            Assert.That(candidate, Is.Not.SameAs(source));
            var properties = new SerializedObject(candidate);
            Assert.That(properties.FindProperty("m_RenderScale").floatValue, Is.EqualTo(1.25f));
            Assert.That(properties.FindProperty("m_MSAA").intValue, Is.EqualTo(2));
            Assert.That(properties.FindProperty("m_ShadowDistance").floatValue, Is.EqualTo(shadowDistance));
            Assert.That(properties.FindProperty("m_AdditionalLightsPerObjectLimit").intValue, Is.EqualTo(additionalLights));
            Assert.That(EditorJsonUtility.ToJson(source), Is.EqualTo(before), "Saved visual settings must survive the comparison");
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(originalQuality), "Creating an experiment must not activate it");
        }
        finally { Object.DestroyImmediate(candidate); }
    }

    [TestCase(.5f, 4)]
    [TestCase(2f, 4)]
    [TestCase(float.NaN, 4)]
    [TestCase(float.PositiveInfinity, 4)]
    [TestCase(1f, 3)]
    public void InvalidDiagnosticRequestDoesNotCreateOrActivateAPipeline(float scale, int msaa)
    {
        var source = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        RenderPipelineAsset active = QualitySettings.renderPipeline;
        Assert.That(QuestRenderDiagnostics.TryCreatePipelineOverride(source, scale, msaa, out var candidate), Is.False);
        Assert.That(candidate, Is.Null);
        Assert.That(QualitySettings.renderPipeline, Is.SameAs(active));
    }

    [TestCase(-1f, 4)]
    [TestCase(float.NaN, 4)]
    [TestCase(50f, -1)]
    [TestCase(50f, 5)]
    public void InvalidLightingDiagnosticDoesNotCreateOrActivateAPipeline(float shadowDistance, int additionalLights)
    {
        var source = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
        RenderPipelineAsset active = QualitySettings.renderPipeline;
        Assert.That(QuestRenderDiagnostics.TryCreatePipelineOverride(source, 1.25f, 4, out var candidate,
            shadowDistance, additionalLights), Is.False);
        Assert.That(candidate, Is.Null);
        Assert.That(QualitySettings.renderPipeline, Is.SameAs(active));
    }
}
