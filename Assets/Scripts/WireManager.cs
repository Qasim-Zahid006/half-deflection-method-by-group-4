using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// WireManager — handles interactive wire creation through clicking and dragging.
/// 
/// USAGE:
///   1. Drag the WireManager prefab into your scene (or attach this script to any GameObject)
///   2. Assign the ConnectingWire prefab in the Inspector
///   3. Make sure all terminal anchors are tagged "TerminalAnchor" and have colliders
///   
/// USER INTERACTIONS:
///   - Left-click on a terminal → start drawing a wire
///   - Move mouse → wire follows cursor with snap preview
///   - Left-click on a second terminal → wire is permanently created
///   - Escape → cancel current drag
///   - Right-click on a wire → delete it
/// </summary>
public class WireManager : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("The ConnectingWire prefab to instantiate when user creates a wire")]
    public GameObject connectingWirePrefab;

    [Header("Drag Settings")]
    [Tooltip("How close (in meters) the mouse must be to a terminal to snap")]
    public float snapRadius = 0.45f;

    [Tooltip("Layer mask for terminal detection (use Default if unsure)")]
    public LayerMask terminalLayer = -1;

    [Header("Visual Feedback")]
    [Tooltip("Color of the wire while being dragged")]
    public Color dragColor = new Color(1f, 0.8f, 0.2f, 0.7f);

    // === State ===
    private bool isDragging = false;
    private Transform dragStartTerminal;
    private LineRenderer dragPreviewLine;
    private GameObject dragPreviewGO;

    // Track all created wires for management
    private List<ConnectingWire> allWires = new List<ConnectingWire>();

    void Update()
    {
        // Cancel drag with Escape
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelDrag();
            return;
        }

        if (WasPrimaryPressedThisFrame())
        {
            HandleLeftClick();
        }
        else if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }

        // Update drag preview while dragging
        if (isDragging)
        {
            UpdateDragPreview();
        }
    }

    void HandleLeftClick()
    {
        // Raycast from camera through mouse position
        Ray ray = Camera.main.ScreenPointToRay(GetPointerScreenPosition());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, terminalLayer))
        {
            // Check if we hit a terminal anchor
            if (hit.collider.CompareTag("TerminalAnchor"))
            {
                if (!isDragging)
                {
                    // First click — start dragging
                    StartDrag(hit.collider.transform);
                }
                else
                {
                    // Second click — finalize the wire
                    if (hit.collider.transform != dragStartTerminal)
                    {
                        FinalizeWire(hit.collider.transform);
                    }
                    else
                    {
                        Debug.Log("Can't connect terminal to itself");
                    }
                }
            }
        }
    }

    void HandleRightClick()
    {
        // Right-click on a wire to delete it
        Ray ray = Camera.main.ScreenPointToRay(GetPointerScreenPosition());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            ConnectingWire wire = hit.collider.GetComponentInParent<ConnectingWire>();
            if (wire != null)
            {
                DeleteWire(wire);
            }
        }
    }

    void StartDrag(Transform terminal)
    {
        dragStartTerminal = terminal;
        isDragging = true;

        // Create a temporary preview wire
        dragPreviewGO = new GameObject("DragPreview");
        dragPreviewLine = dragPreviewGO.AddComponent<LineRenderer>();
        dragPreviewLine.positionCount = 2;
        dragPreviewLine.startWidth = 0.04f;
        dragPreviewLine.endWidth = 0.04f;
        dragPreviewLine.useWorldSpace = true;
        dragPreviewLine.startColor = dragColor;
        dragPreviewLine.endColor = dragColor;
        dragPreviewLine.material = new Material(Shader.Find("Sprites/Default"));
        dragPreviewLine.startColor = dragColor;
        dragPreviewLine.endColor = dragColor;

        Debug.Log($"Started wire from {terminal.name}");
    }

    void UpdateDragPreview()
    {
        if (dragPreviewLine == null) return;

        Vector3 startPos = dragStartTerminal.position;
        Vector3 endPos = GetMouseWorldPosition();

        // Check if mouse is near another terminal — snap to it
        Transform snapTarget = FindNearestTerminal(endPos);
        if (snapTarget != null && snapTarget != dragStartTerminal)
        {
            endPos = snapTarget.position;
        }

        dragPreviewLine.SetPosition(0, startPos);
        dragPreviewLine.SetPosition(1, endPos);
    }

    Vector3 GetMouseWorldPosition()
    {
        // Project mouse onto the same depth as the starting terminal
        Vector3 screenPos = GetPointerScreenPosition();
        screenPos.z = Vector3.Distance(Camera.main.transform.position, dragStartTerminal.position);
        return Camera.main.ScreenToWorldPoint(screenPos);
    }

    bool WasPrimaryPressedThisFrame()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            return true;

        return Touchscreen.current != null
            && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
    }

    Vector3 GetPointerScreenPosition()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();

        if (Mouse.current != null)
            return Mouse.current.position.ReadValue();

        return Vector3.zero;
    }

    Transform FindNearestTerminal(Vector3 position)
    {
        GameObject[] anchors = GameObject.FindGameObjectsWithTag("TerminalAnchor");
        Transform closest = null;
        float closestDist = snapRadius;

        foreach (var anchor in anchors)
        {
            float dist = Vector3.Distance(anchor.transform.position, position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = anchor.transform;
            }
        }

        return closest;
    }

    void FinalizeWire(Transform endTerminal)
    {
        CreateWire(dragStartTerminal, endTerminal);

        Debug.Log($"Wire created: {dragStartTerminal.name} → {endTerminal.name}");

        // Clean up drag preview
        CancelDrag();
    }

    void CancelDrag()
    {
        if (dragPreviewGO != null)
        {
            Destroy(dragPreviewGO);
        }

        isDragging = false;
        dragStartTerminal = null;
        dragPreviewLine = null;
    }

    void DeleteWire(ConnectingWire wire)
    {
        if (wire == null) return;

        allWires.Remove(wire);
        Destroy(wire.gameObject);
        Debug.Log("Wire deleted");
    }

    public ConnectingWire CreateWire(Transform startTerminal, Transform endTerminal)
    {
        if (connectingWirePrefab == null)
        {
            Debug.LogError("ConnectingWire prefab not assigned to WireManager!");
            return null;
        }

        if (startTerminal == null || endTerminal == null || startTerminal == endTerminal)
            return null;

        GameObject wireGO = Instantiate(connectingWirePrefab);
        wireGO.name = $"Wire_{startTerminal.name}_to_{endTerminal.name}";

        ConnectingWire wireScript = wireGO.GetComponent<ConnectingWire>();
        if (wireScript != null)
        {
            wireScript.sagAmount = Mathf.Min(wireScript.sagAmount, 0.04f);
            wireScript.Connect(startTerminal, endTerminal);
            allWires.Add(wireScript);
        }

        return wireScript;
    }

    public void ClearAllWires()
    {
        allWires.RemoveAll(w => w == null);
        foreach (var wire in allWires)
            if (wire != null) Destroy(wire.gameObject);
        allWires.Clear();

        foreach (var wire in FindObjectsByType<ConnectingWire>())
            if (wire != null) Destroy(wire.gameObject);
    }

    /// <summary>
    /// Public API: get all wires currently in the scene
    /// </summary>
    public List<ConnectingWire> GetAllWires()
    {
        // Clean up null entries (destroyed wires)
        allWires.RemoveAll(w => w == null);
        return allWires;
    }
}
