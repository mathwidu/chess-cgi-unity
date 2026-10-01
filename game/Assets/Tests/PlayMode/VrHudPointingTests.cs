#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class VrHudPointingTests
{
    private static readonly int AnyLayer = ~(1 << XRPhysicsPusher.PhysicsLayer);
    private GameObject panelObject;
    private RectTransform panel;
    private readonly List<GameObject> created = new List<GameObject>();
    private InputDevice headset;

    [SetUp]
    public void SetUp()
    {
        panelObject = new GameObject("Fixture panel", typeof(RectTransform));
        panel = (RectTransform)panelObject.transform;
        panel.sizeDelta = new Vector2(1920f, 1080f);
        panel.localScale = Vector3.one * 0.0012f;
        panel.SetPositionAndRotation(new Vector3(0f, 1.6f, 1.35f), Quaternion.identity);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(panelObject);
        foreach (GameObject item in created) Object.DestroyImmediate(item);
        created.Clear();
    }

    private static Ray RayTo(Vector3 from, Vector3 to) => new Ray(from, to - from);
    private Vector3 PanelPoint(float x, float y) => panel.TransformPoint(new Vector3(x, y, 0f));

    [Test]
    public void ARayAtTheEmptyPanelFindsItsPoint()
    {
        Vector3 origin = new Vector3(0.2f, 1.2f, -0.4f);
        Vector3 target = PanelPoint(-800f, 450f);
        Assert.That(HudAim.TryGetPanelPoint(RayTo(origin, target), panel, 0f, 10f, AnyLayer, out Vector3 point), Is.True);
        Assert.That(Vector3.Distance(point, target), Is.LessThan(.001f));
    }

    [Test]
    public void TheMarginWidensTheAreaPastThePanelEdge()
    {
        Vector3 origin = new Vector3(0f, 1.2f, -0.4f);
        Vector3 justOutside = PanelPoint(960f + 100f, 0f);
        Assert.That(HudAim.TryGetPanelPoint(RayTo(origin, justOutside), panel, 0f, 10f, AnyLayer, out _), Is.False);
        Assert.That(HudAim.TryGetPanelPoint(RayTo(origin, justOutside), panel, 150f, 10f, AnyLayer, out _), Is.True);
        Assert.That(HudAim.TryGetPanelPoint(RayTo(origin, PanelPoint(960f + 400f, 0f)), panel, 150f, 10f, AnyLayer, out _), Is.False);
    }

    [Test]
    public void APanelBehindOrBeyondRangeIsNotAimedAt()
    {
        Vector3 origin = new Vector3(0f, 1.2f, -0.4f);
        Assert.That(HudAim.TryGetPanelPoint(new Ray(origin, Vector3.back), panel, 150f, 10f, AnyLayer, out _), Is.False);
        Assert.That(HudAim.TryGetPanelPoint(RayTo(origin, PanelPoint(0f, 0f)), panel, 150f, 1f, AnyLayer, out _), Is.False);
    }

    [Test]
    public void AnObstacleInFrontOfThePanelHidesTheAimButAHandProxyDoesNot()
    {
        Vector3 origin = new Vector3(0f, 1.2f, -0.4f);
        Ray ray = RayTo(origin, PanelPoint(0f, 0f));
        GameObject proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        created.Add(proxy);
        proxy.layer = XRPhysicsPusher.PhysicsLayer;
        proxy.transform.position = Vector3.Lerp(origin, PanelPoint(0f, 0f), .4f);
        Physics.SyncTransforms();
        Assert.That(HudAim.TryGetPanelPoint(ray, panel, 0f, 10f, AnyLayer, out _), Is.True, "Contact proxies never block the ray.");

        proxy.layer = 0;
        Physics.SyncTransforms();
        Assert.That(HudAim.TryGetPanelPoint(ray, panel, 0f, 10f, AnyLayer, out _), Is.False, "The board, pieces and table do.");
    }

    [UnityTest]
    public IEnumerator VrHudButtonsGrowTheirHitAreaWithoutReachingTheirNeighbours()
    {
        headset = InputSystem.AddDevice("XRHMD");
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        var hud = Object.FindFirstObjectByType<GameHud>();
        hud.RebuildInterface();
        yield return null;
        Canvas.ForceUpdateCanvases();

        Button isolated = ButtonNamed(hud, "CloseHelpButton");
        Vector4 padding = isolated.targetGraphic.raycastPadding;
        Assert.That(padding, Is.EqualTo(new Vector4(-16f, -16f, -16f, -16f)), "A button with nothing around it gets the full padding.");

        Button left = ButtonNamed(hud, "PreviewRotateLeftButton");
        Button right = ButtonNamed(hud, "PreviewRotateRightButton");
        Rect leftArea = HitArea(left), rightArea = HitArea(right);
        Assert.That(leftArea.width, Is.GreaterThan(((RectTransform)left.transform).rect.width), "Crowded buttons still grow where there is room.");
        Assert.That(leftArea.xMax, Is.LessThanOrEqualTo(rightArea.xMin + .001f), "Neighbours never share a hit area.");

        Selectable[] controls = hud.GetComponentsInChildren<Selectable>(true).Where(s => s.targetGraphic != null && s.targetGraphic.raycastTarget).ToArray();
        foreach (Selectable control in controls)
        {
            Vector4 p = control.targetGraphic.raycastPadding;
            Assert.That(p.x <= 0 && p.y <= 0 && p.z <= 0 && p.w <= 0 && p.x >= -16f && p.y >= -16f && p.z >= -16f && p.w >= -16f, Is.True, control.name);
        }
    }

    [UnityTearDown]
    public IEnumerator TearDownScene()
    {
        if (headset == null) yield break;
        InputSystem.RemoveDevice(headset);
        headset = null;
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after hud pointing test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    private static Button ButtonNamed(GameHud hud, string name) => hud.GetComponentsInChildren<Button>(true).First(b => b.name == name);

    private static Rect HitArea(Selectable control)
    {
        Rect rect = ((RectTransform)control.transform).rect;
        Vector4 padding = control.targetGraphic.raycastPadding;
        Vector2 origin = ((RectTransform)control.transform).anchoredPosition;
        return Rect.MinMaxRect(origin.x + rect.xMin + padding.x, origin.y + rect.yMin + padding.y,
            origin.x + rect.xMax - padding.z, origin.y + rect.yMax - padding.w);
    }
}
#endif
