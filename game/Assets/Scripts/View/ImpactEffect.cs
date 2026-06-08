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
        renderer.sharedMaterial = RuntimeMaterialFactory.Create("Runtime_ImpactEffect", color);

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
