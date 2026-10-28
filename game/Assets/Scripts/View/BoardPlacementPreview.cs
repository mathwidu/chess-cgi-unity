using UnityEngine;
using UnityEngine.Rendering;

// The frame follows the piece's base projection, using the same board lookup
// as a drop. It never attracts the piece or chooses a square from the hand.
public sealed class BoardPlacementPreview : MonoBehaviour
{
    private BoardView board;
    private ChessGameController game;
    private LineRenderer frame;
    private Material material;
    public BoardSquare? Target { get; private set; }
    public bool IsLegal { get; private set; }

    private void Awake()
    {
        board = GetComponent<BoardView>();
        game = FindFirstObjectByType<ChessGameController>();
        var marker = new GameObject("Placement target");
        marker.transform.SetParent(transform, false);
        frame = marker.AddComponent<LineRenderer>();
        frame.useWorldSpace = false;
        frame.loop = true;
        frame.positionCount = 4;
        frame.shadowCastingMode = ShadowCastingMode.Off;
        frame.receiveShadows = false;
        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        frame.sharedMaterial = material;
        Hide();
    }

    private void LateUpdate()
    {
        PieceView piece = game != null ? game.SelectedPiece : null;
        if (!XRRig.IsHeadsetPresent || board == null || piece == null || game.IsInputBlocked ||
            !piece.TryGetComponent(out ThrowablePiece grab) || !grab.isSelected ||
            !board.TryGetSquareAt(piece.transform.position, out BoardSquare target))
        {
            Hide();
            return;
        }

        Target = target;
        IsLegal = game.CanStartMoveWithDrop(piece, piece.transform.position);
        Color color = IsLegal ? new Color(.15f, 1f, .4f) :
            target.Equals(piece.Square) ? new Color(1f, .75f, .1f) : new Color(1f, .18f, .12f);
        material.color = color;
        frame.widthMultiplier = .035f * transform.lossyScale.x;
        Vector3 center = transform.InverseTransformPoint(board.GetPieceWorldPosition(target));
        center.y += .035f;
        float half = board.SquareSize * .44f;
        frame.SetPosition(0, center + new Vector3(-half, 0f, -half));
        frame.SetPosition(1, center + new Vector3(-half, 0f, half));
        frame.SetPosition(2, center + new Vector3(half, 0f, half));
        frame.SetPosition(3, center + new Vector3(half, 0f, -half));
        frame.enabled = true;
    }

    private void Hide()
    {
        Target = null;
        IsLegal = false;
        if (frame != null) frame.enabled = false;
    }

    private void OnDisable() => Hide();
    private void OnDestroy()
    {
        if (frame != null) Destroy(frame.gameObject);
        if (material != null) Destroy(material);
    }
}
