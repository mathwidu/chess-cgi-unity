using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public sealed class UiOnlyCurveData : ICurveInteractionDataProvider
{
    private const float PanelMargin = 150f;
    private const float PanelLookupSeconds = 1f;
    private const float MaxAimDistance = 10f;

    private readonly NearFarInteractor interactor;
    private readonly ICurveInteractionDataProvider curve;
    private RectTransform panel;
    private float nextPanelLookup;

    public UiOnlyCurveData(NearFarInteractor interactor)
    {
        this.interactor = interactor;
        curve = interactor;
    }

    public bool isActive => HasUiHit || TryGetIdleAim(out _);
    public bool hasValidSelect => curve.hasValidSelect;
    public Transform curveOrigin => curve.curveOrigin;
    public NativeArray<Vector3> samplePoints => curve.samplePoints;
    public Vector3 lastSamplePoint => curve.lastSamplePoint;

    private bool HasUiHit => interactor.TryGetCurrentUIRaycastResult(out _);

    private RectTransform Panel
    {
        get
        {
            if (panel == null && Time.unscaledTime >= nextPanelLookup)
            {
                nextPanelLookup = Time.unscaledTime + PanelLookupSeconds;
                GameHud hud = Object.FindFirstObjectByType<GameHud>();
                panel = hud != null ? hud.GetComponent<RectTransform>() : null;
            }

            return panel != null && panel.gameObject.activeInHierarchy ? panel : null;
        }
    }

    public EndPointType TryGetCurveEndPoint(out Vector3 endPoint, bool snapToSelectedAttachIfAvailable = false, bool snapToSnapVolumeIfAvailable = false)
    {
        if (!HasUiHit && TryGetIdleAim(out endPoint))
        {
            return EndPointType.EmptyCastHit;
        }

        return curve.TryGetCurveEndPoint(out endPoint, snapToSelectedAttachIfAvailable, snapToSnapVolumeIfAvailable);
    }

    public EndPointType TryGetCurveEndNormal(out Vector3 endNormal, bool snapToSelectedAttachIfAvailable = false)
    {
        return curve.TryGetCurveEndNormal(out endNormal, snapToSelectedAttachIfAvailable);
    }

    private bool TryGetIdleAim(out Vector3 point)
    {
        point = default;
        RectTransform hud = Panel;
        Transform origin = curve.curveOrigin;
        if (interactor.hasSelection || interactor.hasHover || hud == null || origin == null)
        {
            return false;
        }

        return HudAim.TryGetPanelPoint(new Ray(origin.position, origin.forward), hud, PanelMargin, MaxAimDistance,
            ~(1 << XRPhysicsPusher.PhysicsLayer), out point);
    }
}
