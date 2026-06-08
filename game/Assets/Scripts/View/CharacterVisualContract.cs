using UnityEngine;

public enum CharacterRigStatus
{
    StaticMesh,
    RigCandidate,
    RiggedHumanoid,
    RiggedProp
}

public sealed class CharacterVisualContract : MonoBehaviour
{
    [SerializeField] private ChessPieceKind pieceKind;
    [SerializeField] private CharacterRigStatus rigStatus = CharacterRigStatus.StaticMesh;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform effectsSocket;
    [SerializeField] private Transform hitSocket;
    [SerializeField] private Transform groundSocket;
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private Transform rightHandSocket;
    [SerializeField] private Transform leftHandSocket;
    [SerializeField] private Transform castSocket;

    public ChessPieceKind PieceKind => pieceKind;

    public CharacterRigStatus RigStatus => rigStatus;

    public Animator Animator => animator;

    public Transform EffectsSocket => effectsSocket;

    public Transform HitSocket => hitSocket;

    public Transform GroundSocket => groundSocket;

    public Transform WeaponSocket => weaponSocket;

    public Transform RightHandSocket => rightHandSocket;

    public Transform LeftHandSocket => leftHandSocket;

    public Transform CastSocket => castSocket;

    public bool HasAnimator => animator != null;

    public bool IsRigged => rigStatus == CharacterRigStatus.RiggedHumanoid || rigStatus == CharacterRigStatus.RiggedProp;

    public void Configure(ChessPieceKind kind, CharacterRigStatus status, Animator targetAnimator)
    {
        pieceKind = kind;
        rigStatus = status;
        animator = targetAnimator != null ? targetAnimator : GetComponentInChildren<Animator>();
        EnsureRequiredSockets();
    }

    public void EnsureRequiredSockets()
    {
        effectsSocket = EnsureSocket(effectsSocket, "EffectsSocket", Vector3.up);
        hitSocket = EnsureSocket(hitSocket, "HitSocket", Vector3.up * 0.75f);
        groundSocket = EnsureSocket(groundSocket, "GroundSocket", Vector3.zero);
        weaponSocket = EnsureSocket(weaponSocket, "WeaponSocket", new Vector3(0.28f, 0.72f, 0.12f));
        rightHandSocket = EnsureSocket(rightHandSocket, "RightHandSocket", new Vector3(0.34f, 0.62f, 0.04f));
        leftHandSocket = EnsureSocket(leftHandSocket, "LeftHandSocket", new Vector3(-0.34f, 0.62f, 0.04f));
        castSocket = EnsureSocket(castSocket, "CastSocket", new Vector3(0f, 0.92f, 0.22f));
    }

    private Transform EnsureSocket(Transform current, string socketName, Vector3 localPosition)
    {
        if (current != null)
        {
            return current;
        }

        Transform existing = transform.Find(socketName);
        if (existing != null)
        {
            existing.localPosition = localPosition;
            return existing;
        }

        GameObject socket = new GameObject(socketName);
        socket.transform.SetParent(transform, false);
        socket.transform.localPosition = localPosition;
        return socket.transform;
    }
}
