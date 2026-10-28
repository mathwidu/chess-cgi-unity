#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
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
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;

// Preserve the nearest target: a refused grab must not jump to a neighboring piece.
public sealed class HandNearEligibilityTests
{
    private XRSimulatedHMD headset;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        if (Object.FindFirstObjectByType<XRRig>() == null)
            new GameObject("Hand target eligibility replay bootstrap").AddComponent<XRRig>();
        yield return null;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after hand eligibility test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator PinchingACloserWrongTurnPawnDoesNotGrabAnEligibleNeighbor()
    {
        var game = Object.FindFirstObjectByType<ChessGameController>();
        var board = Object.FindFirstObjectByType<BoardView>();
        game.StartLocalGame();
        yield return null;
        Transform offset = XRRig.Origin.Find("Camera Offset");
        offset.GetComponent<XRInputModalityManager>().enabled = false;
        foreach (string name in new[] { "LeftHandInteractor", "RightHandInteractor", "Left Controller", "Right Controller" })
            offset.Find(name).gameObject.SetActive(false);

        GameObject hand = offset.Find("LeftHandInteractor").gameObject;
        foreach (TrackedPoseDriver driver in hand.GetComponentsInChildren<TrackedPoseDriver>(true))
            driver.enabled = false;
        foreach (XRPokeInteractor poke in hand.GetComponentsInChildren<XRPokeInteractor>(true))
            poke.enabled = false;
        NearFarInteractor near = hand.GetComponentInChildren<NearFarInteractor>(true);
        near.enableFarCasting = false;
        near.enableUIInteraction = false;
        near.selectInput = new XRInputButtonReader("Replayed eligibility pinch")
        {
            inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue,
        };

        PieceView white = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e2")));
        PieceView black = board.Pieces.First(piece => piece.Square.Equals(BoardSquare.FromAlgebraic("e7")));
        var whiteGrab = white.GetComponent<XRGrabInteractable>();
        var blackGrab = black.GetComponent<XRGrabInteractable>();
        Assert.That(game.CanGrabPiece(white), Is.True);
        Assert.That(game.CanGrabPiece(black), Is.False);

        // Isolate two real game pieces from the rest of the board while preserving their
        // colliders, domain state, turn filters and the rig's original near radius/sorting.
        Vector3 isolated = new Vector3(40f, 8f, 0f);
        black.transform.position = isolated;
        white.transform.position = isolated + Vector3.right * .04f;
        near.nearInteractionCaster.castOrigin.position = isolated + Vector3.up * .005f;
        hand.SetActive(true);
        yield return PhysicsSteps(3);

        var caster = (SphereInteractionCaster)near.nearInteractionCaster;
        var hits = new List<Collider>();
        Assert.That(caster.TryGetColliderTargets(near.interactionManager, hits), Is.True);
        Assert.That(hits, Does.Contain(whiteGrab.colliders[0]), "The white piece must really be inside the native near query.");
        Assert.That(hits, Does.Contain(blackGrab.colliders[0]), "The closer wrong-turn piece must share that query.");
        float whiteDistance = Vector3.Distance(near.GetAttachTransform(whiteGrab).position, white.transform.position);
        float blackDistance = Vector3.Distance(near.GetAttachTransform(blackGrab).position, black.transform.position);
        Assert.That(blackDistance, Is.LessThan(whiteDistance), "The ineligible pawn must be the closer sorting candidate.");
        Assert.That(near.interactablesHovered, Does.Contain(blackGrab), "Hover must preserve the closest intended target.");

        near.selectInput.QueueManualState(true, 1f);
        yield return PhysicsSteps(4);
        Assert.That(blackGrab.isSelected, Is.False, "The closest piece still cannot be grabbed outside its turn.");
        Assert.That(whiteGrab.isSelected, Is.False, "A refused grab must not attract a selectable neighbor into the hand.");
        Assert.That(near.hasSelection, Is.False);
        Assert.That(game.SelectedPiece, Is.Null);
        Assert.That(game.MoveHistory.Count, Is.Zero);
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
