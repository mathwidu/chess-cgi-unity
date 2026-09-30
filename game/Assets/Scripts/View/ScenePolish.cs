using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public sealed class ScenePolish : MonoBehaviour
{
    private const string CollegeThemeName = "CollegeTheme";
    private const string LightingRigName = "LightingRig";
    private const string ClassroomResource = "Environment/FeevaleComputerLab";
    // The authored room uses metres; BoardView uses 1.25 units per square.
    private const float BoardMetresPerUnit = 0.045f;
    private const float AuthoredBoardHeight = 0.78f;

    [SerializeField] private bool applyOnAwake = true;
    private Transform classroom;
    private BoardView board;
    private UniversalAdditionalLightData keyLightData;
    private readonly Light[] indoorFill = new Light[2];
    private ReflectionProbe roomReflection;
    private Matrix4x4 reflectionRoomPose;
    private int reflectionDelayFrames;

    private bool builtForHeadset;

    public void ApplyPolish()
    {
        builtForHeadset = XRRig.IsHeadsetPresent;
        Transform collegeTheme = EnsureChildRoot(CollegeThemeName);
        Transform lightingRig = EnsureChildRoot(LightingRigName);

        BuildLightingRig(lightingRig);
        BuildCollegeTheme(collegeTheme);
        ApplyCameraDefaults();
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.51f, 0.53f, 0.55f);
        RenderSettings.ambientEquatorColor = new Color(0.39f, 0.40f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.17f, 0.15f, 0.12f);
    }

    private void Awake()
    {
        if (applyOnAwake)
        {
            ApplyPolish();
        }
    }

    private void Update()
    {
        // XR can come up after Awake (the XR simulator does); refit the room to the mode in use.
        if (Application.isPlaying && XRRig.IsHeadsetPresent != builtForHeadset)
        {
            builtForHeadset = XRRig.IsHeadsetPresent;
            BuildCollegeTheme(EnsureChildRoot(CollegeThemeName));
        }
    }

    private void LateUpdate()
    {
        // Follow the actual board pose, including a headset connected after Awake.
        // This also keeps the table aligned when the board's serialized pose changes.
        AlignClassroomToBoard();
        // Cache the static room once after its pose settles, not every frame.
        if (Application.isPlaying && roomReflection != null && reflectionDelayFrames > 0
            && --reflectionDelayFrames == 0)
            roomReflection.RenderProbe();
    }

    private void AlignClassroomToBoard()
    {
        if (classroom == null) return;
        if (board == null) board = Object.FindFirstObjectByType<BoardView>();
        if (board == null) return;
        Transform boardTransform = board.transform;
        Vector3 referenceScale = board.RoomReferenceScale;
        classroom.SetPositionAndRotation(
            board.RoomReferencePosition + boardTransform.rotation *
                Vector3.Scale(Vector3.down * (AuthoredBoardHeight / BoardMetresPerUnit), referenceScale),
            boardTransform.rotation);
        Vector3 scale = referenceScale / BoardMetresPerUnit;
        Vector3 parentScale = classroom.parent.lossyScale;
        classroom.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);
        if (keyLightData != null)
            keyLightData.usePipelineSettings = referenceScale.x > .1f;
        float roomScale = Mathf.Abs(classroom.lossyScale.x);
        for (int i = 0; i < indoorFill.Length; i++)
        {
            if (indoorFill[i] == null) continue;
            indoorFill[i].enabled = true;
            indoorFill[i].transform.position = classroom.TransformPoint(new Vector3(0, 2.85f, i == 0 ? -2.1f : 2.3f));
            indoorFill[i].transform.rotation = classroom.rotation * Quaternion.Euler(90f, 0f, 0f);
            indoorFill[i].range = 5.8f * roomScale;
            // URP point attenuation uses squared world distance. Preserve the
            // same illumination when the desktop room is enlarged with the board.
            indoorFill[i].intensity = 2.1f * roomScale * roomScale;
        }
        if (roomReflection != null && reflectionRoomPose != classroom.localToWorldMatrix)
        {
            reflectionRoomPose = classroom.localToWorldMatrix;
            roomReflection.transform.SetPositionAndRotation(classroom.TransformPoint(new Vector3(0, 1.15f, 0)), classroom.rotation);
            roomReflection.size = new Vector3(6.8f, 3.3f, 9.4f) * roomScale;
            roomReflection.center = new Vector3(0, .5f, .1f) * roomScale;
            roomReflection.nearClipPlane = .08f * roomScale;
            roomReflection.farClipPlane = 12f * roomScale;
            roomReflection.blendDistance = .3f * roomScale;
            reflectionDelayFrames = 2;
        }
    }

    private void BuildLightingRig(Transform lightingRig)
    {
        ClearChildren(lightingRig);

        Light key = CreateLight(lightingRig, "Key Light", LightType.Directional, new Vector3(0f, 2f, 0f));
        // The recorded room is lit by overhead tubes at night. One shared
        // shadow light represents them; individual fixtures remain emissive.
        key.transform.rotation = Quaternion.Euler(68f, -35f, 0f);
        key.intensity = 0.72f;
        key.color = new Color(0.97f, 0.985f, 1f);
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.64f;
        // The desktop pipeline's 0.1 depth bias causes self-shadow stripes on
        // the metre-scale table. Override this light only at tabletop scale.
        key.shadowBias = 1f;
        key.shadowNormalBias = .5f;
        keyLightData = key.GetUniversalAdditionalLightData();
        // Keep the shadow source stable even when diffuse room fill is brighter.
        RenderSettings.sun = key;

        Light fill = CreateLight(lightingRig, "Fill Light", LightType.Directional, new Vector3(0f, 2f, 0f));
        fill.transform.rotation = Quaternion.Euler(35f, 125f, 0f);
        fill.intensity = 0.95f;
        fill.color = new Color(0.94f, 0.97f, 1f);
        fill.shadows = LightShadows.None;

        for (int i = 0; i < indoorFill.Length; i++)
        {
            indoorFill[i] = CreateLight(lightingRig, "Ceiling Fill " + (i + 1), LightType.Spot, Vector3.zero);
            indoorFill[i].enabled = false;
            indoorFill[i].color = new Color(.97f, .985f, 1f);
            indoorFill[i].shadows = LightShadows.None;
            indoorFill[i].spotAngle = 150f;
            indoorFill[i].innerSpotAngle = 90f;
        }

        var reflectionObject = new GameObject("Room Reflection");
        reflectionObject.transform.SetParent(lightingRig, false);
        roomReflection = reflectionObject.AddComponent<ReflectionProbe>();
        roomReflection.mode = ReflectionProbeMode.Realtime;
        roomReflection.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        roomReflection.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        roomReflection.resolution = 128;
        roomReflection.hdr = false;
        roomReflection.boxProjection = true;
        roomReflection.intensity = .65f;
        roomReflection.clearFlags = ReflectionProbeClearFlags.SolidColor;
        roomReflection.backgroundColor = new Color(.40f, .45f, .5f);
        // The environment alone is on Ignore Raycast (built-in layer 2).
        // Exclude moving pieces, world-space HUD and the isolated preview studio.
        roomReflection.cullingMask = 1 << 2;
        reflectionRoomPose = Matrix4x4.zero;
        reflectionDelayFrames = 0;
    }

    private void BuildCollegeTheme(Transform collegeTheme)
    {
        ClearChildren(collegeTheme);

        GameObject prefab = Resources.Load<GameObject>(ClassroomResource);
        if (prefab != null)
        {
            classroom = Object.Instantiate(prefab, collegeTheme, false).transform;
            classroom.name = "FeevaleComputerLab";
            foreach (Transform part in classroom.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = 2;
            AlignClassroomToBoard();

            GameObject labTable = new GameObject("Table");
            labTable.transform.SetParent(collegeTheme, false);
            if (board != null)
            {
                board.FitVrRoomToMode(labTable.transform);
            }
            labTable.AddComponent<TableView>().BuildOnLabDesk(classroom);
            return;
        }

        classroom = null;

        // Floor and table are modelled in VR meters; the board fits the room to the current mode.
        board = Object.FindFirstObjectByType<BoardView>();
        if (board != null)
        {
            board.FitVrRoomToMode(collegeTheme);
        }

        Material floorMaterial = CreateMaterial("Runtime_Floor", new Color(0.32f, 0.31f, 0.29f), 0f, 0.4f);
        CreateCube(collegeTheme, "Floor", new Vector3(0f, -0.02f, 0f), new Vector3(4f, 0.04f, 4f), floorMaterial, false);

        GameObject table = new GameObject("Table");
        table.transform.SetParent(collegeTheme, false);
        table.AddComponent<TableView>().Build();
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

        // Leave space for the coordinates above the desktop action bar.
        camera.fieldOfView = XRRig.IsHeadsetPresent ? 42f : 50f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.36f, 0.34f, 0.31f);
        // The desktop board is enlarged; its room uses the same scale.
        camera.farClipPlane = Mathf.Max(camera.farClipPlane, 250f);
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

    internal static GameObject CreateCube(
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

    internal static Material CreateMaterial(string name, Color color, float metallic, float smoothness)
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
            // Destroy is deferred; an inactive leftover never runs Start beside its replacement.
            child.SetActive(false);
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
