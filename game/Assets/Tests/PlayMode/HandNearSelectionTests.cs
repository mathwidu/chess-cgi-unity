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
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class HandNearSelectionTests
{
    private XRSimulatedHMD headset;
    private BoardView board;
    private ChessGameController game;
    private Transform offset;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        if (Object.FindFirstObjectByType<XRRig>() == null)
            new GameObject("Hand near-selection replay bootstrap").AddComponent<XRRig>();
        yield return null;
        yield return null;

        Assert.That(XRRig.Origin, Is.Not.Null);
        offset = XRRig.Origin.Find("Camera Offset");
        offset.GetComponent<XRInputModalityManager>().enabled = false;
        foreach (string name in new[] { "LeftHandInteractor", "RightHandInteractor", "Left Controller", "Right Controller" })
        {
            GameObject root = offset.Find(name).gameObject;
            root.SetActive(false);
            foreach (TrackedPoseDriver driver in root.GetComponentsInChildren<TrackedPoseDriver>(true))
                driver.enabled = false;
        }

        board = Object.FindFirstObjectByType<BoardView>();
        game = Object.FindFirstObjectByType<ChessGameController>();
        game.StartLocalGame();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after hand near-selection test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator PinchingBeforeApproachingAPieceAcquiresItAndOpeningTheHandReleasesIt()
    {
        foreach (string hand in new[] { "LeftHandInteractor", "RightHandInteractor" })
        {
            PieceView pawn = Pawn();
            XRGrabInteractable grab = pawn.GetComponent<XRGrabInteractable>();
            NearFarInteractor near = ActivateInteractor(hand, pawn.transform.position + Vector3.up);
            yield return PhysicsSteps(2);
            Assert.That(near.hasHover, Is.False, "The pinch must begin outside every near target.");

            near.selectInput.QueueManualState(true, 1f);
            yield return PhysicsSteps(3);
            Assert.That(near.selectInput.ReadIsPerformed(), Is.True);
            Assert.That(near.hasSelection, Is.False);

            // Keep the same pinch held while the real caster enters the piece.
            // No XRInteractionManager.SelectEnter call or fresh press can hide the regression.
            PositionNearInteractor(near, grab.colliders[0].bounds.center);
            yield return PhysicsSteps(3);
            // NearFar stops casting once selected, so held pieces need not remain hovered.
            Assert.That(near.IsSelecting(grab), Is.True, "The native near interactor must own the intended pawn.");
            Assert.That(grab.isSelected, Is.True,
                $"{hand} must acquire a piece reached during an already held pinch; pressed={near.selectInput.ReadIsPerformed()}, hover={near.hasHover}, selected={game.SelectedPiece?.name}.");
            Assert.That(game.SelectedPiece, Is.SameAs(pawn));

            near.selectInput.QueueManualState(false, 0f);
            yield return PhysicsSteps(3);
            Assert.That(grab.isSelected, Is.False, "Opening the fingers must still release the piece.");
            Assert.That(game.SelectedPiece, Is.Null);
            Assert.That(game.MoveHistory.Count, Is.Zero, "Grabbing and releasing on the original square is not a move.");
            near.transform.parent.gameObject.SetActive(false);
        }
    }

    [UnityTest]
    public IEnumerator ASecondPinchedHandWaitsForTheFirstHandToReleaseThePiece()
    {
        PieceView pawn = Pawn();
        XRGrabInteractable grab = pawn.GetComponent<XRGrabInteractable>();
        int acquisitions = 0;
        grab.selectEntered.AddListener(_ => acquisitions++);
        NearFarInteractor first = ActivateInteractor("LeftHandInteractor", grab.colliders[0].bounds.center);
        yield return PhysicsSteps(2);
        first.selectInput.QueueManualState(true, 1f);
        yield return PhysicsSteps(3);
        Assert.That(first.IsSelecting(grab), Is.True);
        Assert.That(acquisitions, Is.EqualTo(1));

        NearFarInteractor second = ActivateInteractor("RightHandInteractor", pawn.transform.position + Vector3.up);
        yield return PhysicsSteps(2);
        second.selectInput.QueueManualState(true, 1f);
        yield return PhysicsSteps(3);
        PositionNearInteractor(second, grab.colliders[0].bounds.center);
        yield return PhysicsSteps(8);
        Assert.That(second.interactablesHovered, Does.Contain(grab));
        Assert.That(first.IsSelecting(grab), Is.True, "A second pinch must not steal the first hand's held piece.");
        Assert.That(second.IsSelecting(grab), Is.False);
        Assert.That(acquisitions, Is.EqualTo(1), "Both held pinches must not alternate ownership across frames.");
        Assert.That(game.SelectedPiece, Is.SameAs(pawn));

        // The second hand keeps its original pinch; only the owner opens its fingers.
        first.selectInput.QueueManualState(false, 0f);
        yield return PhysicsSteps(4);
        Assert.That(first.IsSelecting(grab), Is.False);
        Assert.That(second.IsSelecting(grab), Is.True, "The waiting pinch can acquire the now released piece.");
        Assert.That(acquisitions, Is.EqualTo(2));
        Assert.That(game.SelectedPiece, Is.SameAs(pawn));
        Assert.That(game.MoveHistory.Count, Is.Zero);

        second.selectInput.QueueManualState(false, 0f);
        yield return PhysicsSteps(3);
        Assert.That(grab.isSelected, Is.False);
        Assert.That(game.SelectedPiece, Is.Null);
    }

    [UnityTest]
    public IEnumerator ApproachingAPieceWithoutPinchingOnlyHoversIt()
    {
        foreach (string hand in new[] { "LeftHandInteractor", "RightHandInteractor" })
        {
            PieceView pawn = Pawn();
            XRGrabInteractable grab = pawn.GetComponent<XRGrabInteractable>();
            NearFarInteractor near = ActivateInteractor(hand, grab.colliders[0].bounds.center);
            near.selectInput.QueueManualState(false, 0f);
            yield return PhysicsSteps(4);

            Assert.That(near.interactablesHovered, Does.Contain(grab));
            Assert.That(grab.isSelected, Is.False, "Near contact with an open hand cannot start a chess selection.");
            Assert.That(game.SelectedPiece, Is.Null);
            Assert.That(game.MoveHistory.Count, Is.Zero);
            near.transform.parent.gameObject.SetActive(false);
        }
    }

    [UnityTest]
    public IEnumerator ControllersStillRequireAFreshPressAfterApproachingAPiece()
    {
        foreach (string controller in new[] { "Left Controller", "Right Controller" })
        {
            PieceView pawn = Pawn();
            XRGrabInteractable grab = pawn.GetComponent<XRGrabInteractable>();
            NearFarInteractor near = ActivateInteractor(controller, pawn.transform.position + Vector3.up);
            yield return PhysicsSteps(2);
            near.selectInput.QueueManualState(true, 1f);
            yield return PhysicsSteps(3);
            PositionNearInteractor(near, grab.colliders[0].bounds.center);
            yield return PhysicsSteps(3);

            Assert.That(near.interactablesHovered, Does.Contain(grab));
            Assert.That(grab.isSelected, Is.False, "The hands' acquisition tolerance must not change controller trigger behavior.");
            near.selectInput.QueueManualState(false, 0f);
            yield return PhysicsSteps(2);
            near.selectInput.QueueManualState(true, 1f);
            yield return PhysicsSteps(3);
            Assert.That(grab.isSelected, Is.True, "A fresh controller press at the piece must still grab it.");
            Assert.That(game.SelectedPiece, Is.SameAs(pawn));

            near.selectInput.QueueManualState(false, 0f);
            yield return PhysicsSteps(3);
            Assert.That(game.SelectedPiece, Is.Null);
            near.gameObject.SetActive(false);
        }
    }

    private PieceView Pawn() => board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e2")));

    private NearFarInteractor ActivateInteractor(string name, Vector3 position)
    {
        GameObject root = offset.Find(name).gameObject;
        // Replay only near selection; UI pointing and poke have independent test coverage.
        foreach (XRPokeInteractor poke in root.GetComponentsInChildren<XRPokeInteractor>(true))
            poke.enabled = false;
        NearFarInteractor near = root.GetComponentInChildren<NearFarInteractor>(true);
        near.enableFarCasting = false;
        near.enableUIInteraction = false;
        near.selectInput = new XRInputButtonReader("Replayed near pinch or trigger")
        {
            inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue,
        };
        PositionNearInteractor(near, position);
        root.SetActive(true);
        return near;
    }

    private static void PositionNearInteractor(NearFarInteractor near, Vector3 position)
    {
        // The controller follows its tracked root, while a hand follows Pinch Grab Pose.
        // Move that pose so native grab offsets and distance sorting stay consistent.
        Transform pose = near.interactionAttachController.transformToFollow;
        pose.position += position - near.nearInteractionCaster.castOrigin.position;
    }

    private static IEnumerator PhysicsSteps(int count)
    {
        Physics.SyncTransforms();
        for (int i = 0; i < count; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }
    }
}
#endif
