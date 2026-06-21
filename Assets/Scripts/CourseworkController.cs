using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Quiz controller for Experiment 14.4 coursework only.
/// </summary>
public class CourseworkController : MonoBehaviour
{
    [System.Serializable]
    public class Question
    {
        public string questionText;
        public string[] options = new string[4];
        public int correctIndex;
    }

    private readonly List<Question> questionBank = new()
    {
        new Question
        {
            questionText = "In the half-deflection method, the galvanometer resistance R_G equals:",
            options = new[] { "A)  HRB reading R", "B)  LRB/shunt reading R_S at half deflection", "C)  Cell emf E", "D)  Twice the full deflection" },
            correctIndex = 1
        },
        new Question
        {
            questionText = "K2 must remain open while setting the first deflection because:",
            options = new[] { "A)  The galvanometer must receive the unshunted current first", "B)  K2 controls the battery voltage", "C)  LRB works only when K2 is open", "D)  HRB is bypassed by K2" },
            correctIndex = 0
        },
        new Question
        {
            questionText = "The High Resistance Box (HRB) is mainly used to:",
            options = new[] { "A)  Short the cell", "B)  Limit current and protect the galvanometer", "C)  Measure voltage directly", "D)  Replace the galvanometer" },
            correctIndex = 1
        },
        new Question
        {
            questionText = "The Low Resistance Box (LRB), R_S, is connected:",
            options = new[] { "A)  In series with the battery", "B)  In series with HRB", "C)  In parallel with the galvanometer", "D)  Across K1 only" },
            correctIndex = 2
        },
        new Question
        {
            questionText = "At half deflection, the correct relation is:",
            options = new[] { "A)  R_G = R_S", "B)  R_G = 2R_S", "C)  R_G = R + R_S", "D)  R_G = E/I" },
            correctIndex = 0
        },
        new Question
        {
            questionText = "If full deflection is 24 divisions, the half-deflection target is:",
            options = new[] { "A)  6 divisions", "B)  12 divisions", "C)  24 divisions", "D)  48 divisions" },
            correctIndex = 1
        },
        new Question
        {
            questionText = "If the LRB reads 82 ohm at half deflection, then R_G is:",
            options = new[] { "A)  41 ohm", "B)  82 ohm", "C)  164 ohm", "D)  0 ohm" },
            correctIndex = 1
        },
        new Question
        {
            questionText = "Why are three readings taken?",
            options = new[] { "A)  To average random error", "B)  To change the unit", "C)  To discharge the battery", "D)  To make K1 unnecessary" },
            correctIndex = 0
        },
        new Question
        {
            questionText = "A dry cell is preferred because:",
            options = new[] { "A)  It gives safer low voltage for a sensitive galvanometer", "B)  It has infinite voltage", "C)  It removes the need for HRB", "D)  It makes R_G zero" },
            correctIndex = 0
        },
        new Question
        {
            questionText = "After recording one half-deflection reading, the next reading should start by:",
            options = new[] { "A)  Opening K2 and setting a fresh full deflection", "B)  Removing the galvanometer", "C)  Closing only K2", "D)  Setting HRB to zero" },
            correctIndex = 0
        }
    };

    private const int TOTAL_QUESTIONS = 5;
    private const int PASS_SCORE = 3;

    private GameObject quizPromptPanel;
    private GameObject dimBackground;
    private TMP_Text promptTitle;
    private TMP_Text promptText;
    private readonly Button[] optionButtons = new Button[4];
    private readonly TMP_Text[] optionTexts = new TMP_Text[4];
    private TMP_Text feedbackText;

    private readonly List<Question> currentQuiz = new();
    private int currentQuestionIndex;
    private int score;
    private bool answered;
    private bool quizComplete;

    void Start()
    {
        var backGO = GameObject.Find("BackButton");
        if (backGO != null) backGO.GetComponent<Button>().onClick.AddListener(OnBackClicked);

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Debug.LogError("CourseworkController: Canvas not found"); return; }
        Transform canvasT = canvas.transform;

        Transform popupT = canvasT.Find("QuizPromptPanel");
        Transform dimT = canvasT.Find("DimBackground");

        if (popupT != null) quizPromptPanel = popupT.gameObject;
        if (dimT != null) dimBackground = dimT.gameObject;

        if (popupT != null)
        {
            promptTitle = popupT.Find("PromptTitle")?.GetComponent<TMP_Text>();
            promptText = popupT.Find("PromptText")?.GetComponent<TMP_Text>();

            string[] optNames = { "OptionA", "OptionB", "OptionC", "OptionD" };
            for (int i = 0; i < 4; i++)
            {
                Transform t = popupT.Find(optNames[i]);
                if (t == null) continue;
                optionButtons[i] = t.GetComponent<Button>();
                optionTexts[i] = t.GetComponentInChildren<TMP_Text>();
                int idx = i;
                optionButtons[i].onClick.AddListener(() => OnAnswerSelected(idx));
            }

            feedbackText = popupT.Find("FeedbackText")?.GetComponent<TMP_Text>();
        }

        if (quizPromptPanel != null) quizPromptPanel.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    public void OnBackClicked() => StartQuiz();

    void StartQuiz()
    {
        currentQuiz.Clear();
        var pool = new List<Question>(questionBank);
        int count = Mathf.Min(TOTAL_QUESTIONS, pool.Count);
        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, pool.Count);
            currentQuiz.Add(pool[idx]);
            pool.RemoveAt(idx);
        }

        currentQuestionIndex = 0;
        score = 0;
        quizComplete = false;
        if (quizPromptPanel != null) quizPromptPanel.SetActive(true);
        if (dimBackground != null) dimBackground.SetActive(true);
        ShowCurrentQuestion();
    }

    void ShowCurrentQuestion()
    {
        if (currentQuestionIndex >= currentQuiz.Count) { ShowResults(); return; }
        Question q = currentQuiz[currentQuestionIndex];
        if (promptTitle != null) promptTitle.text = $"Question {currentQuestionIndex + 1} of {TOTAL_QUESTIONS}";
        if (promptText != null) promptText.text = q.questionText;

        for (int i = 0; i < 4; i++)
            if (optionTexts[i] != null) optionTexts[i].text = q.options[i];

        answered = false;
        for (int i = 0; i < 4; i++)
        {
            if (optionButtons[i] == null) continue;
            optionButtons[i].gameObject.SetActive(true);
            SetButtonColor(optionButtons[i], new Color(0.96f, 0.96f, 0.96f));
            SetButtonTextColor(optionButtons[i], new Color(0.23f, 0.23f, 0.23f));
            optionButtons[i].interactable = true;
        }
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
    }

    public void OnAnswerSelected(int index)
    {
        if (answered) return;
        if (quizComplete) { OnResultsButtonClicked(index); return; }
        answered = true;

        for (int i = 0; i < 4; i++)
            if (optionButtons[i] != null) optionButtons[i].interactable = false;

        Question q = currentQuiz[currentQuestionIndex];
        bool correct = index == q.correctIndex;
        if (correct)
        {
            score++;
            SetButtonColor(optionButtons[index], new Color(0.16f, 0.7f, 0.5f));
            SetButtonTextColor(optionButtons[index], Color.white);
            if (feedbackText != null) { feedbackText.text = "Correct!"; feedbackText.color = new Color(0.16f, 0.7f, 0.5f); feedbackText.gameObject.SetActive(true); }
        }
        else
        {
            SetButtonColor(optionButtons[index], new Color(0.9f, 0.3f, 0.36f));
            SetButtonTextColor(optionButtons[index], Color.white);
            SetButtonColor(optionButtons[q.correctIndex], new Color(0.16f, 0.7f, 0.5f));
            SetButtonTextColor(optionButtons[q.correctIndex], Color.white);
            if (feedbackText != null) { feedbackText.text = "Incorrect. Correct answer highlighted."; feedbackText.color = new Color(0.9f, 0.3f, 0.36f); feedbackText.gameObject.SetActive(true); }
        }
        StartCoroutine(AdvanceAfterDelay(1.4f));
    }

    IEnumerator AdvanceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentQuestionIndex++;
        ShowCurrentQuestion();
    }

    void ShowResults()
    {
        quizComplete = true;
        answered = false;
        bool passed = score >= PASS_SCORE;
        if (promptTitle != null) promptTitle.text = passed ? "Quiz Complete!" : "Try Again";
        if (promptText != null)
            promptText.text = passed
                ? $"You scored <b>{score}/{TOTAL_QUESTIONS}</b>\n\nYou can now proceed."
                : $"You scored <b>{score}/{TOTAL_QUESTIONS}</b>\n\nYou need at least {PASS_SCORE}/{TOTAL_QUESTIONS} to pass. Review Exp 14.4 and retry.";

        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        if (passed)
        {
            SetupResultButton(0, "", Color.clear, false);
            SetupResultButton(1, "Return to Main Menu", new Color(0.16f, 0.7f, 0.5f), true);
            SetupResultButton(2, "", Color.clear, false);
            SetupResultButton(3, "", Color.clear, false);
        }
        else
        {
            SetupResultButton(0, "", Color.clear, false);
            SetupResultButton(1, "Retry Quiz", new Color(0.9f, 0.3f, 0.36f), true);
            SetupResultButton(2, "", Color.clear, false);
            SetupResultButton(3, "", Color.clear, false);
        }
    }

    void SetupResultButton(int index, string label, Color color, bool visible)
    {
        if (optionButtons[index] == null) return;
        optionButtons[index].gameObject.SetActive(visible);
        if (!visible) return;
        SetButtonColor(optionButtons[index], color);
        SetButtonTextColor(optionButtons[index], Color.white);
        if (optionTexts[index] != null) optionTexts[index].text = label;
        optionButtons[index].interactable = true;
    }

    void OnResultsButtonClicked(int index)
    {
        if (index != 1) return;
        if (score >= PASS_SCORE)
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            quizComplete = false;
            StartQuiz();
        }
    }

    void SetButtonColor(Button btn, Color color)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    void SetButtonTextColor(Button btn, Color color)
    {
        if (btn == null) return;
        var txt = btn.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.color = color;
    }
}
