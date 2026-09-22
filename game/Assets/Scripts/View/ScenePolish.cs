using UnityEngine;

[ExecuteAlways]
public sealed class ScenePolish : MonoBehaviour
{
    private const string CollegeThemeName = "CollegeTheme";
    private const string LightingRigName = "LightingRig";

    private static readonly Vector3 DesktopTablePosition = new Vector3(0f, -0.54f, 0f);
    private static readonly Vector3 DesktopTableScale = new Vector3(12f, 0.8f, 12f);
    private static readonly Vector3 VrTablePosition = new Vector3(0f, 0.387f, 0f);
    private static readonly Vector3 VrTableScale = new Vector3(0.9f, 0.774f, 0.9f);

    [SerializeField] private bool applyOnAwake = true;

    public void ApplyPolish()
    {
        Transform collegeTheme = EnsureChildRoot(CollegeThemeName);
        Transform lightingRig = EnsureChildRoot(LightingRigName);

        BuildLightingRig(lightingRig);
        BuildCollegeTheme(collegeTheme);
        ApplyCameraDefaults();
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.75f);
        RenderSettings.ambientEquatorColor = new Color(0.42f, 0.39f, 0.35f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.18f, 0.16f);
    }

    private void Awake()
    {
        if (applyOnAwake)
        {
            ApplyPolish();
        }
    }

    private void BuildLightingRig(Transform lightingRig)
    {
        ClearChildren(lightingRig);

        Light key = CreateLight(lightingRig, "Key Light", LightType.Directional, new Vector3(0f, 2f, 0f));
        key.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        key.intensity = 1.2f;
        key.color = new Color(1f, 0.95f, 0.86f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.6f;

        Light fill = CreateLight(lightingRig, "Fill Light", LightType.Directional, new Vector3(0f, 2f, 0f));
        fill.transform.rotation = Quaternion.Euler(35f, 150f, 0f);
        fill.intensity = 0.45f;
        fill.color = new Color(0.78f, 0.85f, 1f);
        fill.shadows = LightShadows.None;
    }

    private void BuildCollegeTheme(Transform collegeTheme)
    {
        ClearChildren(collegeTheme);

        Material tableMaterial = CreateMaterial("Runtime_Table_Wood", new Color(0.42f, 0.27f, 0.17f), 0.38f, 0.48f);
        Material floorMaterial = CreateMaterial("Runtime_Floor", new Color(0.32f, 0.31f, 0.29f), 0f, 0.4f);

        bool headsetPresent = XRRig.IsHeadsetPresent;
        Vector3 tablePosition = headsetPresent ? VrTablePosition : DesktopTablePosition;
        Vector3 tableScale = headsetPresent ? VrTableScale : DesktopTableScale;

        CreateCube(collegeTheme, "Floor", new Vector3(0f, -0.02f, 0f), new Vector3(4f, 0.04f, 4f), floorMaterial, false);
        CreateCube(collegeTheme, "Table", tablePosition, tableScale, tableMaterial, false);
    }

    private void ApplyCameraDefaults()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = Object.FindFirstObjectByType<Camera>();
        }

        if (camera == null)
        {
            return;
        }

        camera.fieldOfView = 42f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.36f, 0.34f, 0.31f);
    }

    private Transform EnsureChildRoot(string rootName)
    {
        return EnsureChild(transform, rootName);
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            return existing;
        }

        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        return child.transform;
    }

    private static Light CreateLight(Transform parent, string name, LightType type, Vector3 position)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent);
        lightObject.transform.localPosition = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = type;
        return light;
    }

    private static GameObject CreateCube(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        bool keepCollider)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;

        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.sharedMaterial = material;

        if (!keepCollider)
        {
            DestroyCollider(cube);
        }

        return cube;
    }

    private static Material CreateMaterial(string name, Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader)
        {
            name = name
        };
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private static void DestroyCollider(GameObject gameObject)
    {
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(collider);
        }
        else
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Object.Destroy(child);
            }
            else
            {
                Object.DestroyImmediate(child);
            }
        }
    }
}
