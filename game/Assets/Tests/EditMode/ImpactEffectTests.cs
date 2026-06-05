using NUnit.Framework;
using UnityEngine;

public class ImpactEffectTests
{
    [Test]
    public void CreateImpact_CreatesShortLivedVisualRoot()
    {
        GameObject root = ImpactEffect.CreateImpact(Vector3.one, Color.yellow);
        try
        {
            Assert.AreEqual("ImpactEffect", root.name);
            Assert.IsNotNull(root.GetComponentInChildren<Renderer>());
            Assert.AreEqual(Vector3.one, root.transform.position);
            Assert.IsNull(root.GetComponent<Collider>());
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
