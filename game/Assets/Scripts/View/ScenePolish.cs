using UnityEngine;

[ExecuteAlways]
public sealed class ScenePolish : MonoBehaviour
{
    private const string CollegeThemeName = "CollegeTheme";
    private const string LightingRigName = "LightingRig";

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

        Light key = CreateLight(lightingRig, "Key Light", LightType.Directional, new Vector3(0f, 4f, 0f));
        key.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        key.intensity = 1.45f;
        key.color = new Color(1f, 0.93f, 0.82f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.72f;

        Light fill = CreateLight(lightingRig, "Fill Light", LightType.Point, new Vector3(-5f, 5f, -4f));
        fill.intensity = 95f;
        fill.range = 13f;
        fill.color = new Color(0.72f, 0.82f, 1f);
        fill.shadows = LightShadows.None;

        Light rim = CreateLight(lightingRig, "Rim Light", LightType.Point, new Vector3(4f, 4.5f, 5f));
        rim.intensity = 60f;
        rim.range = 10f;
        rim.color = new Color(0.85f, 0.92f, 1f);
        rim.shadows = LightShadows.None;
    }

    private void BuildCollegeTheme(Transform collegeTheme)
    {
        ClearChildren(collegeTheme);

        Material tableMaterial = CreateMaterial("Runtime_Table_Wood", new Color(0.42f, 0.27f, 0.17f), 0.38f, 0.48f);
        Material wallMaterial = CreateMaterial("Runtime_Warm_Wall", new Color(0.62f, 0.58f, 0.51f), 0f, 0.55f);
        Material boardMaterial = CreateMaterial("Runtime_Whiteboard", new Color(0.86f, 0.88f, 0.84f), 0f, 0.6f);
        Material darkMaterial = CreateMaterial("Runtime_Dark_Prop", new Color(0.09f, 0.1f, 0.11f), 0f, 0.35f);
        Material accentMaterial = CreateMaterial("Runtime_CGI_Accent", new Color(0.18f, 0.36f, 0.5f), 0f, 0.5f);
        Material bookRed = CreateMaterial("Runtime_Book_Red", new Color(0.45f, 0.12f, 0.11f), 0f, 0.45f);
        Material bookBlue = CreateMaterial("Runtime_Book_Blue", new Color(0.1f, 0.2f, 0.42f), 0f, 0.45f);

        CreateCube(collegeTheme, "Table", new Vector3(0f, -0.22f, 0f), new Vector3(13.2f, 0.28f, 13.2f), tableMaterial, false);
        CreateCube(collegeTheme, "BackWall", new Vector3(0f, 2.6f, 6.35f), new Vector3(13.2f, 5.2f, 0.18f), wallMaterial, false);
        CreateCube(collegeTheme, "Whiteboard", new Vector3(0f, 3.2f, 6.23f), new Vector3(4.6f, 1.65f, 0.08f), boardMaterial, false);
        CreateCube(collegeTheme, "CGIWhiteboardMark", new Vector3(-1.55f, 3.35f, 6.17f), new Vector3(1.05f, 0.08f, 0.04f), accentMaterial, false);
        CreateCube(collegeTheme, "Notebook", new Vector3(-5.35f, 0.05f, -2.25f), new Vector3(1.05f, 0.08f, 0.75f), darkMaterial, false);

        Transform books = EnsureChild(collegeTheme, "Books");
        CreateCube(books, "Book Red", new Vector3(5.15f, 0.04f, -1.6f), new Vector3(0.88f, 0.08f, 0.55f), bookRed, false);
        CreateCube(books, "Book Blue", new Vector3(5.25f, 0.16f, -1.55f), new Vector3(0.82f, 0.08f, 0.5f), bookBlue, false);
        CreateCube(books, "Book Dark", new Vector3(5.35f, 0.28f, -1.5f), new Vector3(0.76f, 0.08f, 0.46f), darkMaterial, false);
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
