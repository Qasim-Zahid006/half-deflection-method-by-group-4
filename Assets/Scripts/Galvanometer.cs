using UnityEngine;
using TMPro;

public class Galvanometer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform needlePivot;
    [SerializeField] private TMP_Text readoutLabel;

    [Header("Sensitivity")]
    [Tooltip("Full-scale current in amps. Smaller = more sensitive. Default 1 mA")]
    [SerializeField] private float fullScaleAmps = 0.001f;

    [Tooltip("Below this, considered zero (noise floor)")]
    [SerializeField] private float detectionThreshold = 0.000005f;

    [Tooltip("Max needle deflection in degrees from zero")]
    [SerializeField] private float maxDeflection = 60f;

    [Header("Needle Physics (damped spring)")]
    [SerializeField] private float stiffness = 80f;
    [SerializeField] private float damping = 0.35f;

    [Header("Overload")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color overloadColor = Color.red;

    [Header("Runtime (read-only)")]
    [SerializeField] private float currentAmps;
    [SerializeField] private float displayedAngle;
    [SerializeField] private float angularVelocity;
    [SerializeField] private bool isOverloaded;

    private float targetAngle;
    private static readonly System.Collections.Generic.List<Galvanometer> allInstances = new();
    public static System.Collections.Generic.IReadOnlyList<Galvanometer> AllInstances => allInstances;
    void OnEnable() => allInstances.Add(this);
    void OnDisable() => allInstances.Remove(this);
    public void SetCurrent(float amps)
    {
        currentAmps = amps;
        float normalized = amps / fullScaleAmps;
        isOverloaded = Mathf.Abs(normalized) > 1f;
        normalized = Mathf.Clamp(normalized, -1f, 1f);
        targetAngle = normalized * maxDeflection;
    }

    public bool IsDetectingCurrent() => Mathf.Abs(currentAmps) > detectionThreshold;
    public float GetCurrentAmps() => currentAmps;
    public float GetCurrentMilliAmps() => currentAmps * 1000f;
    public bool IsOverloaded() => isOverloaded;

    void Update()
    {
        if (needlePivot == null) return;

        float displacement = targetAngle - displayedAngle;
        float springForce = displacement * stiffness;
        float dampingForce = -angularVelocity * (2f * damping * Mathf.Sqrt(stiffness));
        float acceleration = springForce + dampingForce;

        angularVelocity += acceleration * Time.deltaTime;
        displayedAngle += angularVelocity * Time.deltaTime;

        needlePivot.localRotation = Quaternion.Euler(0f, 0f, displayedAngle);

        if (readoutLabel != null)
        {
            readoutLabel.text = !IsDetectingCurrent()
                ? "0.00 mA"
                : $"{currentAmps * 1000f:F2} mA";
            readoutLabel.color = isOverloaded ? overloadColor : normalColor;
        }
    }
}