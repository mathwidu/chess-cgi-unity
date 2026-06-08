using NUnit.Framework;
using UnityEngine;

public class ModularCharacterRigTests
{
    [Test]
    public void AutoBind_FindsGeneratedRigPartsByName()
    {
        GameObject character = BuildRigObject();
        try
        {
            ModularCharacterRig rig = character.AddComponent<ModularCharacterRig>();

            Assert.IsTrue(rig.AutoBind());
            Assert.IsTrue(rig.CanAnimateWalk);
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }

    [Test]
    public void AutoBind_FindsBlenderHumanoidBoneAliases()
    {
        GameObject character = BuildBlenderRigObject();
        try
        {
            ModularCharacterRig rig = character.AddComponent<ModularCharacterRig>();

            Assert.IsTrue(rig.AutoBind());
            Assert.IsTrue(rig.CanAnimateWalk);
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }

    [Test]
    public void ApplyWalk_RotatesLegsInOppositeDirections()
    {
        GameObject character = BuildRigObject();
        try
        {
            ModularCharacterRig rig = character.AddComponent<ModularCharacterRig>();
            rig.AutoBind();
            Transform leftLeg = FindRequired(character.transform, "LeftLegRoot");
            Transform rightLeg = FindRequired(character.transform, "RightLegRoot");

            rig.ApplyWalk(0.25f);

            Assert.Greater(leftLeg.localEulerAngles.x, 1f);
            Assert.Greater(360f - rightLeg.localEulerAngles.x, 1f);
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }

    [Test]
    public void ApplyWalk_WithSettingsUsesReadableStride()
    {
        GameObject character = BuildRigObject();
        try
        {
            ModularCharacterRig rig = character.AddComponent<ModularCharacterRig>();
            rig.AutoBind();
            Transform leftLeg = FindRequired(character.transform, "LeftLegRoot");
            PieceMotionSettings settings = new PieceMotionSettings(0.82f, 0.045f, 3.2f, 0.45f, 1.5f, 0.018f, 0.024f);

            rig.ApplyWalk(0.18f, settings);

            Assert.Greater(Mathf.Abs(leftLeg.localEulerAngles.x), 1f);
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }

    [Test]
    public void ResetPose_RestoresWalkRotations()
    {
        GameObject character = BuildRigObject();
        try
        {
            ModularCharacterRig rig = character.AddComponent<ModularCharacterRig>();
            rig.AutoBind();
            Transform leftLeg = FindRequired(character.transform, "LeftLegRoot");

            rig.ApplyWalk(0.25f);
            rig.ResetPose();

            Assert.AreEqual(Quaternion.identity, leftLeg.localRotation);
        }
        finally
        {
            Object.DestroyImmediate(character);
        }
    }

    private static GameObject BuildRigObject()
    {
        GameObject root = new GameObject("ModularCharacter");
        AddChild(root.transform, "TorsoRoot");
        AddChild(root.transform, "HeadRoot");
        AddChild(root.transform, "LeftArmRoot");
        AddChild(root.transform, "RightArmRoot");
        AddChild(root.transform, "LeftLegRoot");
        AddChild(root.transform, "RightLegRoot");
        AddChild(root.transform, "LeftFootRoot");
        AddChild(root.transform, "RightFootRoot");
        return root;
    }

    private static GameObject BuildBlenderRigObject()
    {
        GameObject root = new GameObject("BlenderCharacter");
        AddChild(root.transform, "Hips");
        AddChild(root.transform, "Spine");
        AddChild(root.transform, "Chest");
        AddChild(root.transform, "Head");
        AddChild(root.transform, "UpperArm.L");
        AddChild(root.transform, "UpperArm.R");
        AddChild(root.transform, "Thigh.L");
        AddChild(root.transform, "Thigh.R");
        AddChild(root.transform, "Foot.L");
        AddChild(root.transform, "Foot.R");
        return root;
    }

    private static Transform AddChild(Transform parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static Transform FindRequired(Transform root, string targetName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == targetName)
            {
                return child;
            }
        }

        Assert.Fail($"Missing child {targetName}");
        return null;
    }
}
