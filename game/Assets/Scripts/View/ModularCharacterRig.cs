using UnityEngine;

public sealed class ModularCharacterRig : MonoBehaviour
{
    [SerializeField] private Transform torsoRoot;
    [SerializeField] private Transform headRoot;
    [SerializeField] private Transform leftArmRoot;
    [SerializeField] private Transform rightArmRoot;
    [SerializeField] private Transform leftLegRoot;
    [SerializeField] private Transform rightLegRoot;
    [SerializeField] private Transform leftFootRoot;
    [SerializeField] private Transform rightFootRoot;
    [SerializeField] private float legSwingAngle = 22f;
    [SerializeField] private float armSwingAngle = 16f;
    [SerializeField] private float footSwingAngle = 10f;

    private Vector3 torsoRestPosition;
    private Quaternion torsoRestRotation;
    private Quaternion headRestRotation;
    private Quaternion leftArmRestRotation;
    private Quaternion rightArmRestRotation;
    private Quaternion leftLegRestRotation;
    private Quaternion rightLegRestRotation;
    private Quaternion leftFootRestRotation;
    private Quaternion rightFootRestRotation;
    private bool restPoseCaptured;

    public bool CanAnimateWalk => leftLegRoot != null && rightLegRoot != null && leftFootRoot != null && rightFootRoot != null;

    public void Configure(
        Transform torso,
        Transform head,
        Transform leftArm,
        Transform rightArm,
        Transform leftLeg,
        Transform rightLeg,
        Transform leftFoot,
        Transform rightFoot)
    {
        torsoRoot = torso;
        headRoot = head;
        leftArmRoot = leftArm;
        rightArmRoot = rightArm;
        leftLegRoot = leftLeg;
        rightLegRoot = rightLeg;
        leftFootRoot = leftFoot;
        rightFootRoot = rightFoot;
        CaptureRestPose();
    }

    public bool AutoBind()
    {
        torsoRoot = torsoRoot != null ? torsoRoot : FindDescendant("TorsoRoot", "Chest", "Spine", "Hips");
        headRoot = headRoot != null ? headRoot : FindDescendant("HeadRoot", "Head");
        leftArmRoot = leftArmRoot != null ? leftArmRoot : FindDescendant("LeftArmRoot", "UpperArm.L", "UpperArm_L");
        rightArmRoot = rightArmRoot != null ? rightArmRoot : FindDescendant("RightArmRoot", "UpperArm.R", "UpperArm_R");
        leftLegRoot = leftLegRoot != null ? leftLegRoot : FindDescendant("LeftLegRoot", "Thigh.L", "Thigh_L");
        rightLegRoot = rightLegRoot != null ? rightLegRoot : FindDescendant("RightLegRoot", "Thigh.R", "Thigh_R");
        leftFootRoot = leftFootRoot != null ? leftFootRoot : FindDescendant("LeftFootRoot", "Foot.L", "Foot_L");
        rightFootRoot = rightFootRoot != null ? rightFootRoot : FindDescendant("RightFootRoot", "Foot.R", "Foot_R");
        CaptureRestPose();
        return CanAnimateWalk;
    }

    public void CaptureRestPose()
    {
        torsoRestPosition = torsoRoot != null ? torsoRoot.localPosition : Vector3.zero;
        torsoRestRotation = torsoRoot != null ? torsoRoot.localRotation : Quaternion.identity;
        headRestRotation = headRoot != null ? headRoot.localRotation : Quaternion.identity;
        leftArmRestRotation = leftArmRoot != null ? leftArmRoot.localRotation : Quaternion.identity;
        rightArmRestRotation = rightArmRoot != null ? rightArmRoot.localRotation : Quaternion.identity;
        leftLegRestRotation = leftLegRoot != null ? leftLegRoot.localRotation : Quaternion.identity;
        rightLegRestRotation = rightLegRoot != null ? rightLegRoot.localRotation : Quaternion.identity;
        leftFootRestRotation = leftFootRoot != null ? leftFootRoot.localRotation : Quaternion.identity;
        rightFootRestRotation = rightFootRoot != null ? rightFootRoot.localRotation : Quaternion.identity;
        restPoseCaptured = true;
    }

    public void ApplyWalk(float normalizedTime)
    {
        ApplyWalk(normalizedTime, PieceMotionSettings.Default);
    }

    public void ApplyWalk(float normalizedTime, PieceMotionSettings settings)
    {
        if (!restPoseCaptured)
        {
            CaptureRestPose();
        }

        float t = Mathf.Clamp01(normalizedTime);
        float phase = t * Mathf.PI * 2f * settings.StrideCycles;
        float stride = Mathf.Sin(phase);
        float lift = Mathf.Pow(Mathf.Abs(stride), 1.25f);
        float secondaryPhase = Mathf.Sin(phase * 2f);

        if (torsoRoot != null)
        {
            torsoRoot.localPosition = torsoRestPosition +
                Vector3.up * lift * settings.TorsoBobHeight +
                Vector3.right * stride * settings.BodySway;
            torsoRoot.localRotation = torsoRestRotation * Quaternion.Euler(secondaryPhase * 1.2f, stride * 1.6f, -stride * 1.4f);
        }

        if (headRoot != null)
        {
            headRoot.localRotation = headRestRotation * Quaternion.Euler(secondaryPhase * -0.9f, stride * -0.8f, 0f);
        }

        if (leftLegRoot != null)
        {
            leftLegRoot.localRotation = leftLegRestRotation * Quaternion.Euler(stride * legSwingAngle, 0f, 0f);
        }

        if (rightLegRoot != null)
        {
            rightLegRoot.localRotation = rightLegRestRotation * Quaternion.Euler(-stride * legSwingAngle, 0f, 0f);
        }

        if (leftFootRoot != null)
        {
            leftFootRoot.localRotation = leftFootRestRotation * Quaternion.Euler(-Mathf.Max(0f, stride) * footSwingAngle, 0f, 0f);
        }

        if (rightFootRoot != null)
        {
            rightFootRoot.localRotation = rightFootRestRotation * Quaternion.Euler(Mathf.Min(0f, stride) * footSwingAngle, 0f, 0f);
        }

        if (leftArmRoot != null)
        {
            leftArmRoot.localRotation = leftArmRestRotation * Quaternion.Euler(-stride * armSwingAngle, 0f, 0f);
        }

        if (rightArmRoot != null)
        {
            rightArmRoot.localRotation = rightArmRestRotation * Quaternion.Euler(stride * armSwingAngle, 0f, 0f);
        }
    }

    public void ResetPose()
    {
        if (torsoRoot != null)
        {
            torsoRoot.localPosition = torsoRestPosition;
            torsoRoot.localRotation = torsoRestRotation;
        }

        if (headRoot != null)
        {
            headRoot.localRotation = headRestRotation;
        }

        if (leftArmRoot != null)
        {
            leftArmRoot.localRotation = leftArmRestRotation;
        }

        if (rightArmRoot != null)
        {
            rightArmRoot.localRotation = rightArmRestRotation;
        }

        if (leftLegRoot != null)
        {
            leftLegRoot.localRotation = leftLegRestRotation;
        }

        if (rightLegRoot != null)
        {
            rightLegRoot.localRotation = rightLegRestRotation;
        }

        if (leftFootRoot != null)
        {
            leftFootRoot.localRotation = leftFootRestRotation;
        }

        if (rightFootRoot != null)
        {
            rightFootRoot.localRotation = rightFootRestRotation;
        }
    }

    private Transform FindDescendant(params string[] targetNames)
    {
        Transform[] descendants = GetComponentsInChildren<Transform>(true);
        foreach (Transform descendant in descendants)
        {
            foreach (string targetName in targetNames)
            {
                if (descendant.name == targetName)
                {
                    return descendant;
                }
            }
        }

        return null;
    }
}
