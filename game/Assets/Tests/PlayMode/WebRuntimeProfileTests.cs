#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class WebRuntimeProfileTests
{
    private RenderPipelineAsset previousPipeline;
    private RenderPipelineAsset source;
    private string sourceBefore;
    private int previousTargetFrameRate;
    private int previousVSyncCount;
    private GameObject runtimeObject;
    private WebRuntimeProfile profile;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousPipeline = QualitySettings.renderPipeline;
        previousTargetFrameRate = Application.targetFrameRate;
        previousVSyncCount = QualitySettings.vSyncCount;
        source = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/Web_RPAsset.asset");
        Assert.That(source, Is.Not.Null);
        sourceBefore = EditorJsonUtility.ToJson(source);
        QualitySettings.renderPipeline = source;
        runtimeObject = new GameObject("Web runtime profile test");
        profile = runtimeObject.AddComponent<WebRuntimeProfile>();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        try
        {
            if (runtimeObject != null) Object.Destroy(runtimeObject);
            yield return null;
            Assert.That(QualitySettings.renderPipeline, Is.SameAs(source), "Destroying the Web profile must restore its previous pipeline.");
            Assert.That(EditorJsonUtility.ToJson(source), Is.EqualTo(sourceBefore), "Runtime sampling must not rewrite the saved Web profile.");
        }
        finally
        {
            QualitySettings.renderPipeline = previousPipeline;
            Application.targetFrameRate = previousTargetFrameRate;
            QualitySettings.vSyncCount = previousVSyncCount;
        }
    }

    [Test]
    public void SustainedSlowSamplesKeepCharacterRenderResolutionStable()
    {
        Assert.That(QualitySettings.renderPipeline, Is.Not.SameAs(source));
        Assert.That(RenderScale(), Is.EqualTo(1f));
        Assert.That(new SerializedObject(QualitySettings.renderPipeline).FindProperty("m_MSAA").intValue, Is.EqualTo(2),
            "This checks requested MSAA; device support must be measured in the browser.");
        MethodInfo sample = typeof(WebRuntimeProfile).GetMethod("RecordFrameSample", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(sample, Is.Not.Null);
        for (int i = 1; i <= 12; i++)
        {
            sample.Invoke(profile, new object[] { 20f, 1d, true });
            Assert.That(RenderScale(), Is.EqualTo(1f),
                $"Slow sample {i} must not silently remove character detail. FPS is observed, not an acceptance threshold.");
        }
        sample.Invoke(profile, new object[] { 60f, 1d, true });
        profile.OnWebVisibility("1");
        profile.OnWebVisibility("0");
        sample.Invoke(profile, new object[] { 20f, 1d, false });
        Assert.That(RenderScale(), Is.EqualTo(1f), "Recovery and visibility changes must preserve render resolution.");
    }

    private static float RenderScale() => new SerializedObject(QualitySettings.renderPipeline).FindProperty("m_RenderScale").floatValue;
}
#endif
