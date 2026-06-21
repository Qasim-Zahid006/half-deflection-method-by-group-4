using UnityEngine;

/// <summary>
/// ConnectingWire Module — Phases 1, 2, 3 complete
/// 
/// Renders a smooth curved wire between two terminal Transforms.
/// Supports manual placement (assign in Inspector) and runtime creation (via WireManager).
/// Includes energized state with color change and animated current flow particles.
/// 
/// PUBLIC API (for other modules):
///   wire.Connect(transformA, transformB)  → connect two terminals programmatically
///   wire.Disconnect()                     → disconnect the wire
///   wire.EnergizeWire(true/false)         → toggle current flow visual
///   wire.IsConnected                      → bool: are both terminals assigned?
///   wire.IsEnergized                      → bool: is wire showing current flow?
///   wire.resistance                       → float: wire's resistance (typically ~0)
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ConnectingWire : MonoBehaviour
{
    [Header("Wire Connection Points")]
    [Tooltip("Drag any Transform here — wire starts at this point")]
    public Transform startTerminal;

    [Tooltip("Drag any Transform here — wire ends at this point")]
    public Transform endTerminal;

    [Header("Wire Properties")]
    [Tooltip("Wire resistance in Ohms (typically ~0 for ideal connecting wire)")]
    public float resistance = 0.001f;

    [Header("Visual Settings")]
    [Tooltip("How much the wire sags downward (0 = straight, 1 = strong sag)")]
    [Range(0f, 1f)] public float sagAmount = 0.2f;

    [Tooltip("Number of line segments — more = smoother curve")]
    [Range(2, 50)] public int segmentCount = 20;

    [Header("Wire State Colors")]
    [Tooltip("Color when no current flows")]
    public Color idleColor = new Color(0.1f, 0.1f, 0.1f);

    [Tooltip("Color when wire is energized (current flowing)")]
    public Color energizedColor = new Color(0.9f, 0.4f, 0.1f);

    [Header("Visual References")]
    public Transform endpointA;
    public Transform endpointB;

    [Header("Phase 3 — Current Flow Effect")]
    [Tooltip("Particle system that activates when wire is energized")]
    public GameObject currentFlowEffect;

    [Tooltip("Set true to show energized state (testing only — Circuit Engine will control this)")]
    public bool isEnergized = false;

    // === Private state ===
    private LineRenderer lineRenderer;
    private bool lastEnergized = false;

    // === Public properties ===
    public bool IsConnected
    {
        get { return startTerminal != null && endTerminal != null; }
    }

    public bool IsEnergized
    {
        get { return isEnergized; }
        set { isEnergized = value; UpdateColor(); }
    }

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segmentCount;
    }

    void Start()
    {
        UpdateColor();
    }

    void Update()
    {
        if (isEnergized != lastEnergized)
        {
            UpdateColor();
            lastEnergized = isEnergized;
        }

        UpdateWireVisual();

        // Pulse wire color when energized
        if (isEnergized && lineRenderer != null)
        {
            // Pulse between energizedColor and a brighter version
            float pulse = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;  // 0 to 1
            Color pulseColor = Color.Lerp(energizedColor, Color.yellow, pulse);
            lineRenderer.startColor = pulseColor;
            lineRenderer.endColor = pulseColor;
        }

        // Disable the current flow effect (shadder thing) completely
        // if (isEnergized)
        // {
        //     UpdateParticleSystemPath();
        // }
    }

    void UpdateWireVisual()
    {
        if (!IsConnected)
        {
            lineRenderer.enabled = false;
            if (endpointA != null) endpointA.gameObject.SetActive(false);
            if (endpointB != null) endpointB.gameObject.SetActive(false);
            return;
        }

        lineRenderer.enabled = true;
        if (endpointA != null) endpointA.gameObject.SetActive(true);
        if (endpointB != null) endpointB.gameObject.SetActive(true);

        Vector3 startPos = startTerminal.position;
        Vector3 endPos = endTerminal.position;

        // Draw catenary curve (gravity sag) between two points
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector3 point = Vector3.Lerp(startPos, endPos, t);
            float sag = sagAmount * 4f * t * (1f - t);
            point.y -= sag;
            lineRenderer.SetPosition(i, point);
        }

        // Move visual endpoints to terminals
        if (endpointA != null) endpointA.position = startPos;
        if (endpointB != null) endpointB.position = endPos;
    }

    void UpdateColor()
    {
        if (lineRenderer == null) return;

        if (currentFlowEffect != null)
        {
            currentFlowEffect.SetActive(false); // Always hide the "shadder thing"
        }

        // If not energized, just set static idle color
        if (!isEnergized)
        {
            lineRenderer.startColor = idleColor;
            lineRenderer.endColor = idleColor;
        }
        // If energized, color will pulse in Update()
    }


    void UpdateParticleSystemPath()
    {
        if (currentFlowEffect == null) return;
        if (!IsConnected) return;

        ParticleSystem ps = currentFlowEffect.GetComponent<ParticleSystem>();
        if (ps == null) return;

        // Move particle system to the wire's midpoint
        Vector3 midpoint = Vector3.Lerp(startTerminal.position, endTerminal.position, 0.5f);
        midpoint.y -= sagAmount;  // adjust for wire sag
        currentFlowEffect.transform.position = midpoint;

        // Align the edge shape with the wire's direction
        Vector3 direction = endTerminal.position - startTerminal.position;
        float distance = direction.magnitude;

        if (distance > 0.01f)
        {
            // For Single Sided Edge, particles spawn along the local X axis
            // So we need to rotate so X points along the wire
            currentFlowEffect.transform.rotation = Quaternion.LookRotation(Vector3.up, direction.normalized);
        }

        // Set edge length to match wire length
        var shape = ps.shape;
        shape.radius = distance / 2f;
    }

    // ===== PUBLIC API =====

    public void Connect(Transform a, Transform b)
    {
        startTerminal = a;
        endTerminal = b;
        UpdateWireVisual();
        Debug.Log($"Wire connected: {a.name} ↔ {b.name}");
    }

    public void Disconnect()
    {
        startTerminal = null;
        endTerminal = null;
        lineRenderer.enabled = false;
    }

    public void EnergizeWire(bool energize)
    {
        isEnergized = energize;
        UpdateColor();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateColor();
        }
    }
}