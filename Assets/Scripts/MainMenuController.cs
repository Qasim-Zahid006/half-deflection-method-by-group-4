using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires the main menu to the remaining practical: Experiment 14.4.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    void Start()
    {
        HideLegacyPracticalButton("Exp14_1Button");
        HideLegacyPracticalButton("Exp14_2Button");
        HideLegacyPracticalButton("Exp14_3Button");
        HideLegacyPracticalButton("Exp14_2Button_Deleted");
        HideLegacyPracticalButton("Exp14_3Button_Deleted");
        HideLegacyPracticalButton("Exp14_4Button_Deleted");

        if (!Wire("PracticalButton", () => Load("HalfDeflectionScene"), "PRACTICAL"))
            Wire("Exp14_4Button", () => Load("HalfDeflectionScene"), "PRACTICAL 14.4");

        Wire("CourseworkButton", () => Load("CourseWork"));
        Wire("QuitButton",     QuitGame);
        ArrangeButton("CourseworkButton", -360f, 100f);
        ArrangeButton("PracticalButton", 0f, 100f);
        ArrangeButton("Exp14_4Button", 0f, 100f);
        ArrangeButton("QuitButton", 360f, 100f);

        if (debugLog) Debug.Log("MainMenuController: 14.4 menu wired");
    }

    bool Wire(string goName, System.Action action, string label = null)
    {
        var go = GameObject.Find(goName);
        if (go == null) { if (debugLog) Debug.LogWarning($"MainMenuController: '{goName}' not found"); return false; }
        go.SetActive(true);

        var btn = go.GetComponent<Button>();
        if (btn == null) { Debug.LogWarning($"MainMenuController: '{goName}' has no Button component"); return false; }
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => action());
        if (!string.IsNullOrWhiteSpace(label))
        {
            var text = go.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (text != null) text.text = label;
        }
        if (debugLog) Debug.Log($"MainMenuController: wired {goName}");
        return true;
    }

    void HideLegacyPracticalButton(string goName)
    {
        var go = GameObject.Find(goName);
        if (go != null) go.SetActive(false);
    }

    void ArrangeButton(string goName, float x, float y)
    {
        var go = GameObject.Find(goName);
        if (go == null) return;
        if (go.TryGetComponent<RectTransform>(out var rect))
            rect.anchoredPosition = new Vector2(x, y);
    }

    void Load(string sceneName) => SceneManager.LoadScene(sceneName);

    void QuitGame()
    {
        if (debugLog) Debug.Log("Quit pressed");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
