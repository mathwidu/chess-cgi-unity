using UnityEngine;
using UnityEngine.Rendering;

public sealed class PieceFactory : MonoBehaviour
{
    [SerializeField] private Material whiteMaterial;
    [SerializeField] private Material blackMaterial;
    [SerializeField] private GameObject pawnPrefab;
    [SerializeField] private GameObject rookPrefab;
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject bishopPrefab;
    [SerializeField] private GameObject queenPrefab;
    [SerializeField] private GameObject kingPrefab;
    [SerializeField] private GameObject whitePawnPrefab;
    [SerializeField] private GameObject blackPawnPrefab;
    [SerializeField] private GameObject whiteRookPrefab;
    [SerializeField] private GameObject blackRookPrefab;
    [SerializeField] private GameObject whiteKnightPrefab;
    [SerializeField] private GameObject blackKnightPrefab;
    [SerializeField] private GameObject whiteBishopPrefab;
    [SerializeField] private GameObject blackBishopPrefab;
    [SerializeField] private GameObject whiteQueenPrefab;
    [SerializeField] private GameObject blackQueenPrefab;
    [SerializeField] private GameObject whiteKingPrefab;
    [SerializeField] private GameObject blackKingPrefab;
    [SerializeField] private float customVisualHeight = 1.15f;
    [SerializeField] private float customVisualBaseOffset = 0.02f;

    public void Configure(Material white, Material black)
    {
        whiteMaterial = white;
        blackMaterial = black;
    }

    public void ConfigureCustomPrefab(ChessPieceKind kind, GameObject prefab)
    {
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                pawnPrefab = prefab;
                break;
            case ChessPieceKind.Rook:
                rookPrefab = prefab;
                break;
            case ChessPieceKind.Knight:
                knightPrefab = prefab;
                break;
            case ChessPieceKind.Bishop:
                bishopPrefab = prefab;
                break;
            case ChessPieceKind.Queen:
                queenPrefab = prefab;
                break;
            case ChessPieceKind.King:
                kingPrefab = prefab;
                break;
        }
    }

    public void ConfigureCustomPrefab(ChessPieceKind kind, ChessSide side, GameObject prefab)
    {
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                if (side == ChessSide.White)
                {
                    whitePawnPrefab = prefab;
                }
                else
                {
                    blackPawnPrefab = prefab;
                }

                break;
            case ChessPieceKind.Rook:
                if (side == ChessSide.White)
                {
                    whiteRookPrefab = prefab;
                }
                else
                {
                    blackRookPrefab = prefab;
                }

                break;
            case ChessPieceKind.Knight:
                if (side == ChessSide.White)
                {
                    whiteKnightPrefab = prefab;
                }
                else
                {
                    blackKnightPrefab = prefab;
                }

                break;
            case ChessPieceKind.Bishop:
                if (side == ChessSide.White)
                {
                    whiteBishopPrefab = prefab;
                }
                else
                {
                    blackBishopPrefab = prefab;
                }

                break;
            case ChessPieceKind.Queen:
                if (side == ChessSide.White)
                {
                    whiteQueenPrefab = prefab;
                }
                else
                {
                    blackQueenPrefab = prefab;
                }

                break;
            case ChessPieceKind.King:
                if (side == ChessSide.White)
                {
                    whiteKingPrefab = prefab;
                }
                else
                {
                    blackKingPrefab = prefab;
                }

                break;
        }
    }

    public PieceView CreatePiece(VisualPieceState state, Vector3 position, Transform parent)
    {
        GameObject root = new GameObject($"{state.Side} {state.Kind}");
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.transform.rotation = GetIdleRotation(state.Side);

        PieceView pieceView = root.AddComponent<PieceView>();
        AddCollider(root);
        Material sideMaterial = state.Side == ChessSide.White ? whiteMaterial : blackMaterial;
        bool hasCustomShape = BuildCustomShape(root.transform, state.Kind, state.Side, sideMaterial);
        if (!hasCustomShape)
        {
            BuildPrimitiveShape(root.transform, state.Kind, sideMaterial);
        }

        pieceView.SetVisualRoot(hasCustomShape ? root.transform.Find("CustomVisual") : root.transform);
        ConfigureRenderers(root.transform);
        pieceView.Initialize(state);
        return pieceView;
    }

    private static void AddCollider(GameObject root)
    {
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.height = 1.4f;
        collider.radius = 0.35f;
        collider.center = new Vector3(0f, 0.7f, 0f);
    }

    private bool BuildCustomShape(Transform parent, ChessPieceKind kind, ChessSide side, Material sideMaterial)
    {
        GameObject prefab = GetCustomPrefab(kind, side, out bool isSideSpecific);
        if (prefab == null)
        {
            return false;
        }

        GameObject visual = Object.Instantiate(prefab, parent);
        visual.name = "CustomVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        FitCustomVisual(visual.transform, GetCustomVisualHeight(kind));
        ConfigureCustomVisualExtensions(visual, kind);
        if (isSideSpecific)
        {
            TeamOutfitApplier.ApplyTo(visual.transform, side);
        }
        else
        {
            TeamOutfitApplier.ApplyToOrCreateAccent(visual.transform, side);
        }

        return true;
    }

    private static void ConfigureCustomVisualExtensions(GameObject visual, ChessPieceKind kind)
    {
        Animator animator = visual.GetComponentInChildren<Animator>();

        CharacterAnimationDriver driver = visual.GetComponent<CharacterAnimationDriver>();
        if (driver == null)
        {
            driver = visual.AddComponent<CharacterAnimationDriver>();
        }

        driver.Configure(animator);

        ModularCharacterRig modularRig = visual.GetComponent<ModularCharacterRig>();
        if (modularRig == null)
        {
            modularRig = visual.AddComponent<ModularCharacterRig>();
        }

        modularRig.AutoBind();

        CharacterVisualContract contract = visual.GetComponent<CharacterVisualContract>();
        if (contract == null)
        {
            contract = visual.AddComponent<CharacterVisualContract>();
        }

        contract.Configure(kind, animator != null ? CharacterRigStatus.RigCandidate : CharacterRigStatus.StaticMesh, animator);
    }

    private static Quaternion GetIdleRotation(ChessSide side)
    {
        return side == ChessSide.Black ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
    }

    private GameObject GetCustomPrefab(ChessPieceKind kind, ChessSide side, out bool isSideSpecific)
    {
        GameObject sidePrefab = GetSideSpecificPrefab(kind, side);
        if (sidePrefab != null)
        {
            isSideSpecific = true;
            return sidePrefab;
        }

        isSideSpecific = false;
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                return pawnPrefab;
            case ChessPieceKind.Rook:
                return rookPrefab;
            case ChessPieceKind.Knight:
                return knightPrefab;
            case ChessPieceKind.Bishop:
                return bishopPrefab;
            case ChessPieceKind.Queen:
                return queenPrefab;
            case ChessPieceKind.King:
                return kingPrefab;
            default:
                return null;
        }
    }

    private GameObject GetSideSpecificPrefab(ChessPieceKind kind, ChessSide side)
    {
        bool isWhite = side == ChessSide.White;
        switch (kind)
        {
            case ChessPieceKind.Pawn:
                return isWhite ? whitePawnPrefab : blackPawnPrefab;
            case ChessPieceKind.Rook:
                return isWhite ? whiteRookPrefab : blackRookPrefab;
            case ChessPieceKind.Knight:
                return isWhite ? whiteKnightPrefab : blackKnightPrefab;
            case ChessPieceKind.Bishop:
                return isWhite ? whiteBishopPrefab : blackBishopPrefab;
            case ChessPieceKind.Queen:
                return isWhite ? whiteQueenPrefab : blackQueenPrefab;
            case ChessPieceKind.King:
                return isWhite ? whiteKingPrefab : blackKingPrefab;
            default:
                return null;
        }
    }

    private float GetCustomVisualHeight(ChessPieceKind kind)
    {
        switch (kind)
        {
            case ChessPieceKind.Rook:
            case ChessPieceKind.Knight:
            case ChessPieceKind.Bishop:
                return customVisualHeight + 0.16f;
            case ChessPieceKind.Queen:
            case ChessPieceKind.King:
                return customVisualHeight + 0.28f;
            default:
                return customVisualHeight;
        }
    }

    private void FitCustomVisual(Transform visual, float targetHeight)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            visual.localPosition = new Vector3(0f, customVisualBaseOffset, 0f);
            return;
        }

        Bounds bounds = CalculateBounds(renderers);
        if (bounds.size.y > 0.001f)
        {
            float scale = targetHeight / bounds.size.y;
            visual.localScale *= scale;
        }

        bounds = CalculateBounds(renderers);
        visual.position += new Vector3(0f, customVisualBaseOffset - bounds.min.y, 0f);
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private static void ConfigureRenderers(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static void BuildPrimitiveShape(Transform parent, ChessPieceKind kind, Material material)
    {
        AddCylinder(parent, "Base", new Vector3(0f, 0.08f, 0f), new Vector3(0.7f, 0.16f, 0.7f), material);
        AddCylinder(parent, "Stem", new Vector3(0f, 0.45f, 0f), new Vector3(0.36f, 0.7f, 0.36f), material);

        switch (kind)
        {
            case ChessPieceKind.Pawn:
                AddSphere(parent, "Head", new Vector3(0f, 0.95f, 0f), new Vector3(0.42f, 0.42f, 0.42f), material);
                break;
            case ChessPieceKind.Rook:
                AddCube(parent, "Crown", new Vector3(0f, 1f, 0f), new Vector3(0.58f, 0.22f, 0.58f), material);
                break;
            case ChessPieceKind.Knight:
                AddCube(parent, "HorseHead", new Vector3(0.1f, 1f, 0f), new Vector3(0.45f, 0.55f, 0.32f), material);
                break;
            case ChessPieceKind.Bishop:
                AddSphere(parent, "Mitre", new Vector3(0f, 1f, 0f), new Vector3(0.48f, 0.62f, 0.48f), material);
                break;
            case ChessPieceKind.Queen:
                AddSphere(parent, "Crown", new Vector3(0f, 1.02f, 0f), new Vector3(0.58f, 0.42f, 0.58f), material);
                AddSphere(parent, "Top", new Vector3(0f, 1.38f, 0f), new Vector3(0.22f, 0.22f, 0.22f), material);
                break;
            case ChessPieceKind.King:
                AddSphere(parent, "Crown", new Vector3(0f, 1.02f, 0f), new Vector3(0.52f, 0.42f, 0.52f), material);
                AddCube(parent, "CrossVertical", new Vector3(0f, 1.42f, 0f), new Vector3(0.12f, 0.4f, 0.12f), material);
                AddCube(parent, "CrossHorizontal", new Vector3(0f, 1.48f, 0f), new Vector3(0.36f, 0.1f, 0.1f), material);
                break;
        }
    }

    private static void AddCylinder(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ConfigurePart(part, parent, name, localPosition, localScale, material);
    }

    private static void AddSphere(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ConfigurePart(part, parent, name, localPosition, localScale, material);
    }

    private static void AddCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ConfigurePart(part, parent, name, localPosition, localScale, material);
    }

    private static void ConfigurePart(GameObject part, Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
    {
        Collider collider = part.GetComponent<Collider>();
        if (Application.isPlaying)
        {
            Object.Destroy(collider);
        }
        else
        {
            Object.DestroyImmediate(collider);
        }
        part.name = name;
        part.transform.SetParent(parent);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;

        if (material != null)
        {
            part.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
