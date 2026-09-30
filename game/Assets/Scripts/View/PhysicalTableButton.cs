using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// The cap is a dynamic body. A joint guides its travel; collisions supply the push.
public sealed class PhysicalTableButton : MonoBehaviour
{
    public const int PhysicsLayer = 27;
    public const float Stroke = 0.012f;
    public const float CapDepth = 0.022f;
    private const float ActivationFraction = 0.7f;
    private const float ReleaseFraction = 0.15f;
    private const float Spring = 600f;
    private const float Damping = 40f;

    private readonly HashSet<Collider> contacts = new HashSet<Collider>();
    private Rigidbody anchorBody;
    private Rigidbody capBody;
    private Mesh capMesh;
    private Material capMaterial;
    private Material rimMaterial;
    private TableHeightButtonGraphic face;
    private Color availableColor;
    private UnityAction onPressed;
    private bool available = true;
    private bool armed = true;
    private bool allowPointer;
    private float pointerPressUntil;
    private Vector3 savedPosition;
    private Quaternion savedRotation;

    public Rigidbody Body => capBody;
    public TableHeightButtonGraphic FaceGraphic => face;
    public Vector3 PressDirection => transform.forward;
    public Vector3 RestFaceCenter => transform.TransformPoint(Vector3.back * (CapDepth * 0.5f));
    public Vector3 FaceCenter => capBody.position - PressDirection * (CapDepth * 0.5f * transform.lossyScale.z);
    public float PressDepth => transform.InverseTransformPoint(capBody.position).z;
    public float PressFraction => Mathf.Clamp01(PressDepth / Stroke);
    public bool IsPressed => PressFraction >= ActivationFraction;
    public bool IsAvailable => available;

    public void Configure(float diameter, Color color, bool pointsUp, bool pointerInput, UnityAction pressed)
    {
        allowPointer = pointerInput;
        onPressed = pressed;
        availableColor = color;
        anchorBody = gameObject.AddComponent<Rigidbody>();
        anchorBody.isKinematic = true;
        anchorBody.useGravity = false;

        rimMaterial = ScenePolish.CreateMaterial("Runtime_Table_ButtonRim", new Color(0.12f, 0.15f, 0.19f), 0.65f, 0.45f);
        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "ButtonRim";
        rim.transform.SetParent(transform, false);
        rim.transform.localPosition = Vector3.forward * 0.0145f;
        rim.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rim.transform.localScale = new Vector3(diameter + .012f, .006f, diameter + .012f);
        Destroy(rim.GetComponent<Collider>());
        rim.GetComponent<Renderer>().sharedMaterial = rimMaterial;

        var cap = new GameObject("PressableCap", typeof(MeshFilter), typeof(MeshRenderer));
        cap.layer = PhysicsLayer;
        cap.transform.SetParent(transform, false);
        capMesh = BuildCylinder(diameter * 0.5f, CapDepth);
        cap.GetComponent<MeshFilter>().sharedMesh = capMesh;
        capMaterial = ScenePolish.CreateMaterial("Runtime_Table_ButtonCap", color, 0.1f, 0.38f);
        cap.GetComponent<MeshRenderer>().sharedMaterial = capMaterial;
        var collider = cap.AddComponent<MeshCollider>();
        collider.sharedMesh = capMesh;
        collider.convex = true;
        // Default contact offsets are almost as large as the entire stroke.
        collider.contactOffset = .0005f * transform.lossyScale.z;

        capBody = cap.AddComponent<Rigidbody>();
        capBody.useGravity = false;
        capBody.mass = .2f;
        capBody.interpolation = RigidbodyInterpolation.Interpolate;
        capBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        capBody.solverIterations = 16;
        capBody.solverVelocityIterations = 8;
        capBody.maxDepenetrationVelocity = .5f * transform.lossyScale.z;
        cap.AddComponent<PhysicalTableButtonContacts>().Owner = this;

        var joint = cap.AddComponent<ConfigurableJoint>();
        joint.connectedBody = anchorBody;
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = Vector3.zero;
        joint.connectedAnchor = Vector3.forward * (Stroke * .5f);
        // Joint X follows the cap's local Z, into the mount.
        joint.axis = Vector3.forward;
        joint.secondaryAxis = Vector3.up;
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Locked;
        joint.linearLimit = new SoftJointLimit { limit = Stroke * .5f * transform.lossyScale.z, contactDistance = .0001f };
        joint.enableCollision = false;
        joint.enablePreprocessing = false;

        var canvasObject = new GameObject("CapFace", typeof(RectTransform), typeof(Canvas));
        var rect = (RectTransform)canvasObject.transform;
        rect.SetParent(cap.transform, false);
        rect.localPosition = Vector3.back * (CapDepth * .5f + .0002f);
        rect.sizeDelta = Vector2.one * diameter * 2000f;
        rect.localScale = Vector3.one / 2000f;
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = pointerInput ? Camera.main : XRRig.EyeCamera;
        if (pointerInput) canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        face = canvasObject.AddComponent<TableHeightButtonGraphic>();
        face.color = color;
        face.PointsUp = pointsUp;
        face.raycastTarget = pointerInput;
    }

    public void SetAvailable(bool value)
    {
        available = value;
        Color color = value ? availableColor : new Color(availableColor.r * .42f, availableColor.g * .42f, availableColor.b * .42f);
        face.color = color;
        capMaterial.color = color;
    }

    // Mouse input applies a force to the same body; activation still depends on travel.
    public void PressFromPointer()
    {
        if (allowPointer && available && armed)
            pointerPressUntil = Time.fixedTime + .2f;
    }

    public void PrepareMountMove()
    {
        savedPosition = transform.InverseTransformPoint(capBody.position);
        savedRotation = Quaternion.Inverse(transform.rotation) * capBody.rotation;
    }

    public void FinishMountMove()
    {
        // The table moves instantly by a height step or changes sides. Move the joint
        // anchor and cap together, preserving the actual depression without a solver kick.
        anchorBody.position = transform.position;
        anchorBody.rotation = transform.rotation;
        capBody.position = transform.TransformPoint(savedPosition);
        capBody.rotation = transform.rotation * savedRotation;
        capBody.linearVelocity = Vector3.zero;
        capBody.angularVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (capBody == null) return;
        contacts.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        float fraction = PressFraction;
        if (!armed && fraction <= ReleaseFraction && contacts.Count == 0 && Time.fixedTime >= pointerPressUntil)
            armed = true;

        if (armed && fraction >= ActivationFraction)
        {
            armed = false;
            if (available) onPressed?.Invoke();
        }

        float worldDepth = PressDepth * transform.lossyScale.z;
        float velocity = Vector3.Dot(capBody.linearVelocity, PressDirection);
        float acceleration = -Spring * worldDepth - Damping * velocity;
        if (Time.fixedTime < pointerPressUntil)
            acceleration += 1200f * Stroke * transform.lossyScale.z;
        capBody.AddForce(PressDirection * acceleration, ForceMode.Acceleration);
    }

    internal void Contact(Collider other, bool touching)
    {
        if (touching) contacts.Add(other);
        else contacts.Remove(other);
    }

    private void OnDestroy()
    {
        if (capMesh != null) Destroy(capMesh);
        if (capMaterial != null) Destroy(capMaterial);
        if (rimMaterial != null) Destroy(rimMaterial);
    }

    private static Mesh BuildCylinder(float radius, float depth)
    {
        const int segments = 48;
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float b = (i + 1) * Mathf.PI * 2f / segments;
            Vector3 p = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
            Vector3 q = new Vector3(Mathf.Cos(b) * radius, Mathf.Sin(b) * radius, 0f);
            int start = vertices.Count;
            vertices.Add(p + Vector3.back * (depth * .5f));
            vertices.Add(q + Vector3.back * (depth * .5f));
            vertices.Add(p + Vector3.forward * (depth * .5f));
            vertices.Add(q + Vector3.forward * (depth * .5f));
            vertices.Add(Vector3.back * (depth * .5f));
            vertices.Add(Vector3.forward * (depth * .5f));
            triangles.AddRange(new[] { start, start + 1, start + 2, start + 1, start + 3, start + 2,
                start + 4, start + 1, start, start + 5, start + 2, start + 3 });
        }
        var mesh = new Mesh { name = "Runtime_Table_ButtonCylinder" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}

// Collision callbacks belong to the dynamic cap, while its owner stays fixed to the mount.
public sealed class PhysicalTableButtonContacts : MonoBehaviour
{
    public PhysicalTableButton Owner;
    private void OnCollisionEnter(Collision collision) => Owner.Contact(collision.collider, true);
    private void OnCollisionStay(Collision collision) => Owner.Contact(collision.collider, true);
    private void OnCollisionExit(Collision collision) => Owner.Contact(collision.collider, false);
}
