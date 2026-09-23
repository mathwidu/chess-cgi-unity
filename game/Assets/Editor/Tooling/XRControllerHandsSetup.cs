using UnityEditor;
using UnityEngine;

public static class XRControllerHandsSetup
{
    private const string RayMaterialPath = "Assets/Resources/XR/ControllerRayMaterial.mat";
    private const string HandMaterialPath = "Assets/Resources/XR/HandsDefaultMaterial.mat";
    private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

    [MenuItem("Chess CGI/VR/Create Controller Hands And Ray Material")]
    public static void CreateControllerHandsAndRayMaterial()
    {
        CreateRayMaterial();
        CreateControllerHand("Assets/Resources/XR/LeftHand.fbx", "Assets/Resources/XR/LeftControllerHand.prefab",
            new Vector3(-0.03f, -0.02f, -0.08f), Quaternion.Euler(0f, 0f, 90f));
        CreateControllerHand("Assets/Resources/XR/RightHand.fbx", "Assets/Resources/XR/RightControllerHand.prefab",
            new Vector3(0.03f, -0.02f, -0.08f), Quaternion.Euler(0f, 0f, -90f));

        AssetDatabase.SaveAssets();
        Debug.Log($"CHESS_CGI_XR_CONTROLLER_HANDS_SETUP rayMaterial={RayMaterialPath}");
    }

    private static void CreateRayMaterial()
    {
        Shader shader = Shader.Find(UnlitShaderName);
        if (shader == null)
        {
            throw new System.InvalidOperationException($"Shader \"{UnlitShaderName}\" was not found.");
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(RayMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, RayMaterialPath);
        }

        material.shader = shader;
        material.color = Color.cyan;
        EditorUtility.SetDirty(material);
    }

    private static void CreateControllerHand(string modelPath, string prefabPath, Vector3 localPosition, Quaternion localRotation)
    {
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        Material handMaterial = AssetDatabase.LoadAssetAtPath<Material>(HandMaterialPath);
        if (model == null || handMaterial == null)
        {
            throw new System.InvalidOperationException($"Missing {modelPath} or {HandMaterialPath}.");
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.transform.SetLocalPositionAndRotation(localPosition, localRotation);
        foreach (Renderer handRenderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            handRenderer.sharedMaterial = handMaterial;
        }

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }
}
