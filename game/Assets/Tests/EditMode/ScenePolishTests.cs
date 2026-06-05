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
            Assert.IsNotNull(collegeTheme.Find("Floor"));
            Assert.IsNotNull(collegeTheme.Find("Table"));
            Assert.IsNotNull(collegeTheme.Find("NorthWall"));
            Assert.IsNotNull(collegeTheme.Find("SouthWall"));
            Assert.IsNotNull(collegeTheme.Find("LeftWall"));
            Assert.IsNotNull(collegeTheme.Find("RightWall"));
            Assert.IsNotNull(collegeTheme.Find("NorthWhiteboard"));
            Assert.IsNotNull(collegeTheme.Find("SouthWhiteboard"));
            Assert.IsNotNull(collegeTheme.Find("Notebook"));
            Assert.IsNotNull(collegeTheme.Find("Books"));
            Assert.IsNotNull(collegeTheme.Find("CGIWhiteboardMarkNorth"));
            Assert.IsNotNull(collegeTheme.Find("CGIWhiteboardMarkSouth"));
            Assert.IsNotNull(collegeTheme.Find("DeskTrim"));
            Assert.IsNotNull(collegeTheme.Find("MarkerTrayNorth"));
            Assert.IsNotNull(collegeTheme.Find("MarkerTraySouth"));
            Assert.IsNotNull(collegeTheme.Find("SmallClock"));
            Assert.AreEqual(0, collegeTheme.GetComponentsInChildren<Collider>().Length);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void ApplyPolish_KeepsWallsOutsideTurnCameraPath()
    {
        GameObject rig = new GameObject("Scene Polish Test Rig");
        try
        {
            ScenePolish polish = rig.AddComponent<ScenePolish>();

            polish.ApplyPolish();

            Transform collegeTheme = rig.transform.Find("CollegeTheme");
            Assert.IsNull(collegeTheme.Find("BackWall"));
            Assert.Greater(collegeTheme.Find("NorthWall").localPosition.z, 12f);
            Assert.Less(collegeTheme.Find("SouthWall").localPosition.z, -12f);
            Assert.Greater(Mathf.Abs(collegeTheme.Find("LeftWall").localPosition.x), 7f);
            Assert.Greater(Mathf.Abs(collegeTheme.Find("RightWall").localPosition.x), 7f);
        }
        finally
        {
            Object.DestroyImmediate(rig);
        }
    }

    [Test]
    public void ApplyPolish_CreatesClassroomDetailsWithoutBlockingBoard()
    {
        GameObject rig = new GameObject("Scene Polish Test Rig");
        try
        {
            ScenePolish polish = rig.AddComponent<ScenePolish>();

            polish.ApplyPolish();

            Transform collegeTheme = rig.transform.Find("CollegeTheme");
            Assert.IsNotNull(collegeTheme.Find("DeskTrim"));
            Assert.IsNotNull(collegeTheme.Find("MarkerTrayNorth"));
            Assert.IsNotNull(collegeTheme.Find("MarkerTraySouth"));
            Assert.IsNotNull(collegeTheme.Find("SmallClock"));
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
