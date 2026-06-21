using UnityEngine;
using TMPro;

/// <summary>
/// Fixed-value resistor visual used by the 14.4 HRB/LRB blocks.
/// </summary>
public class Resistor : MonoBehaviour
{
    [Header("Resistance")]
    [Tooltip("Resistance value in ohms")]
    public float resistance = 10f;

    [Header("Terminals (drag the two Cylinder leads)")]
    public Transform terminalA;
    public Transform terminalB;

    [Header("Optional Label")]
    [Tooltip("Floating TMP text that shows the resistor value (optional)")]
    [SerializeField] private TMP_Text valueLabel;

    [Tooltip("Format string: {0} is replaced by the resistance value")]
    [SerializeField] private string labelFormat = "{0:F1} ohm";

    public float Resistance => resistance;

    void Start()
    {
        UpdateLabel();
    }

    void OnValidate()
    {
        if (resistance < 0.001f) resistance = 0.001f;
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (valueLabel != null)
            valueLabel.text = string.Format(labelFormat, resistance);
    }
}
