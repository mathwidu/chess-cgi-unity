#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class ThrownPieceTests
{
    private static readonly Vector3 GroundCenter = new Vector3(30f, -20f, 30f);
    private const float GroundTop = -19.5f;

    private XRSimulatedHMD headset;
    private BoardView board;
    private ChessGameController game;
    private XRInteractionManager manager;
    private XRDirectInteractor hand;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        headset = InputSystem.AddDevice<XRSimulatedHMD>();
        EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Main.unity", new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        yield return null;
        board = Object.FindFirstObjectByType<BoardView>();
        game = Object.FindFirstObjectByType<ChessGameController>();
        manager = Object.FindFirstObjectByType<XRInteractionManager>();
        game.StartLocalGame();
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.transform.position = GroundCenter;
        ground.transform.localScale = new Vector3(10f, 1f, 10f);
        hand = new GameObject("Thrown piece test hand").AddComponent<XRDirectInteractor>();
        hand.transform.position = new Vector3(GroundCenter.x, 0f, GroundCenter.z);
        hand.interactionManager = manager;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (headset != null && headset.added) InputSystem.RemoveDevice(headset);
        Scene previous = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(SceneManager.CreateScene("Empty after thrown piece test"));
        yield return SceneManager.UnloadSceneAsync(previous);
    }

    [UnityTest]
    public IEnumerator AGrabbedPieceCarriesItsWeightLowLikeAWeightedBase()
    {
        PieceView pawn = Pawn();
        Rigidbody body = pawn.GetComponent<Rigidbody>();
        float height = pawn.GetComponent<Collider>().bounds.size.y;
        yield return LetGoAbove(pawn, GroundTop + 0.5f, Quaternion.identity);

        float lift = Vector3.Dot(body.worldCenterOfMass - pawn.transform.position, pawn.transform.up);
        Assert.That(lift / height, Is.InRange(0.1f, 0.3f), "The centre of mass should sit low, like a weighted base.");
    }

    [UnityTest]
    public IEnumerator ADroppedPieceBouncesOffTheFloor()
    {
        PieceView pawn = Pawn();
        Rigidbody body = pawn.GetComponent<Rigidbody>();
        yield return LetGoAbove(pawn, GroundTop + 0.5f, Quaternion.Euler(30f, 0f, 0f));

        bool landed = false;
        float highestRebound = 0f;
        for (float t = 0f; t < 1.5f; t += Time.fixedDeltaTime)
        {
            yield return new WaitForFixedUpdate();
            landed |= body.linearVelocity.y > -0.05f && pawn.transform.position.y < GroundTop + 0.2f;
            if (landed)
            {
                highestRebound = Mathf.Max(highestRebound, body.linearVelocity.y);
            }
        }

        Assert.That(landed, Is.True, $"The piece should reach the floor, ended at {pawn.transform.position}.");
        Assert.That(highestRebound, Is.GreaterThan(0.2f), "A piece hitting the floor at about 3 m/s should bounce.");
        Assert.That(pawn.transform.position.y, Is.GreaterThan(GroundTop - 0.05f), "The piece must not fall through the floor.");
    }

    [UnityTest]
    public IEnumerator APieceLyingOnItsSideRollsWhenPushed()
    {
        PieceView pawn = Pawn();
        Rigidbody body = pawn.GetComponent<Rigidbody>();
        yield return LetGoAbove(pawn, GroundTop + 0.03f, Quaternion.Euler(0f, 0f, 90f));
        yield return new WaitForSeconds(0.6f);

        Quaternion lying = pawn.transform.rotation;
        Assert.That(Vector3.Angle(pawn.transform.up, Vector3.up), Is.GreaterThan(60f), "The pawn should still lie on its side before the push.");
        body.linearVelocity = Vector3.forward * 0.6f;
        float turned = 0f;
        Quaternion previous = lying;
        for (float t = 0f; t < 0.8f; t += Time.fixedDeltaTime)
        {
            yield return new WaitForFixedUpdate();
            turned += Quaternion.Angle(previous, pawn.transform.rotation);
            previous = pawn.transform.rotation;
        }

        Assert.That(turned, Is.GreaterThan(180f), $"A pushed piece on its side should roll, it turned {turned:F0} degrees.");
    }

    [UnityTest]
    public IEnumerator AThrownPieceCannotPressTheTableButtons()
    {
        PhysicalTableButton[] buttons = Object.FindObjectsByType<PhysicalTableButton>(FindObjectsSortMode.None);
        TableView table = Object.FindFirstObjectByType<TableView>();
        PhysicalTableButton raise = buttons.First(b => b.FaceGraphic.PointsUp);
        int step = table.HeightStep;
        PieceView pawn = Pawn();
        yield return LetGoAbove(pawn, 0f, Quaternion.identity);
        pawn.transform.position = raise.RestFaceCenter - raise.PressDirection * 0.05f;
        pawn.GetComponent<Rigidbody>().linearVelocity = raise.PressDirection * 2f;

        float deepest = 0f;
        for (float t = 0f; t < 0.5f; t += Time.fixedDeltaTime)
        {
            yield return new WaitForFixedUpdate();
            deepest = Mathf.Max(deepest, raise.PressFraction);
        }

        Assert.That(Physics.GetIgnoreLayerCollision(PieceView.PhysicsLayer, PhysicalTableButton.PhysicsLayer), Is.True);
        Assert.That(Physics.GetIgnoreLayerCollision(PieceView.PhysicsLayer, BoardScaleHandles.PhysicsLayer), Is.True);
        Assert.That(deepest, Is.LessThan(0.05f), "A thrown piece must pass the table button without pressing it.");
        Assert.That(table.HeightStep, Is.EqualTo(step));
    }

    private PieceView Pawn() => board.Pieces.First(p => p.Square.Equals(BoardSquare.FromAlgebraic("e2")));

    private IEnumerator LetGoAbove(PieceView piece, float height, Quaternion rotation)
    {
        var grab = piece.GetComponent<ThrowablePiece>();
        manager.SelectEnter((IXRSelectInteractor)hand, grab);
        yield return null;
        yield return new WaitForFixedUpdate();
        manager.SelectExit((IXRSelectInteractor)hand, grab);
        yield return null;
        yield return new WaitForFixedUpdate();
        Rigidbody body = piece.GetComponent<Rigidbody>();
        body.position = new Vector3(GroundCenter.x, height, GroundCenter.z);
        body.rotation = rotation;
        piece.transform.SetPositionAndRotation(body.position, rotation);
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        Assert.That(body.isKinematic, Is.False, "Letting go off the board should leave the piece to physics. ");
        Assert.That(game.MoveHistory.Count, Is.Zero);
    }
}
#endif
