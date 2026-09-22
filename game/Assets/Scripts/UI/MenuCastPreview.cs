using System.Collections.Generic;
using UnityEngine;

// Renders the project's own characters into the menu without adding gameplay pieces.
// The camera renders only while a side transition is changing the composition.
public sealed class MenuCastPreview : MonoBehaviour
{
    private const int PreviewLayer = 30;
    private readonly List<Material> materials = new List<Material>();
    private GameObject stage;
    private Transform cast;
    private Camera previewCamera;
    private RenderTexture texture;
    private float angle = -8f;
    private float targetAngle = -8f;
    private bool dirty = true;

    public void Configure(UnityEngine.UI.RawImage image, ChessSide side)
    {
        if (stage == null) BuildStage();
        image.texture = texture;
        SetSide(side);
    }

    public void SetSide(ChessSide side)
    {
        targetAngle = side == ChessSide.White ? -8f : 8f;
    }

    private void LateUpdate()
    {
        if (stage == null) return;
        bool moving = Mathf.Abs(angle - targetAngle) > 0.01f;
        if (!dirty && !moving) return;
        angle = moving ? Mathf.Lerp(angle, targetAngle, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime)) : targetAngle;
        cast.localRotation = Quaternion.Euler(0, angle, 0);
        previewCamera.Render();
        dirty = false;
    }

    private void OnEnable() { dirty = true; }

    private void BuildStage()
    {
        stage = new GameObject("MenuCastStage");
        stage.transform.position = new Vector3(1400, 1400, 1400);
        cast = new GameObject("Cast").transform;
        cast.SetParent(stage.transform, false);
        Material ivory = MakeMaterial(new Color32(238, 241, 223, 255));
        Material green = MakeMaterial(new Color32(27, 85, 49, 255));
        Material black = MakeMaterial(new Color32(30, 38, 32, 255));
        Material edge = MakeMaterial(new Color32(248, 213, 28, 255));
        Primitive(PrimitiveType.Cube, "StageEdge", new Vector3(0, -0.085f, 0.18f), new Vector3(3.56f, 0.1f, 2.36f), edge);
        for (int rank = 0; rank < 4; rank++)
        for (int file = 0; file < 6; file++)
            Primitive(PrimitiveType.Cube, "Square", new Vector3((file - 2.5f) * 0.58f, -0.02f, (rank - 1.5f) * 0.58f + 0.18f), new Vector3(0.58f, 0.05f, 0.58f), (rank + file) % 2 == 0 ? ivory : green);

        AddCharacter("CustomPieces/Knight_Gustavo", new Vector3(-1.03f, 0.045f, 0.24f), 1.50f, ivory);
        AddCharacter("CustomPieces/Queen_Marta", new Vector3(0.03f, 0.045f, 0.49f), 1.90f, ivory);
        AddCharacter("CustomPieces/Pawn_Mathwidu_Redhead_v2", new Vector3(0.94f, 0.045f, -0.43f), 1.65f, black);

        texture = new RenderTexture(1100, 840, 24, RenderTextureFormat.ARGB32)
        {
            name = "MenuCastTexture",
            antiAliasing = 4,
            useMipMap = false
        };
        texture.Create();
        var cameraObject = new GameObject("MenuCastCamera");
        cameraObject.transform.SetParent(stage.transform, false);
        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.enabled = false;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = Color.clear;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = 1.6f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 20;
        previewCamera.targetTexture = texture;
        previewCamera.transform.localPosition = new Vector3(0.15f, 2.35f, -5f);
        previewCamera.transform.LookAt(stage.transform.position + new Vector3(0, 0.82f, 0));
        AddLight("CastKey", new Vector3(-2, 3, -3), 3f, Color.white);
        AddLight("CastFill", new Vector3(2, 2.8f, -1), 1.6f, new Color(0.9f, 1, 0.92f));
    }

    private void AddCharacter(string path, Vector3 position, float height, Material baseMaterial)
    {
        Primitive(PrimitiveType.Cylinder, "CharacterBase", position + Vector3.up * 0.04f, new Vector3(0.78f, 0.075f, 0.78f), baseMaterial);
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null) return;
        GameObject character = Instantiate(prefab, cast);
        character.name = "Menu_" + prefab.name;
        character.transform.localPosition = position;
        character.transform.localRotation = Quaternion.Euler(0, 180, 0);
        foreach (Collider collider in character.GetComponentsInChildren<Collider>()) collider.enabled = false;
        foreach (Transform child in character.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = PreviewLayer;
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        Bounds bounds = BoundsOf(renderers);
        character.transform.localScale *= height / Mathf.Max(0.01f, bounds.size.y);
        bounds = BoundsOf(renderers);
        Vector3 foot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        character.transform.position += cast.TransformPoint(position + Vector3.up * 0.12f) - foot;
    }

    private static Bounds BoundsOf(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private Material MakeMaterial(Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.18f);
        materials.Add(material);
        return material;
    }

    private void Primitive(PrimitiveType kind, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(kind);
        part.name = name;
        part.layer = PreviewLayer;
        part.transform.SetParent(cast, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Collider>().enabled = false;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void AddLight(string name, Vector3 position, float intensity, Color color)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(stage.transform, false);
        lightObject.transform.localPosition = position;
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 9;
        light.intensity = intensity;
        light.color = color;
        light.cullingMask = 1 << PreviewLayer;
    }

    private void OnDestroy()
    {
        if (previewCamera != null) previewCamera.targetTexture = null;
        if (texture != null) { texture.Release(); Destroy(texture); }
        if (stage != null) Destroy(stage);
        foreach (Material material in materials) if (material != null) Destroy(material);
    }
}
