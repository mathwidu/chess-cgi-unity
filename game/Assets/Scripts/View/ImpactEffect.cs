using UnityEngine;

public static class ImpactEffect
{
    public static GameObject CreateImpact(Vector3 position, Color color)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "ImpactEffect";
        root.transform.position = position;
        root.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);

        Renderer renderer = root.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = color;
        renderer.sharedMaterial = material;

        Collider collider = root.GetComponent<Collider>();
        if (Application.isPlaying)
        {
            Object.Destroy(collider);
            Object.Destroy(root, 0.35f);
        }
        else if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        return root;
    }
}
