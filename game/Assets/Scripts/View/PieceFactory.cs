using UnityEngine;

public sealed class PieceFactory : MonoBehaviour
{
    [SerializeField] private Material whiteMaterial;
    [SerializeField] private Material blackMaterial;

    public void Configure(Material white, Material black)
    {
        whiteMaterial = white;
        blackMaterial = black;
    }

    public PieceView CreatePiece(VisualPieceState state, Vector3 position, Transform parent)
    {
        GameObject root = new GameObject($"{state.Side} {state.Kind}");
        root.transform.SetParent(parent);
        root.transform.position = position;

        PieceView pieceView = root.AddComponent<PieceView>();
        AddCollider(root);
        BuildPrimitiveShape(root.transform, state.Kind, state.Side == ChessSide.White ? whiteMaterial : blackMaterial);
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
