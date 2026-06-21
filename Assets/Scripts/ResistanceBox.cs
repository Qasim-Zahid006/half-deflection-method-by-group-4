using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// ResistanceBox — variable resistance controlled by a UI Slider.
/// Used for both HRB (High Resistance Box, 0-10k ohm) and LRB (Low Resistance Box, 0-500 ohm) in Experiment 14.4.
/// </summary>
public class ResistanceBox : MonoBehaviour, IScrollHandler
{
    [Header("Range (ohms)")]
    public float minResistance = 0f;
    public float maxResistance = 10000f;

    [Header("UI")]
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text label;
    [SerializeField] private string labelPrefix = "HRB:";
    [SerializeField] private float wheelStep = 0.05f;

    [Header("Runtime")]
    [SerializeField] private float currentResistance;

    public float Resistance => currentResistance;

    void Start()
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            slider.wholeNumbers = false;
            slider.onValueChanged.AddListener(OnSliderChanged);
            OnSliderChanged(slider.value);
        }
        else OnSliderChanged(0.5f);
    }

    void OnSliderChanged(float t)
    {
        currentResistance = Mathf.Lerp(minResistance, maxResistance, t);
        if (label != null) label.text = $"{labelPrefix} {currentResistance:F0} Ω";
    }

    public void SetToMiddle()
    {
        if (slider != null) slider.value = 0.5f;
        else OnSliderChanged(0.5f);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (slider == null) return;
        float direction = Mathf.Sign(eventData.scrollDelta.y);
        slider.value = Mathf.Clamp01(slider.value + direction * wheelStep);
    }
}
