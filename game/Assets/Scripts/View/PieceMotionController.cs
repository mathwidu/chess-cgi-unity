using System.Collections;
using UnityEngine;

public sealed class PieceMotionController : MonoBehaviour
{
    [SerializeField] private float walkDuration = 0.55f;
    [SerializeField] private float stepHeight = 0.08f;
    [SerializeField] private float leanAngle = 4.5f;
    [SerializeField] private float captureDuration = 0.45f;

    public PieceMotionSettings Settings => new PieceMotionSettings(walkDuration, stepHeight, leanAngle, captureDuration);

    public IEnumerator MovePiece(PieceView piece, Vector3 target)
    {
        if (piece == null)
        {
            yield break;
        }

        yield return piece.MoveWithWalk(target, Settings);
    }

    public void MoveInstant(PieceView piece, Vector3 target)
    {
        if (piece != null)
        {
            piece.transform.position = target;
        }
    }
}
