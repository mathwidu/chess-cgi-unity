using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public sealed class UiOnlyCurveData : ICurveInteractionDataProvider
{
    private readonly NearFarInteractor interactor;
    private readonly ICurveInteractionDataProvider curve;

    public UiOnlyCurveData(NearFarInteractor interactor)
    {
        this.interactor = interactor;
        curve = interactor;
    }

    public bool isActive => interactor.TryGetCurrentUIRaycastResult(out _);
    public bool hasValidSelect => curve.hasValidSelect;
    public Transform curveOrigin => curve.curveOrigin;
    public NativeArray<Vector3> samplePoints => curve.samplePoints;
    public Vector3 lastSamplePoint => curve.lastSamplePoint;

    public EndPointType TryGetCurveEndPoint(out Vector3 endPoint, bool snapToSelectedAttachIfAvailable = false, bool snapToSnapVolumeIfAvailable = false)
    {
        return curve.TryGetCurveEndPoint(out endPoint, snapToSelectedAttachIfAvailable, snapToSnapVolumeIfAvailable);
    }

    public EndPointType TryGetCurveEndNormal(out Vector3 endNormal, bool snapToSelectedAttachIfAvailable = false)
    {
        return curve.TryGetCurveEndNormal(out endNormal, snapToSelectedAttachIfAvailable);
    }
}
