#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Drives a visible test fingertip through the same physics contacts used in VR.
// This is only used by the batch review capture; it does not change XR devices.
public sealed class TableButtonPhysicsReview : MonoBehaviour
{
    public bool Finished { get; private set; }
    public Exception Error { get; private set; }
    private TableView table;
    private PhysicalTableButton heldButton;
    private Transform target;
    private Rigidbody rawPusher;
    private XRPhysicsPusher trackedPusher;
    private float radius;
    private float heldDepth;
    private Material tipMaterial;

    public void Begin(TableView subject, bool vr)
    {
        table = subject;
        radius = .012f * table.transform.lossyScale.z;
        target = new GameObject("Review fingertip target").transform;
        target.SetParent(transform, false);
        var proxy = new GameObject("Review physics fingertip");
        proxy.transform.SetParent(transform, false);
        if (vr)
        {
            trackedPusher = proxy.AddComponent<XRPhysicsPusher>();
            trackedPusher.Configure(target, () => true, null, radius);
        }
        else
        {
            var collider = proxy.AddComponent<SphereCollider>();
            collider.radius = radius;
            collider.contactOffset = .0005f * table.transform.lossyScale.z;
            collider.excludeLayers = ~(1 << PhysicalTableButton.PhysicsLayer);
            rawPusher = proxy.AddComponent<Rigidbody>();
            rawPusher.isKinematic = true;
            rawPusher.useGravity = false;
        }
        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tip.name = "Review visible fingertip";
        tip.transform.SetParent(proxy.transform, false);
        tip.transform.localScale = Vector3.one * radius * 2f;
        Destroy(tip.GetComponent<Collider>());
        tipMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
        {
            name = "Review_Fingertip", color = new Color(.9f, .82f, .72f)
        };
        tipMaterial.SetFloat("_Smoothness", .3f);
        tip.GetComponent<Renderer>().sharedMaterial = tipMaterial;
        StartCoroutine(Guarded(Run()));
    }

    private IEnumerator Run()
    {
        int expected = 0;
        foreach (string name in new[] { "TableRaiseButton", "TableLowerButton" })
        {
            heldButton = GameObject.Find(name + "Mechanism").GetComponent<PhysicalTableButton>();
            heldDepth = -.03f;
            target.position = Position(heldDepth);
            if (rawPusher != null) rawPusher.position = target.position;
            yield return Hold(-.03f, 10);
            if (trackedPusher != null) Check(trackedPusher.IsArmed, "VR fingertip proxy did not arm outside the cap.");
            yield return Move(-.03f, .003f, 25);
            Check(table.HeightStep == expected, "A shallow touch must not activate the cap.");
            yield return Move(.003f, PhysicalTableButton.Stroke * .93f, 25);
            yield return Hold(PhysicalTableButton.Stroke * .93f, 30);
            expected = name == "TableRaiseButton" ? 1 : 0;
            Check(heldButton.IsPressed, "Physics contact did not depress " + name);
            Check(table.HeightStep == expected, "Held cap did not produce exactly one height step.");
            if (name == "TableRaiseButton")
            {
                yield return Move(heldDepth, -.03f, 25);
                yield return Hold(-.03f, 40);
                Check(heldButton.PressFraction < .04f, "The released cap did not spring back.");
            }
        }
        Debug.Log("TABLE_CONTROLS_PHYSICS_PASSED contact=kinematic-fingertip cap=dynamic joint=limited spring=return hold=single-step raise=1 lower=0");
        Finished = true;
        // Keep the blue cap depressed for the screenshot, with red released beside it.
    }

    private void FixedUpdate()
    {
        if (heldButton == null || target == null) return;
        target.position = Position(heldDepth);
        if (rawPusher != null) rawPusher.MovePosition(target.position);
    }

    private Vector3 Position(float depth) => heldButton.RestFaceCenter + heldButton.PressDirection *
        (depth * heldButton.transform.lossyScale.z - radius);

    private IEnumerator Move(float from, float to, int steps)
    {
        for (int i = 1; i <= steps; i++)
        {
            heldDepth = Mathf.Lerp(from, to, i / (float)steps);
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator Hold(float depth, int steps)
    {
        heldDepth = depth;
        for (int i = 0; i < steps; i++) yield return new WaitForFixedUpdate();
    }

    private IEnumerator Guarded(IEnumerator root)
    {
        var stack = new Stack<IEnumerator>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            object current = null;
            bool next = false;
            try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
            catch (Exception error) { Error = error; Finished = true; }
            if (Error != null) yield break;
            if (!next) { stack.Pop(); continue; }
            if (current is IEnumerator nested) { stack.Push(nested); continue; }
            yield return current;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private void OnDestroy()
    {
        if (tipMaterial != null) Destroy(tipMaterial);
    }
}

#endif
