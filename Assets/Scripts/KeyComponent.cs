using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple open/close switch for the half-deflection circuit.
/// </summary>
public class KeyComponent : MonoBehaviour
{
    [Header("State")]
    public bool isClosed = false;
    [SerializeField] private string keyName = "Key";

    [Header("UI (optional)")]
    [SerializeField] private Button keyButton;
    [SerializeField] private TMP_Text keyLabel;
    [SerializeField] private Image keyImage;

    [Header("Visual Colors")]
    [SerializeField] private Color closedColor = new Color(0.16f, 0.7f, 0.5f);
    [SerializeField] private Color openColor = new Color(0.9f, 0.3f, 0.36f);

    public System.Action<bool> OnKeyToggled;

    void Start()
    {
        if (keyButton != null)
            keyButton.onClick.AddListener(Toggle);

        RefreshUI();
    }

    public void Toggle()
    {
        isClosed = !isClosed;
        RefreshUI();
        OnKeyToggled?.Invoke(isClosed);
        Debug.Log($"Key {(isClosed ? "CLOSED" : "OPEN")}");
    }

    public void SetClosed(bool closed)
    {
        if (isClosed == closed) return;
        isClosed = closed;
        RefreshUI();
        OnKeyToggled?.Invoke(isClosed);
    }

    public void SetDisplayName(string displayName)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
            keyName = displayName;
        RefreshUI();
    }

    void RefreshUI()
    {
        if (keyLabel != null)
            keyLabel.text = $"{keyName}: {(isClosed ? "CLOSED" : "OPEN")}";

        if (keyImage != null)
            keyImage.color = isClosed ? closedColor : openColor;
    }
}
