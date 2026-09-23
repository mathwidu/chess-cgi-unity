using System.Collections.Generic;
using UnityEngine;

// The two existing professor models are display clones, never gameplay pieces.
// Render only during a selection transition; keep studio lighting out of the match.
[DefaultExecutionOrder(-10)]
public sealed class MenuCastPreview : MonoBehaviour
{
    private const int PreviewLayer = 30;
    private const float SelectionTransitionSpeed = 9f;

    private sealed class Figure
    {
        public Transform Root;
        public readonly List<Surface> Surfaces = new List<Surface>();
    }
    private sealed class Surface
    {
        public Material Material;
        public string ColorProperty;
        public Color Color;
    }

    private readonly List<Material> materials = new List<Material>();
    private readonly List<Light> sceneLights = new List<Light>();
    private readonly List<bool> lightStates = new List<bool>();
    private GameObject stage;
    private Figure white;
    private Figure black;
    private Camera previewCamera;
    private RenderTexture texture;
    private float selection;
    private float targetSelection;
    private bool dirty = true;

    public void Configure(UnityEngine.UI.RawImage image, ChessSide side)
    {
        if (stage == null)
        {
            BuildStage();
        }
        image.texture = texture;
        SetSide(side);
        selection = targetSelection;
    }

    public void SetSide(ChessSide side, bool both = false)
    {
        targetSelection = both ? 0.5f : side == ChessSide.White ? 0f : 1f;
    }

    private void LateUpdate()
    {
        if (stage == null)
        {
            return;
        }
        bool moving = Mathf.Abs(selection - targetSelection) > 0.001f;
        if (!dirty && !moving)
        {
            return;
        }
        selection = moving ? Mathf.Lerp(selection, targetSelection, 1f - Mathf.Exp(-SelectionTransitionSpeed * Time.unscaledDeltaTime)) : targetSelection;
        Pose(white, new Vector3(-0.82f, 0, 0), 1f - selection, 174f);
        Pose(black, new Vector3(0.65f, 0, 0), selection, 186f);
        RenderStudio();
        dirty = false;
    }

    private void Pose(Figure figure, Vector3 position, float emphasis, float rotation)
    {
        float scale = Mathf.Lerp(0.68f, 1f, emphasis);
        position.z = Mathf.Lerp(0.72f, -0.08f, emphasis);
        position.y = Mathf.Lerp(0.24f, 0f, emphasis);
        figure.Root.localPosition = position;
        figure.Root.localScale = Vector3.one * scale;
        figure.Root.localRotation = Quaternion.Euler(0, rotation, 0);
        foreach (Surface surface in figure.Surfaces)
        {
            Color color = surface.Color * Mathf.Lerp(0.82f, 1f, emphasis);
            color.a = surface.Color.a;
            surface.Material.SetColor(surface.ColorProperty, color);
        }
    }

    public Vector2 BaseViewport(ChessSide side)
    {
        if (previewCamera == null)
        {
            return Vector2.one * 0.5f;
        }
        Transform figure = side == ChessSide.White ? white.Root : black.Root;
        return previewCamera.WorldToViewportPoint(figure.position);
    }

    private void OnEnable()
    {
        dirty = true;
        if (stage != null)
        {
            stage.SetActive(true);
            FindSceneLights();
        }
    }

    private void OnDisable()
    {
        if (stage != null)
        {
            stage.SetActive(false);
        }
    }

    private void BuildStage()
    {
        stage = new GameObject("MenuCastStage");
        stage.transform.position = new Vector3(1400, 1400, 1400);
        white = AddFigure("CustomPieces/Queen_Marta", "WhiteProfessor", new Color32(221, 227, 214, 255));
        black = AddFigure("CustomPieces/King_Ricardo_Carioca", "BlackProfessor", new Color32(24, 32, 27, 255));

        texture = new RenderTexture(1290, 1176, 24, RenderTextureFormat.ARGB32)
        {
            name = "MenuCastTexture", antiAliasing = 4, useMipMap = false
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
        previewCamera.orthographicSize = 1.29f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 20;
        previewCamera.targetTexture = texture;
        previewCamera.transform.localPosition = new Vector3(0, 2.3f, -6f);
        previewCamera.transform.LookAt(stage.transform.position + new Vector3(0, 1.15f, 0));
        AddLight("StudioKey", LightType.Directional, new Vector3(-2, 4, -3), 1.1f, new Color(1f, 0.96f, 0.86f));
        AddLight("StudioFill", LightType.Point, new Vector3(2, 2, -2), 3.2f, new Color(0.80f, 0.92f, 1f));
        AddLight("StudioRim", LightType.Point, new Vector3(0.5f, 3, 1), 4f, new Color(1f, 0.95f, 0.80f));
        FindSceneLights();
    }

    private Figure AddFigure(string path, string name, Color baseColor)
    {
        var figure = new Figure { Root = new GameObject(name).transform };
        figure.Root.SetParent(stage.transform, false);
        Material baseMaterial = MakeMaterial(baseColor);
        Cylinder(figure.Root, "TeamBase", new Vector3(0, 0.045f, 0), new Vector3(1.02f, 0.045f, 1.02f), baseMaterial);
        Cylinder(figure.Root, "BaseRim", new Vector3(0, 0.006f, 0), new Vector3(1.04f, 0.006f, 1.04f), MakeMaterial(new Color32(130, 123, 89, 255)));
        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            return figure;
        }
        GameObject character = Instantiate(prefab, figure.Root);
        character.name = "Menu_" + prefab.name;
        character.transform.localPosition = Vector3.zero;
        character.transform.localRotation = Quaternion.identity;
        foreach (Collider collider in character.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
        foreach (Transform child in character.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = PreviewLayer;
        }
        Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return figure;
        }
        FitCharacterToBase(character.transform, figure.Root, renderers);
        CloneFigureMaterials(figure, renderers);
        return figure;
    }

    private static void FitCharacterToBase(Transform character, Transform baseTransform, Renderer[] renderers)
    {
        Bounds bounds = BoundsOf(renderers);
        character.localScale *= 2.35f / Mathf.Max(0.01f, bounds.size.y);
        bounds = BoundsOf(renderers);
        Vector3 foot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        character.position += baseTransform.TransformPoint(Vector3.up * 0.09f) - foot;
    }

    private void CloneFigureMaterials(Figure figure, Renderer[] renderers)
    {
        foreach (Renderer renderer in renderers)
        {
            Material[] clones = renderer.sharedMaterials;
            for (int i = 0; i < clones.Length; i++)
            {
                if (clones[i] == null)
                {
                    continue;
                }
                Material clone = new Material(clones[i]);
                clones[i] = clone;
                materials.Add(clone);
                string property = clone.HasProperty("_BaseColor") ? "_BaseColor" : clone.HasProperty("_BaseColorFactor") ? "_BaseColorFactor" : null;
                if (property != null) figure.Surfaces.Add(new Surface { Material = clone, ColorProperty = property, Color = clone.GetColor(property) });
            }
            renderer.sharedMaterials = clones;
        }
    }

    private void FindSceneLights()
    {
        sceneLights.Clear();
        lightStates.Clear();
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type != LightType.Directional || light.transform.IsChildOf(stage.transform))
            {
                continue;
            }
            sceneLights.Add(light);
            lightStates.Add(false);
        }
    }

    private void RenderStudio()
    {
        // Camera.Render is synchronous. Restore every external light before the main camera renders.
        for (int i = 0; i < sceneLights.Count; i++)
        {
            if (sceneLights[i] == null)
            {
                continue;
            }
            lightStates[i] = sceneLights[i].enabled;
            sceneLights[i].enabled = false;
        }
        try
        {
            previewCamera.Render();
        }
        finally
        {
            for (int i = 0; i < sceneLights.Count; i++)
                if (sceneLights[i] != null)
                {
                    sceneLights[i].enabled = lightStates[i];
                }
        }
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
        material.SetFloat("_Smoothness", 0.38f);
        materials.Add(material);
        return material;
    }

    private void Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        part.name = name;
        part.layer = PreviewLayer;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Collider>().enabled = false;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void AddLight(string name, LightType type, Vector3 position, float intensity, Color color)
    {
        var lightObject = new GameObject(name);
        lightObject.transform.SetParent(stage.transform, false);
        lightObject.transform.localPosition = position;
        lightObject.transform.localRotation = Quaternion.Euler(30, -25, 0);
        var light = lightObject.AddComponent<Light>();
        light.type = type;
        light.range = 8;
        light.intensity = intensity;
        light.color = color;
        light.cullingMask = 1 << PreviewLayer;
    }

    private void OnDestroy()
    {
        if (previewCamera != null)
        {
            previewCamera.targetTexture = null;
        }
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
        if (stage != null)
        {
            Destroy(stage);
        }
        foreach (Material material in materials)
        {
            if (material != null) Destroy(material);
        }
    }
}
