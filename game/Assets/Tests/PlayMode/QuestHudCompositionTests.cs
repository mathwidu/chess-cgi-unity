#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

public sealed class QuestHudCompositionTests
{
    private Canvas canvas;
    private Camera eye;
    private XRUIInputModule input;
    private TrackedDeviceGraphicRaycaster raycaster;
    private QuestHudComposition composition;
    private UniversalRenderPipelineAsset pipeline;
    private float previousRenderScale;
    private Scene previousScene;
    private Scene fixtureScene;
    private readonly List<Texture2D> sourceTextures = new List<Texture2D>();

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        Assert.That(pipeline, Is.Not.Null, "These capture regressions require the project's real URP renderer.");
        previousRenderScale = pipeline.renderScale;
        // The initial scene may own the TestRunner coroutine. Keep it loaded,
        // and only unload the scene this fixture creates in TearDown.
        previousScene = SceneManager.GetActiveScene();
        fixtureScene = SceneManager.CreateScene("Quest HUD capture fixture");
        SceneManager.SetActiveScene(fixtureScene);

        eye = new GameObject("Fixture eye").AddComponent<Camera>();
        eye.enabled = true;
        eye.nearClipPlane = .01f;
        eye.farClipPlane = 10f;
        input = new GameObject("Fixture XR events", typeof(EventSystem)).AddComponent<XRUIInputModule>();
        input.uiCamera = eye;
        var root = new GameObject("Fixture world HUD", typeof(RectTransform));
        root.layer = 9;
        canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = eye;
        var rect = (RectTransform)root.transform;
        rect.sizeDelta = new Vector2(1024f, 576f);
        rect.localScale = Vector3.one * .001f;
        rect.position = new Vector3(0f, 0f, 2f);
        raycaster = root.AddComponent<TrackedDeviceGraphicRaycaster>();
        raycaster.checkFor3DOcclusion = true;
        raycaster.blockingMask = ~(1 << XRPhysicsPusher.PhysicsLayer);
        composition = root.AddComponent<QuestHudComposition>();
        composition.Configure(canvas);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (pipeline != null) pipeline.renderScale = previousRenderScale;
        if (composition != null) composition.enabled = false;
        foreach (Texture2D texture in sourceTextures)
            if (texture != null) Object.Destroy(texture);
        sourceTextures.Clear();
        if (previousScene.IsValid() && previousScene.isLoaded)
            SceneManager.SetActiveScene(previousScene);
        if (fixtureScene.IsValid() && fixtureScene.isLoaded)
        {
            if (SceneManager.GetActiveScene() == fixtureScene)
                SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after quest HUD capture test"));
            yield return SceneManager.UnloadSceneAsync(fixtureScene);
        }
    }

    [UnityTest]
    public IEnumerator OneTexelStripesRetainTheirContrastWhenTheSceneRendersAtSixtyFivePercent()
    {
        int width = QuestHudComposition.TextureWidth;
        var stripes = new Texture2D(width, 1, TextureFormat.RGBA32, false, true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        sourceTextures.Add(stripes);
        var pixels = new Color32[width];
        for (int x = 0; x < width; x++) pixels[x] = (x & 1) == 0 ? Color.black : Color.white;
        stripes.SetPixels32(pixels);
        stripes.Apply();
        RawImage image = GraphicAt<RawImage>("One output texel stripes", Vector2.one * .5f, Vector2.zero);
        var rect = (RectTransform)image.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        image.texture = stripes;

        pipeline.renderScale = .65f;
        // Canvas render data is registered by an actual player frame. Do not
        // conflate an unbuilt synthetic Canvas with a failed capture request.
        yield return null;
        Activate();
        yield return null;
        CaptureNow();
        Assert.That(composition.CaptureCamera.GetComponent<QuestHudCaptureCamera>().SceneRenderScale,
            Is.EqualTo(.65f).Within(.001f),
            "URP must apply the scene scale before overriding this target, never the Preview camera's global scale 1.");
        Color32[] reducedScene = ReadRow(composition.CaptureTexture);
        pipeline.renderScale = 1f;
        CaptureNow();
        Color32[] fullScene = ReadRow(composition.CaptureTexture);

        float contrast = 0f, difference = 0f;
        int samples = width - 33;
        for (int x = 16; x < width - 17; x++)
        {
            contrast += Mathf.Abs(reducedScene[x + 1].r - reducedScene[x].r) / 255f;
            difference += Mathf.Abs(reducedScene[x].r - fullScene[x].r) / 255f;
        }
        Assert.That(contrast / samples, Is.GreaterThan(.9f),
            "One-texel black/white detail must survive. A fixed-size RT alone does not prove it avoided URP downsampling.");
        Assert.That(difference / samples, Is.LessThan(.02f),
            "Changing the scene render scale must not change the HUD's sampled detail.");
        yield return null;
    }

    [UnityTest]
    public IEnumerator CapturedCoverageAndResolvedColorPreserveTranslucentAndOverlappingUi()
    {
        Color color = new Color(.2f, .5f, .7f, 1f);
        Image opaque = ImageAt("Opaque reference", .25f, color);
        color.a = .5f;
        Image translucent = ImageAt("Half coverage", .5f, color);
        ImageAt("Overlap back", .75f, color);
        ImageAt("Overlap front", .75f, color);
        yield return null;
        Activate();
        yield return null;
        CaptureNow();

        Color opaqueCoverage = ReadPixel(composition.CaptureTexture, .25f, .5f);
        Color halfCoverage = ReadPixel(composition.CaptureTexture, .5f, .5f);
        Color overlapCoverage = ReadPixel(composition.CaptureTexture, .75f, .5f);
        Color emptyCoverage = ReadPixel(composition.CaptureTexture, .03f, .03f);
        Assert.That(opaqueCoverage.a, Is.GreaterThan(.98f));
        Assert.That(halfCoverage.a, Is.EqualTo(.5f).Within(.025f), "The depth mask needs true UI coverage, not alpha squared.");
        Assert.That(overlapCoverage.a, Is.EqualTo(.75f).Within(.025f), "Two half-covered surfaces must combine to 75% coverage.");
        Assert.That(emptyCoverage.a, Is.LessThan(.01f));
        Assert.That(halfCoverage.b, Is.LessThan(opaqueCoverage.b * .9f), "The capture must contain premultiplied color before resolve.");

        Color reference = ReadPixel(composition.LayerTexture, .25f, .5f);
        Color resolvedHalf = ReadPixel(composition.LayerTexture, .5f, .5f);
        Color resolvedOverlap = ReadPixel(composition.LayerTexture, .75f, .5f);
        AssertRgbClose(resolvedHalf, reference, "Resolving half coverage must recover the original color without dark or saturated fringes.");
        AssertRgbClose(resolvedOverlap, reference, "Overlap coverage must not change the straight color.");
        Assert.That(resolvedHalf.a, Is.GreaterThan(.99f));
        Assert.That(resolvedOverlap.a, Is.GreaterThan(.99f));
        Color emptyLayer = ReadPixel(composition.LayerTexture, .03f, .03f);
        AssertRgbClose(emptyLayer, Color.black, "Uncovered texels must resolve to black.");
        Assert.That(emptyLayer.a, Is.GreaterThan(.99f), "Coverage belongs to the eye mask; the underlay is opaque.");

        translucent.color = Color.green;
        yield return null;
        CaptureNow();
        Color changed = ReadPixel(composition.LayerTexture, .5f, .5f);
        Assert.That(changed.g, Is.GreaterThan(.95f));
        Assert.That(changed.r + changed.b, Is.LessThan(.05f), "A changed UI graphic must replace its previous pixels.");
        translucent.gameObject.SetActive(false);
        yield return null;
        CaptureNow();
        Assert.That(ReadPixel(composition.CaptureTexture, .5f, .5f).a, Is.LessThan(.01f), "Hiding UI must clear the previous frame's coverage.");
        AssertRgbClose(ReadPixel(composition.LayerTexture, .5f, .5f), Color.black, "Hidden UI must not leave a stale underlay image.");
        Assert.That(opaque.enabled, Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator CaptureKeepsTheOriginalRaycastAndCamerasWhileReconfigurationAndDisableRestoreOwnership()
    {
        Image image = ImageAt("Original button", .5f, Color.red);
        image.gameObject.layer = 5;
        image.gameObject.AddComponent<Button>().targetGraphic = image;
        Image hidden = ImageAt("Inactive graphic", .25f, Color.blue);
        hidden.gameObject.layer = 8;
        hidden.gameObject.SetActive(false);
        int originalMask = eye.cullingMask;
        Vector3 eyePosition = eye.transform.position;
        Quaternion eyeRotation = eye.transform.rotation;
        yield return null;
        Activate();
        Assert.That(composition.GetComponentsInChildren<Collider>(true).Any(c => c.enabled && c.gameObject.activeInHierarchy), Is.False,
            "The visual depth surface must never intercept UI rays, including its creation frame.");
        yield return null;
        CaptureNow();
        RenderTexture firstCapture = composition.CaptureTexture;
        RenderTexture firstLayer = composition.LayerTexture;
        Camera firstCamera = composition.CaptureCamera;

        Assert.That(canvas.worldCamera, Is.SameAs(eye));
        Assert.That(input.uiCamera, Is.SameAs(eye));
        Assert.That(EventSystem.current.currentInputModule, Is.SameAs(input));
        Assert.That(eye.enabled, Is.True, "Capturing the HUD must keep the eye camera rendering the scene.");
        Assert.That(eye.targetTexture, Is.Null);
        Assert.That(eye.transform.position, Is.EqualTo(eyePosition));
        Assert.That(eye.transform.rotation, Is.EqualTo(eyeRotation));
        Assert.That(eye.cullingMask, Is.EqualTo(originalMask & ~(1 << QuestHudComposition.CaptureLayer)));
        Assert.That(image.enabled && image.raycastTarget, Is.True);
        Assert.That(hidden.gameObject.activeSelf, Is.False);
        Assert.That(hidden.gameObject.layer, Is.EqualTo(QuestHudComposition.CaptureLayer));
        AssertRayHits(image);

        Image replacement = ImageAt("Rebuilt control", .75f, Color.yellow);
        replacement.gameObject.layer = 10;
        composition.Configure(canvas);
        yield return null;
        CaptureNow();
        Assert.That(composition.CaptureTexture, Is.SameAs(firstCapture), "Rebuilding child controls must reuse the capture resources.");
        Assert.That(composition.GetComponentsInChildren<Camera>(true).Length, Is.EqualTo(1));
        Assert.That(replacement.gameObject.layer, Is.EqualTo(QuestHudComposition.CaptureLayer));
        AssertRayHits(replacement);
        Assert.That(composition.GetComponentsInChildren<Collider>(true), Is.Empty);
        // Other code can change unrelated mask bits while composition owns only its capture bit.
        eye.cullingMask ^= 1 << 17;
        int expectedRestoredMask = originalMask ^ (1 << 17);
        composition.enabled = false;
        Assert.That(composition.IsActive, Is.False);
        Assert.That(composition.CaptureCamera, Is.Null);
        Assert.That(composition.CaptureTexture, Is.Null);
        Assert.That(composition.LayerTexture, Is.Null);
        Assert.That(firstCapture.IsCreated() || firstLayer.IsCreated(), Is.False, "Disabling must immediately release both GPU allocations.");
        Assert.That(firstCamera.targetTexture, Is.Null);
        Assert.That(canvas.gameObject.layer, Is.EqualTo(9));
        Assert.That(image.gameObject.layer, Is.EqualTo(5));
        Assert.That(hidden.gameObject.layer, Is.EqualTo(8));
        Assert.That(replacement.gameObject.layer, Is.EqualTo(10));
        Assert.That(eye.cullingMask, Is.EqualTo(expectedRestoredMask));
        Assert.That(canvas.worldCamera, Is.SameAs(eye));
        Assert.That(input.uiCamera, Is.SameAs(eye));
        yield return null;
        Assert.That(firstCamera == null && firstCapture == null && firstLayer == null, Is.True,
            "Released cameras and textures must also be destroyed after the frame.");

        // An eye that did not render this layer before activation must keep excluding it afterwards.
        eye.cullingMask &= ~(1 << QuestHudComposition.CaptureLayer);
        int initiallyExcludedMask = eye.cullingMask;
        composition.enabled = true;
        Activate();
        yield return null;
        CaptureNow();
        Assert.That(ReadPixel(composition.LayerTexture, .75f, .5f).r, Is.GreaterThan(.95f));
        composition.enabled = false;
        Assert.That(eye.cullingMask, Is.EqualTo(initiallyExcludedMask));
    }

    [UnityTest]
    public IEnumerator MovingAndResizingTheCanvasKeepsTheCapturedImageAndVisualSurfaceOnItsRaycastPlane()
    {
        Image image = ImageAt("Off-centre marker", .75f, Color.green);
        yield return null;
        Activate();
        yield return null;
        CaptureNow();
        Assert.That(ReadPixel(composition.LayerTexture, .75f, .5f).g, Is.GreaterThan(.95f));
        var rect = (RectTransform)canvas.transform;
        rect.pivot = new Vector2(.2f, .8f);
        rect.sizeDelta = new Vector2(1600f, 900f);
        rect.SetPositionAndRotation(new Vector3(-1.3f, 1.6f, -2f), Quaternion.Euler(8f, 180f, 0f));
        rect.localScale = new Vector3(.0012f, .0014f, .0012f);
        // A seat change moves both the player and the HUD to the other side.
        eye.transform.SetPositionAndRotation(rect.TransformPoint(rect.rect.center) - rect.forward * 2f, rect.rotation);
        yield return null;
        CaptureNow();

        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        Transform surface = composition.GetComponentInChildren<MeshRenderer>(true).transform;
        Vector3[] surfaceCorners =
        {
            surface.TransformPoint(new Vector3(-.5f, -.5f)), surface.TransformPoint(new Vector3(-.5f, .5f)),
            surface.TransformPoint(new Vector3(.5f, .5f)), surface.TransformPoint(new Vector3(.5f, -.5f))
        };
        foreach (Vector3 corner in corners)
            Assert.That(surfaceCorners.Min(candidate => Vector3.Distance(corner, candidate)), Is.LessThan(.001f),
                "The visible surface must cover exactly the same world rectangle as the original controls.");
        Color marker = ReadPixel(composition.LayerTexture, .75f, .5f);
        Assert.That(marker.g, Is.GreaterThan(.95f), "The capture must follow the new pose, pivot, scale and aspect.");
        Assert.That(marker.r + marker.b, Is.LessThan(.05f));
        Assert.That(ReadPixel(composition.CaptureTexture, .25f, .5f).a, Is.LessThan(.01f), "The marker must not become mirrored or stretched into another control's area.");
        AssertRayHits(image);
        yield return null;
    }

    private Image ImageAt(string name, float x, Color color)
    {
        Image image = GraphicAt<Image>(name, new Vector2(x, .5f), new Vector2(160f, 160f));
        image.color = color;
        return image;
    }

    private T GraphicAt<T>(string name, Vector2 anchor, Vector2 size) where T : Graphic
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)item.transform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.sizeDelta = size;
        return item.AddComponent<T>();
    }

    private void Activate()
    {
        InvokePrivate("Activate", eye, false);
        Assert.That(composition.IsActive, Is.True, "Capture must activate without requiring a native compositor in the Editor.");
    }

    private void CaptureNow()
    {
        composition.Configure(canvas);
        InvokePrivate("LateUpdate");
        Assert.That(composition.IsActive, Is.True, "A failed capture must not silently turn the regression into a Canvas fallback.");
        Assert.That(composition.CaptureTexture.IsCreated() && composition.LayerTexture.IsCreated(), Is.True);
    }

    private void InvokePrivate(string name, params object[] arguments)
    {
        MethodInfo method = typeof(QuestHudComposition).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null, "Missing capture test entry point: " + name);
        method.Invoke(composition, arguments);
    }

    private void AssertRayHits(Graphic graphic)
    {
        Canvas.ForceUpdateCanvases();
        Vector3 target = graphic.rectTransform.TransformPoint(graphic.rectTransform.rect.center);
        Vector3 forward = canvas.transform.forward;
        var data = new TrackedDeviceEventData(EventSystem.current)
        {
            rayPoints = new List<Vector3> { target - forward * .3f, target + forward * .1f },
            layerMask = ~0
        };
        var hits = new List<RaycastResult>();
        raycaster.Raycast(data, hits);
        Assert.That(hits.Any(hit => hit.gameObject == graphic.gameObject), Is.True,
            "The original graphic must remain raycastable after rendering to its separate texture: " + graphic.name);
    }

    private static Color ReadPixel(RenderTexture target, float x, float y)
    {
        var pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        RenderTexture previous = RenderTexture.active;
        try
        {
            RenderTexture.active = target;
            pixel.ReadPixels(new Rect(Mathf.FloorToInt(x * target.width), Mathf.FloorToInt(y * target.height), 1, 1), 0, 0);
            pixel.Apply();
            return pixel.GetPixel(0, 0);
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(pixel);
        }
    }

    private static Color32[] ReadRow(RenderTexture target)
    {
        var row = new Texture2D(target.width, 1, TextureFormat.RGBA32, false, true);
        RenderTexture previous = RenderTexture.active;
        try
        {
            RenderTexture.active = target;
            row.ReadPixels(new Rect(0, target.height / 2, target.width, 1), 0, 0);
            row.Apply();
            return row.GetPixels32();
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(row);
        }
    }

    private static void AssertRgbClose(Color actual, Color expected, string message)
    {
        float difference = Mathf.Max(Mathf.Abs(actual.r - expected.r), Mathf.Abs(actual.g - expected.g), Mathf.Abs(actual.b - expected.b));
        Assert.That(difference, Is.LessThan(.035f), message + $" Actual {actual}; expected {expected}.");
    }
}
#endif
