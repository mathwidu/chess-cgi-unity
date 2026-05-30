using NUnit.Framework;
using UnityEngine;

public class ScenePolishTests
{
    [Test]
    public void ApplyPolish_CreatesStableRootsAndDoesNotDuplicateThem()
    {
        GameObject rig = new GameObject("Scene Polish Test Rig");
        try
        {
            ScenePolish polish = rig.AddComponent<ScenePolish>();

            polish.ApplyPolish();
            polish.ApplyPolish();

            Assert.AreEqual(1, CountChildrenNamed(rig.transform, "CollegeTheme"));
            Assert.AreEqual(1, CountChildrenNamed(rig.transform, "LightingRig"));
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void ApplyPolish_CreatesPremiumLightingRig()
    {
        GameObject rig = new GameObject("Scene Polish Test Rig");
        try
        {
            ScenePolish polish = rig.AddComponent<ScenePolish>();

            polish.ApplyPolish();

            Transform lightingRig = rig.transform.Find("LightingRig");
            Assert.IsNotNull(lightingRig.Find("Key Light"));
            Assert.IsNotNull(lightingRig.Find("Fill Light"));
            Assert.IsNotNull(lightingRig.Find("Rim Light"));
            Assert.AreEqual(3, lightingRig.GetComponentsInChildren<Light>().Length);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void ApplyPolish_CreatesCollegeThemePropsWithoutColliders()
    {
        GameObject rig = new GameObject("Scene Polish Test Rig");
        try
        {
            ScenePolish polish = rig.AddComponent<ScenePolish>();

            polish.ApplyPolish();

            Transform collegeTheme = rig.transform.Find("CollegeTheme");
            Assert.IsNotNull(collegeTheme.Find("Table"));
            Assert.IsNotNull(collegeTheme.Find("BackWall"));
            Assert.IsNotNull(collegeTheme.Find("Whiteboard"));
            Assert.IsNotNull(collegeTheme.Find("Notebook"));
            Assert.IsNotNull(collegeTheme.Find("Books"));
            Assert.IsNotNull(collegeTheme.Find("CGIWhiteboardMark"));
            Assert.AreEqual(0, collegeTheme.GetComponentsInChildren<Collider>().Length);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    private static int CountChildrenNamed(Transform parent, string name)
    {
        int count = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).name == name)
            {
                count++;
            }
        }

        return count;
    }
}
