using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Theory tabs for Experiment 14.4 only.
/// </summary>
public class CourseworkTabs : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private TMP_Text theoryText;
    private ScrollRect contentScrollRect;
    private int selectedTab;

    private readonly string[] content =
    {
        @"<size=36><b>Definition - Half-Deflection Method (Exp 14.4)</b></size>

A galvanometer is a sensitive current detector. Because its coil has resistance, the galvanometer itself behaves like a small resistor called <b>galvanometer resistance</b>.

<size=28><b>Aim</b></size>
To determine the resistance of the galvanometer, written as <b>R<sub>G</sub></b>, by the half-deflection method.

<size=28><b>Principle</b></size>
With the shunt key open, all current passes through the galvanometer and produces a full deflection <b>theta</b>.

When the low resistance box is connected in parallel with the galvanometer and adjusted until the deflection becomes <b>theta/2</b>, the shunt resistance equals the galvanometer resistance:

<size=34><color=#E74C5E><b>R<sub>G</sub> = R<sub>S</sub></b></color></size>

Here <b>R<sub>S</sub></b> is the low resistance box reading at half deflection.",

        @"<size=36><b>Key Formulas - Exp 14.4</b></size>

<size=28><b>1. Full deflection, K2 open</b></size>
<size=30><color=#E74C5E><b>I<sub>G</sub> = E / (R + R<sub>G</sub> + r)</b></color></size>

E = cell emf, R = HRB resistance, R<sub>G</sub> = galvanometer resistance, r = internal resistance.

<size=28><b>2. Shunt connected, K2 closed</b></size>
<size=30><color=#E74C5E><b>R<sub>p</sub> = (R<sub>G</sub> x R<sub>S</sub>) / (R<sub>G</sub> + R<sub>S</sub>)</b></color></size>

The galvanometer and shunt form a parallel combination.

<size=28><b>3. Half-deflection condition</b></size>
<size=34><color=#E74C5E><b>theta' = theta / 2  =>  R<sub>G</sub> = R<sub>S</sub></b></color></size>

<size=28><b>4. Mean value</b></size>
<size=30><color=#E74C5E><b>Mean R<sub>G</sub> = (R<sub>S1</sub> + R<sub>S2</sub> + R<sub>S3</sub>) / 3</b></color></size>",

        @"<size=36><b>Procedure - Exp 14.4</b></size>

<size=28><b>Apparatus</b></size>
Galvanometer, dry cell, high resistance box (HRB), low resistance box (LRB), keys K1 and K2, and connecting wires.

<size=28><b>Setup</b></size>
<b>1.</b> Connect battery, K1, HRB and galvanometer in series.
<b>2.</b> Connect K2 and LRB as a shunt across the galvanometer only.
<b>3.</b> Keep HRB high at first to protect the galvanometer.

<size=28><b>Run</b></size>
<b>4.</b> Close K1 while K2 remains open.
<b>5.</b> Adjust HRB until the galvanometer gives a clear full deflection <b>theta</b>.
<b>6.</b> Click <b>Take Reading</b> to capture <b>theta</b>.
<b>7.</b> Close K2 and adjust LRB until the deflection becomes <b>theta/2</b>.
<b>8.</b> Click <b>Take Reading</b> again. The simulator records <b>R<sub>G</sub> = R<sub>S</sub></b>.
<b>9.</b> Repeat for three readings and take the mean.",

        @"<size=36><b>Overview - Exp 14.4</b></size>

<size=28><b>What This Proves</b></size>
The resistance of a galvanometer can be found without connecting an ohmmeter directly across its delicate coil.

<size=28><b>Why Half Deflection Works</b></size>
At half deflection, the galvanometer current has been reduced to half its original value. This happens when the shunt branch and galvanometer branch have equal resistance, so current divides equally:

<size=34><color=#E74C5E><b>I<sub>G</sub>' = I<sub>G</sub> / 2</b></color></size>
<size=34><color=#E74C5E><b>R<sub>G</sub> = R<sub>S</sub></b></color></size>

<size=28><b>Good Practice</b></size>
Use a dry cell, start with high HRB resistance, keep K2 open while setting the full deflection, and adjust the LRB carefully near <b>theta/2</b>.

<size=28><b>Result</b></size>
A typical school galvanometer may be around <b>40 ohm</b> to <b>120 ohm</b>. The simulator randomizes the hidden value, so the measurement must be performed each run."
    };

    void Start()
    {
        var textGO = GameObject.Find("TheoryText");
        if (textGO == null) { Debug.LogError("CourseworkTabs: TheoryText not found"); return; }
        theoryText = textGO.GetComponent<TMP_Text>();

        var scrollGO = GameObject.Find("ContentScrollView");
        if (scrollGO != null) contentScrollRect = scrollGO.GetComponent<ScrollRect>();

        WireTab("Tab_Definition", 0);
        WireTab("Tab_Formulas", 1);
        WireTab("Tab_Procedure", 2);
        WireTab("Tab_Overview", 3);
        HideExperimentSelector("ExpTab_1");
        HideExperimentSelector("ExpTab_2");
        HideExperimentSelector("ExpTab_3");
        HideExperimentSelector("ExpTab_4");

        ShowContent();
        if (debugLog) Debug.Log("CourseworkTabs: ready for Experiment 14.4");
    }

    void WireTab(string goName, int tabIndex)
    {
        var go = GameObject.Find(goName);
        if (go == null) return;
        var btn = go.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => { selectedTab = tabIndex; ShowContent(); });
    }

    void HideExperimentSelector(string goName)
    {
        var go = GameObject.Find(goName);
        if (go != null) go.SetActive(false);
    }

    void ShowContent()
    {
        if (theoryText == null) return;
        theoryText.text = content[Mathf.Clamp(selectedTab, 0, content.Length - 1)];
        ResetScrollToTop();
    }

    void ResetScrollToTop() => StartCoroutine(ResetNextFrame());

    IEnumerator ResetNextFrame()
    {
        yield return null;
        if (contentScrollRect != null) contentScrollRect.verticalNormalizedPosition = 1f;
    }

    public void SelectExperiment(int expIndex)
    {
        selectedTab = 0;
        ShowContent();
    }

    public void ShowDefinition() { selectedTab = 0; ShowContent(); }
    public void ShowFormulas() { selectedTab = 1; ShowContent(); }
    public void ShowProcedure() { selectedTab = 2; ShowContent(); }
    public void ShowOverview() { selectedTab = 3; ShowContent(); }
}
