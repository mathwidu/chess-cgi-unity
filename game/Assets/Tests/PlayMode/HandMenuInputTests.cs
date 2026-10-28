#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.UI;

public sealed class HandMenuInputTests
{
    private XRSimulatedHMD headset;
    private MetaAimHand aim;
    private MetaAimHand previousAim;
    private bool left;
    private GameObject hand;
    private NearFarInteractor near;
    private GameHud hud;
    private ChessGameController game;
    private float previousCaptureDeltaTime;

    [SetUp]
    public void SetUp()
    {
        previousCaptureDeltaTime = Time.captureDeltaTime;
        Time.captureDeltaTime = 1f / 72f;
    }

    private IEnumerator LoadHand(bool useLeft)
    {
        left = useLeft;
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return Frames(2);
        if (Object.FindFirstObjectByType<XRRig>() == null)
            new GameObject("Menu hand replay bootstrap").AddComponent<XRRig>();
        yield return Frames(2);

        previousAim = left ? MetaAimHand.left : MetaAimHand.right;
        aim = MetaAimHand.CreateHand(left ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right);
        if (left) MetaAimHand.left = aim;
        else MetaAimHand.right = aim;
        Transform offset = XRRig.Origin.Find("Camera Offset");
        offset.GetComponent<XRInputModalityManager>().enabled = false;
        foreach (string name in new[] { "LeftHandInteractor", "RightHandInteractor", "Left Controller", "Right Controller" })
            offset.Find(name).gameObject.SetActive(false);
        hand = offset.Find(left ? "LeftHandInteractor" : "RightHandInteractor").gameObject;
        foreach (TrackedPoseDriver driver in hand.GetComponentsInChildren<TrackedPoseDriver>(true))
            driver.enabled = false;
        foreach (XRPokeInteractor poke in hand.GetComponentsInChildren<XRPokeInteractor>(true))
            poke.enabled = false;
        near = hand.GetComponentInChildren<NearFarInteractor>(true);
        near.enableNearCasting = false;
        var far = (CurveInteractionCaster)near.farInteractionCaster;
        far.enableStabilization = false;
        hud = Object.FindFirstObjectByType<GameHud>();
        game = Object.FindFirstObjectByType<ChessGameController>();
        Assert.That(game.IsMenuOpen, Is.True);
        Assert.That(hud.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.WorldSpace));
        Assert.That(UnityEngine.EventSystems.EventSystem.current.currentInputModule, Is.TypeOf<XRUIInputModule>());
        QueuePinch(0f);
        hand.SetActive(true);
        Canvas.ForceUpdateCanvases();
        yield return Frames(3);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (aim != null && aim.added) InputSystem.RemoveDevice(aim);
        if (left && MetaAimHand.left == aim) MetaAimHand.left = previousAim;
        if (!left && MetaAimHand.right == aim) MetaAimHand.right = previousAim;
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after menu hand test"));
        yield return SceneManager.UnloadSceneAsync(previous);
        Time.captureDeltaTime = previousCaptureDeltaTime;
    }

    [UnityTest]
    public IEnumerator LeftHandRelaxedPinchesChooseLocalModeAndStartThroughTheRealMenu()
        => StartLocalGameThroughMenu(true);

    [UnityTest]
    public IEnumerator RightHandRelaxedPinchesChooseLocalModeAndStartThroughTheRealMenu()
        => StartLocalGameThroughMenu(false);

    [UnityTest]
    public IEnumerator RelaxingAHeldPinchDoesNotClickUntilTheFingersOpen()
    {
        yield return LoadHand(true);
        Button button = ButtonNamed("LocalModeButton");
        int clicks = 0;
        button.onClick.AddListener(() => clicks++);
        yield return AimAt(button);
        QueuePinch(.65f);
        yield return Frames(3);
        QueuePinch(.45f);
        yield return Frames(3);
        Assert.That(clicks, Is.Zero, "A partial pinch must not click.");
        QueuePinch(.85f);
        yield return Frames(3);
        QueuePinch(.65f);
        yield return Frames(3);
        Assert.That(near.uiPressInput.ReadIsPerformed(), Is.True);
        Assert.That(clicks, Is.Zero, "A small strength fluctuation must not release the pressed button.");
        QueuePinch(.45f);
        yield return Frames(4);
        Assert.That(clicks, Is.EqualTo(1));
        yield return Frames(4);
        Assert.That(clicks, Is.EqualTo(1), "Opening must click only once.");
    }

    [UnityTest]
    public IEnumerator TrackingLossCancelsAPressedMenuButtonAndClosedFingersCannotClickOnReturn()
        => InterruptPressedButton(null);

    [UnityTest]
    public IEnumerator FocusLossCancelsAPressedMenuButtonAndClosedFingersCannotClickOnReturn()
        => InterruptPressedButton("OnApplicationFocus");

    [UnityTest]
    public IEnumerator PauseCancelsAPressedMenuButtonAndClosedFingersCannotClickOnReturn()
        => InterruptPressedButton("OnApplicationPause");

    [UnityTest]
    public IEnumerator SwitchingAwayFromAndBackToHandsCancelsThePendingMenuPress()
        => InterruptPressedButton("Modality");

    [UnityTest]
    public IEnumerator FocusLossAndReturnWithinOneFrameStillCancelThePendingPress()
        => InterruptPressedButton("OnApplicationFocus", false);

    [UnityTest]
    public IEnumerator PauseAndReturnWithinOneFrameStillCancelThePendingPress()
        => InterruptPressedButton("OnApplicationPause", false);

    private IEnumerator InterruptPressedButton(string interruption, bool waitDuringInterruption = true)
    {
        yield return LoadHand(true);
        Button button = ButtonNamed("LocalModeButton");
        int clicks = 0;
        button.onClick.AddListener(() => clicks++);
        yield return AimAt(button);
        QueuePinch(.85f);
        yield return Frames(3);
        Assert.That(near.TryGetUIModel(out var pressed), Is.True);
        Assert.That(pressed.select, Is.True, "The actual UI model must be pressed before the interruption.");
        if (interruption == null) QueuePinch(.85f, false);
        else if (interruption == "Modality") hand.SetActive(false);
        else hand.BroadcastMessage(interruption, interruption == "OnApplicationPause", SendMessageOptions.DontRequireReceiver);
        if (waitDuringInterruption) yield return Frames(3);
        Assert.That(clicks, Is.Zero, "Losing a valid hand must cancel the press, not click the button.");

        if (interruption == null) QueuePinch(.85f);
        else if (interruption == "Modality") hand.SetActive(true);
        else hand.BroadcastMessage(interruption, interruption == "OnApplicationFocus", SendMessageOptions.DontRequireReceiver);
        yield return Frames(3);
        Assert.That(clicks, Is.Zero);
        QueuePinch(.45f);
        yield return Frames(3);
        Assert.That(clicks, Is.Zero, "Opening after the interruption only rearms the pinch.");
        yield return AimAt(button);
        QueuePinch(.85f);
        yield return Frames(3);
        QueuePinch(.45f);
        yield return Frames(3);
        Assert.That(clicks, Is.EqualTo(1), "A fresh valid pinch after opening must click normally.");
    }

    private IEnumerator StartLocalGameThroughMenu(bool useLeft)
    {
        yield return LoadHand(useLeft);
        Button mode = ButtonNamed("LocalModeButton");
        int clicks = 0;
        mode.onClick.AddListener(() => clicks++);
        yield return AimAt(mode);
        QueuePinch(.85f);
        yield return Frames(3);
        Assert.That(near.uiPressInput.ReadValue(), Is.EqualTo(.85f).Within(.001f), "Feed the native hand device, without bypassing the UI reader.");
        QueuePinch(.45f);
        yield return Frames(3);
        Assert.That(clicks, Is.EqualTo(1), "A deliberate pinch and opening over the real button must click once.");

        yield return AimAt(ButtonNamed("StartPlayButton"));
        QueuePinch(.85f);
        yield return Frames(3);
        QueuePinch(.45f);
        yield return Frames(3);
        Assert.That(game.IsMenuOpen, Is.False, "The native hand pinch must be able to start the game through its menu.");
        Assert.That(game.IsAgainstComputer, Is.False);
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }

    private Button ButtonNamed(string name) => hud.GetComponentsInChildren<Button>(true).Single(button => button.name == name);

    private IEnumerator AimAt(Button button)
    {
        Canvas.ForceUpdateCanvases();
        RectTransform panel = (RectTransform)hud.transform;
        RectTransform rect = (RectTransform)button.transform;
        Vector3 target = rect.TransformPoint(rect.rect.center);
        Transform ray = near.farInteractionCaster.castOrigin;
        ray.SetPositionAndRotation(target - panel.forward * .3f, Quaternion.LookRotation(panel.forward, panel.up));
        Physics.SyncTransforms();
        yield return Frames(4);
        Assert.That(near.TryGetUIModel(out var model), Is.True);
        Assert.That(model.currentRaycast.isValid, Is.True, "The real XR UI ray must hit the menu before testing its click.");
        Assert.That(model.currentRaycast.gameObject.GetComponentInParent<Button>(), Is.SameAs(button), "The ray must hover the requested button.");
    }

    private void QueuePinch(float strength, bool tracked = true)
    {
        aim.UpdateHand(tracked, MetaAimFlags.Computed | MetaAimFlags.Valid, Pose.identity, strength, 0f, 0f, 0f);
        InputSystem.Update();
    }

    private static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) yield return null;
    }
}
#endif
