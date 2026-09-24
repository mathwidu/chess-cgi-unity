using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class ControllerHandPose : MonoBehaviour
{
    private const float BlendSpeed = 12f;

    private static readonly (string Joint, Vector3 Bend)[] PinchBends =
    {
        ("ThumbMetacarpal", new Vector3(0f, 3f, -23f)),
        ("ThumbProximal", new Vector3(32f, 18f, 0f)),
        ("ThumbDistal", new Vector3(0f, 4f, 0f)),
        ("IndexProximal", new Vector3(27f, 0f, 0f)),
        ("IndexIntermediate", new Vector3(49f, 0f, 0f)),
        ("IndexDistal", new Vector3(40f, 0f, 0f)),
        ("MiddleProximal", new Vector3(55f, 0f, 0f)),
        ("MiddleIntermediate", new Vector3(65f, 0f, 0f)),
        ("MiddleDistal", new Vector3(40f, 0f, 0f)),
        ("RingProximal", new Vector3(60f, 0f, 0f)),
        ("RingIntermediate", new Vector3(65f, 0f, 0f)),
        ("RingDistal", new Vector3(40f, 0f, 0f)),
        ("LittleProximal", new Vector3(60f, 0f, 0f)),
        ("LittleIntermediate", new Vector3(65f, 0f, 0f)),
        ("LittleDistal", new Vector3(40f, 0f, 0f)),
    };

    private readonly List<Transform> bones = new List<Transform>();
    private readonly List<Quaternion> restRotations = new List<Quaternion>();
    private readonly List<Quaternion> pinchRotations = new List<Quaternion>();
    private NearFarInteractor interactor;
    private float weight;

    public float Pinch => weight;

    public void Configure(NearFarInteractor grabbingInteractor, bool leftHand)
    {
        interactor = grabbingInteractor;
        string prefix = leftHand ? "L_" : "R_";
        float mirror = leftHand ? 1f : -1f;
        Dictionary<string, Transform> byName = new Dictionary<string, Transform>();
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            byName[child.name] = child;
        }

        foreach ((string joint, Vector3 bend) in PinchBends)
        {
            if (!byName.TryGetValue(prefix + joint, out Transform bone))
            {
                continue;
            }

            bones.Add(bone);
            restRotations.Add(bone.localRotation);
            pinchRotations.Add(bone.localRotation * Quaternion.Euler(bend.x, bend.y * mirror, bend.z * mirror));
        }
    }

    public void SetPinch(float amount)
    {
        weight = Mathf.Clamp01(amount);
        for (int i = 0; i < bones.Count; i++)
        {
            bones[i].localRotation = Quaternion.Slerp(restRotations[i], pinchRotations[i], weight);
        }
    }

    private void Update()
    {
        float target = interactor != null && interactor.hasSelection ? 1f : 0f;
        if (!Mathf.Approximately(weight, target))
        {
            SetPinch(Mathf.MoveTowards(weight, target, BlendSpeed * Time.deltaTime));
        }
    }
}
