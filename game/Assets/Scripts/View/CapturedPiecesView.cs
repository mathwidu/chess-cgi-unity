using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Captured pieces stand beside the board, on the seated player's right (the height panel is
// on the left). The near half holds what that player took, the far half what the opponent
// took, and a "+N" marks the side ahead in material. Built in board-local units, so it rides
// with the board on the table; a new capture flies from its square to its place once.
public sealed class CapturedPiecesView : MonoBehaviour
{
    private const float DisplayScale = 0.7f;
    private const float TrayX = 7.6f;
    private const float TrayWidth = 2.9f;
    private const float TrayLength = 4.5f;
    private const float TrayGap = 0.4f;
    private const float SlotInset = 0.5f;
    private const float SlotSpacing = 0.95f;
    // 3 × 5 slots: a side can take at most 15 pieces (never the king).
    private const int Columns = 3;
    // The table top sits just under the board base: 0.774 m against the board's 0.78 m, in board units.
    private const float TableTop = -0.133f;
    private const float MatHeight = 0.03f;
    private const float FlySeconds = 0.45f;
    private const float FlyArc = 1.2f;

    private static readonly Color AccentColor = new Color32(255, 221, 0, 255);

    private readonly List<GameObject> shownPieces = new List<GameObject>();
    private ChessGameController game;
    private PieceFactory factory;
    private BoardView board;
    private CameraController cameraController;
    private Transform piecesRoot;
    private RectTransform balanceLabel;
    private Text balanceText;
    private int shownCount = -1;
    private int shownBalance;
    private bool shownPrimitive;
    private bool viewerOnBlackSide;

    public int DisplayedCount => shownPieces.Count;
    public int NearCount { get; private set; }
    public int FarCount { get; private set; }
    public string BalanceText => balanceLabel != null && balanceLabel.gameObject.activeSelf ? balanceText.text : string.Empty;

    public void Build(BoardView boardView)
    {
        board = boardView;
        Material felt = ScenePolish.CreateMaterial("Runtime_Capture_Felt", new Color(0.03f, 0.1f, 0.065f), 0f, 0.08f);
        float matZ = TrayGap * 0.5f + TrayLength * 0.5f;
        ScenePolish.CreateCube(transform, "NearMat", new Vector3(TrayX, TableTop + MatHeight * 0.5f, -matZ),
            new Vector3(TrayWidth, MatHeight, TrayLength), felt, false);
        ScenePolish.CreateCube(transform, "FarMat", new Vector3(TrayX, TableTop + MatHeight * 0.5f, matZ),
            new Vector3(TrayWidth, MatHeight, TrayLength), felt, false);
        piecesRoot = new GameObject("Pieces").transform;
        piecesRoot.SetParent(transform, false);
        BuildBalanceLabel();
    }

    private void BuildBalanceLabel()
    {
        var canvasObject = new GameObject("MaterialBalance", typeof(RectTransform));
        balanceLabel = (RectTransform)canvasObject.transform;
        balanceLabel.SetParent(transform, false);
        balanceLabel.sizeDelta = new Vector2(200f, 90f);
        balanceLabel.localScale = Vector3.one * 0.005f;
        // Lying on the table and read from the seat; no raycaster, so the VR ray passes it by.
        balanceLabel.localRotation = Quaternion.Euler(90f, 0f, 0f);
        canvasObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        var textRect = new GameObject("Text", typeof(RectTransform)).GetComponent<RectTransform>();
        textRect.SetParent(balanceLabel, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        balanceText = textRect.gameObject.AddComponent<Text>();
        Font font = Resources.Load<Font>("UI/Lato-Bold");
        balanceText.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        balanceText.fontSize = 64;
        balanceText.color = AccentColor;
        balanceText.alignment = TextAnchor.MiddleCenter;
        balanceText.raycastTarget = false;
        balanceLabel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (piecesRoot == null)
        {
            return;
        }

        if (game == null || factory == null)
        {
            game = Object.FindFirstObjectByType<ChessGameController>();
            factory = Object.FindFirstObjectByType<PieceFactory>();
            cameraController = Object.FindFirstObjectByType<CameraController>();
            if (game == null || factory == null)
            {
                return;
            }
        }

        bool onBlackSide = XRRig.IsHeadsetPresent
            ? XRRig.SeatedAsBlack
            : cameraController != null && cameraController.CurrentPerspective == ChessSide.Black;
        if (onBlackSide != viewerOnBlackSide)
        {
            // Mirror the whole display to the other player's right; the pieces keep their halves.
            viewerOnBlackSide = onBlackSide;
            transform.localRotation = Quaternion.Euler(0f, onBlackSide ? 180f : 0f, 0f);
            Rebuild(false);
        }

        // Wait for the capturing move to land, so the taken piece leaves the board first.
        bool changed = game.CapturedPieces.Count != shownCount || game.MaterialBalance != shownBalance ||
            factory.UsePrimitivePieces != shownPrimitive;
        if (changed && !game.IsAnimatingMove)
        {
            Rebuild(game.CapturedPieces.Count == shownCount + 1);
        }
    }

    private void Rebuild(bool flyNewest)
    {
        StopAllCoroutines();
        foreach (GameObject piece in shownPieces)
        {
            Destroy(piece);
        }

        shownPieces.Clear();
        shownCount = game.CapturedPieces.Count;
        shownPrimitive = factory.UsePrimitivePieces;
        shownBalance = game.MaterialBalance;
        ChessSide viewerSide = viewerOnBlackSide ? ChessSide.Black : ChessSide.White;

        // Each half lists the opponent's pieces its side took, most valuable first.
        var near = new List<int>();
        var far = new List<int>();
        for (int i = 0; i < game.CapturedPieces.Count; i++)
        {
            (game.CapturedPieces[i].Side == viewerSide ? far : near).Add(i);
        }

        near.Sort(ByValueThenOrder);
        far.Sort(ByValueThenOrder);
        NearCount = near.Count;
        FarCount = far.Count;
        int newest = flyNewest ? game.CapturedPieces.Count - 1 : -1;
        PlaceHalf(near, -1f, newest);
        PlaceHalf(far, 1f, newest);
        RefreshBalance(viewerSide);
    }

    private int ByValueThenOrder(int a, int b)
    {
        int byValue = ChessPieceValue.Of(game.CapturedPieces[b].Kind).CompareTo(ChessPieceValue.Of(game.CapturedPieces[a].Kind));
        return byValue != 0 ? byValue : a.CompareTo(b);
    }

    private void PlaceHalf(List<int> captures, float direction, int newest)
    {
        // Fill from the table edge toward the middle of the board, inner column first.
        float firstRow = TrayGap * 0.5f + TrayLength - SlotInset;
        for (int slot = 0; slot < captures.Count; slot++)
        {
            VisualPieceState captured = game.CapturedPieces[captures[slot]];
            GameObject piece = factory.CreateDisplayPiece(captured.Side, captured.Kind, piecesRoot);
            var place = new Vector3(TrayX - TrayWidth * 0.5f + SlotInset + slot % Columns * SlotSpacing,
                TableTop + MatHeight, direction * (firstRow - slot / Columns * SlotSpacing));
            piece.transform.localPosition = place;
            piece.transform.localScale = Vector3.one * DisplayScale;
            shownPieces.Add(piece);
            if (captures[slot] == newest)
            {
                Vector3 square = piecesRoot.InverseTransformPoint(board.GetPieceWorldPosition(captured.Square));
                StartCoroutine(Fly(piece.transform, square, place));
            }
        }
    }

    private IEnumerator Fly(Transform piece, Vector3 from, Vector3 to)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / FlySeconds)
        {
            float eased = Mathf.SmoothStep(0f, 1f, t);
            piece.localPosition = Vector3.Lerp(from, to, eased) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * FlyArc);
            piece.localScale = Vector3.one * Mathf.Lerp(1f, DisplayScale, eased);
            yield return null;
        }

        piece.localPosition = to;
        piece.localScale = Vector3.one * DisplayScale;
    }

    private void RefreshBalance(ChessSide viewerSide)
    {
        int balance = game.MaterialBalance;
        int viewerAdvantage = viewerSide == ChessSide.White ? balance : -balance;
        balanceLabel.gameObject.SetActive(viewerAdvantage != 0);
        if (viewerAdvantage == 0)
        {
            return;
        }

        // Beside the half of the side ahead, at the corner of the board nearest that half.
        float direction = viewerAdvantage > 0 ? -1f : 1f;
        balanceLabel.localPosition = new Vector3(TrayX, TableTop + MatHeight + 0.01f,
            direction * (TrayGap * 0.5f + TrayLength + 0.45f));
        balanceText.text = "+" + Mathf.Abs(viewerAdvantage);
    }
}
