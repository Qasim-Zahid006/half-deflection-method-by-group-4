using UnityEngine;
using TMPro;

public class Battery : MonoBehaviour
{
    [Header("Source Settings")]
    [Tooltip("EMF in volts. Common: 1.5V (cell), 9V, 12V (lab supply)")]
    public float voltage = 1.5f;

    [Tooltip("Internal resistance in ohms")]
    public float internalResistance = 0.1f;

    [Header("Terminal References")]
    [Tooltip("The positive cap GameObject (also acts as terminal)")]
    public Transform positiveTerminal;
    [Tooltip("The negative cap GameObject (also acts as terminal)")]
    public Transform negativeTerminal;

    [Header("Display")]
    [SerializeField] private TMP_Text voltageLabel;

    // Registry — CircuitSimulator finds batteries this way
    private static readonly System.Collections.Generic.List<Battery> allInstances = new();
    public static System.Collections.Generic.IReadOnlyList<Battery> AllInstances => allInstances;

    void OnEnable() => allInstances.Add(this);
    void OnDisable() => allInstances.Remove(this);

    void OnValidate() => UpdateLabel();
    void Start() => UpdateLabel();

    void UpdateLabel()
    {
        if (voltageLabel != null)
            voltageLabel.text = $"{voltage:0.#}V";
    }

    public void SetVoltage(float value)
    {
        voltage = value;
        UpdateLabel();
    }

    public float GetVoltage() => voltage;
    public float GetInternalResistance() => internalResistance;
    public Transform GetPositiveTerminal() => positiveTerminal;
    public Transform GetNegativeTerminal() => negativeTerminal;
}
