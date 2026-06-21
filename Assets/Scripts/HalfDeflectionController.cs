using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Experiment 14.4 — Find the resistance of galvanometer by half-deflection method.
///
/// Circuit:
///   K1 closed, K2 open:  I_G = V / (R + R_G)               -> full deflection (theta)
///   K1 closed, K2 closed: parallel R_S || R_G, so I_G drops -> half deflection (theta/2)
///   When deflection is exactly half, R_S = R_G.
///
/// Two keys K1 (main, "KeyButton") and K2 (shunt, "Key2Button") are needed.
/// Two sliders: HRBSlider (R), LRBSlider (R_S).
/// Galvanometer shows deflection theta proportional to I_G.
///
/// The TRUE R_G is randomized at start; the student must measure it.
/// </summary>
public class HalfDeflectionController : MonoBehaviour
{
    [Header("Components")]
    public Battery battery;
    public ResistanceBox hrb;             // High Resistance Box (R)
    public ResistanceBox lrb;             // Low Resistance Box (R_S, shunt)
    public KeyComponent k1;               // main key
    public KeyComponent k2;               // shunt key
    public Galvanometer galvo;            // shows needle deflection

    [Header("Wire Validation")]
    public Transform[] requiredTerminals;

    [Header("Settings")]
    [Tooltip("True resistance of galvanometer in ohms. Student must measure this.")]
    public float trueRG = 75f;

    [Tooltip("If true, trueRG is randomized between min and max at scene start.")]
    public bool randomizeAtStart = true;
    public float randomMin = 40f;
    public float randomMax = 120f;

    [Tooltip("Battery voltage (dry cell ~1.5V typical, but 3V works for clearer deflection)")]
    [Range(1f, 6f)] public float supplyVoltage = 3f;

    [Tooltip("Galvanometer max deflection (divisions). When current = full-scale, deflection = this.")]
    public float fullScaleDivisions = 30f;

    [Tooltip("Current that produces full-scale deflection (amps)")]
    public float fullScaleCurrent = 0.005f; // 5 mA

    public bool addMeasurementNoise = false;
    [Range(0f, 0.05f)] public float noiseAmount = 0f;

    [System.Serializable]
    public class Reading
    {
        public int index;
        public float R, deflectionFull, Rs, deflectionHalf, measuredRG;
    }

    private List<Reading> readings = new List<Reading>();
    private TMP_Text readingsTable, resultLabel, statusLabel;
    private Button takeReadingBtn, resetBtn, backBtn, keyButtonRef, key2ButtonRef, autoWireBtn, clearWiresBtn;

    private float liveIG, liveDeflection;
    private bool circuitActive, wiresValid;
    private float thetaFullCaptured;   // remembered deflection when only K1 is closed
    private const float HalfDeflectionTolerance = 0.75f;
    private Transform k1TerminalA, k1TerminalB, k2TerminalA, k2TerminalB;
    private Transform galvoTerminalA, galvoTerminalB;
    private Transform leftJunction, rightJunction;
    private Transform k1Lever, k2Lever;
    private TextMesh hrbSceneLabel, lrbSceneLabel;

    void Start()
    {
        if (randomizeAtStart) trueRG = Random.Range(randomMin, randomMax);
        if (battery != null) battery.SetVoltage(supplyVoltage);

        SetUIText("TitleText", "Experiment 14.4 - Half-Deflection Method");
        SetUIText("ReadingsTableHeader", "Half-Deflection Readings");
        SetUIText("HRBLabel", "HRB: -- Ω");
        SetUIText("LRBLabel", "LRB: -- Ω");
        HideUnusedMeterUI();

        readingsTable = FindUI<TMP_Text>("ReadingsTable");
        resultLabel = FindUI<TMP_Text>("ResultLabel");
        statusLabel = FindUI<TMP_Text>("StatusLabel");
        takeReadingBtn = FindUI<Button>("TakeReadingButton");
        resetBtn = FindUI<Button>("ResetButton");
        backBtn = FindUI<Button>("BackButton");
        keyButtonRef = FindUI<Button>("KeyButton");
        key2ButtonRef = FindUI<Button>("Key2Button");
        autoWireBtn = FindUI<Button>("AutoWireButton");
        clearWiresBtn = FindUI<Button>("ClearWiresButton");
        if (galvo == null) galvo = FindAnyObjectByType<Galvanometer>();
        CreateLabBackground();
        CreateSwitchVisuals();
        CacheGalvanometerTerminals();
        ArrangeSceneObjects();
        CreateRoutingJunctions();
        EnsureWireControlButtons();
        StyleHalfDeflectionUI();
        CreateResistanceSceneLabels();

        if (takeReadingBtn != null) takeReadingBtn.onClick.AddListener(AddReading);
        if (autoWireBtn != null) autoWireBtn.onClick.AddListener(AutoWireCircuit);
        if (clearWiresBtn != null) clearWiresBtn.onClick.AddListener(ClearAllWires);
        if (resetBtn != null) resetBtn.onClick.AddListener(ResetAll);
        if (backBtn != null) backBtn.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        if (k1 != null) k1.SetDisplayName("K1");
        if (k2 != null) k2.SetDisplayName("K2");

        ResetAll();
        Debug.Log($"HalfDeflection: trueRG = {trueRG:F2} ohm (hidden)");
    }

    void Update()
    {
        SolveAndDisplay();
        RefreshKeyButtonLabels();
        RefreshResistanceSceneLabels();
    }

    bool IsCircuitWired()
    {
        var wires = FindObjectsByType<ConnectingWire>();
        if (wires.Length == 0) return false;
        HashSet<Transform> touched = new HashSet<Transform>();
        foreach (var w in wires)
        {
            if (w == null) continue;
            if (w.startTerminal != null) touched.Add(w.startTerminal);
            if (w.endTerminal != null) touched.Add(w.endTerminal);
        }
        foreach (var t in GetRequiredTerminals())
            if (t != null && !touched.Contains(t)) return false;
        return true;
    }

    IEnumerable<Transform> GetRequiredTerminals()
    {
        if (requiredTerminals != null && requiredTerminals.Length > 0)
        {
            foreach (var t in requiredTerminals) yield return t;
            yield break;
        }

        if (battery != null)
        {
            yield return battery.GetPositiveTerminal();
            yield return battery.GetNegativeTerminal();
        }

        foreach (var resistor in FindObjectsByType<Resistor>())
        {
            if (resistor == null) continue;
            if (!resistor.name.Contains("HRB_Visual") && !resistor.name.Contains("LRB_Visual")) continue;
            yield return resistor.terminalA;
            yield return resistor.terminalB;
        }

        yield return galvoTerminalA;
        yield return galvoTerminalB;
        yield return k1TerminalA;
        yield return k1TerminalB;
        yield return k2TerminalA;
        yield return k2TerminalB;
    }

    void SolveAndDisplay()
    {
        wiresValid = IsCircuitWired();
        if (keyButtonRef != null) keyButtonRef.interactable = wiresValid;
        if (key2ButtonRef != null) key2ButtonRef.interactable = wiresValid;

        if (!wiresValid)
        {
            if (k1 != null && k1.isClosed) k1.SetClosed(false);
            if (k2 != null && k2.isClosed) k2.SetClosed(false);
            liveIG = liveDeflection = 0f;
            UpdateStatus("Wire battery, K1, HRB, galvanometer, K2 and LRB terminals to enable the keys");
            UpdateTakeReadingButton("Wire Circuit", false);
            PushToMeters(); return;
        }

        bool K1closed = (k1 != null) ? k1.isClosed : false;
        bool K2closed = (k2 != null) ? k2.isClosed : false;
        circuitActive = K1closed;

        if (!K1closed)
        {
            liveIG = liveDeflection = 0f;
            UpdateStatus("Close K1 to start. Keep K2 OPEN for the first full deflection.");
            UpdateTakeReadingButton("Capture Full Deflection", false);
            PushToMeters(); return;
        }

        float V = battery.GetVoltage();
        float R = hrb != null ? hrb.Resistance : 0f;
        float Rs = lrb != null ? lrb.Resistance : 0f;
        float Rint = battery != null ? battery.GetInternalResistance() : 0f;

        if (!K2closed)
        {
            // Only main loop: V = I_G * (R + R_G + Rint)
            float Rtotal = R + trueRG + Rint;
            liveIG = (Rtotal > 0.0001f) ? V / Rtotal : 0f;
        }
        else
        {
            // K2 closed: shunt R_S || R_G across the galvanometer
            if (Rs < 0.0001f)
            {
                UpdateStatus("LRB ~ 0 ohm - shunt is a short. Increase LRB before recording.");
                UpdateTakeReadingButton("Record Half Reading", false);
                liveIG = 0;
                PushToMeters();
                return;
            }
            float parallel = (trueRG * Rs) / (trueRG + Rs);
            float Rtotal = R + parallel + Rint;
            float Itotal = (Rtotal > 0.0001f) ? V / Rtotal : 0f;
            // Current splits: I_G = I_total * R_S / (R_S + R_G)
            liveIG = Itotal * Rs / (Rs + trueRG);
        }

        if (addMeasurementNoise) liveIG *= 1f + Random.Range(-noiseAmount, noiseAmount);

        liveDeflection = (liveIG / fullScaleCurrent) * fullScaleDivisions;
        liveDeflection = Mathf.Clamp(liveDeflection, -fullScaleDivisions, fullScaleDivisions);

        string statusMsg;
        if (!K2closed)
        {
            if (thetaFullCaptured > 0f)
            {
                statusMsg = $"Full deflection captured: theta = {thetaFullCaptured:F1} div. Close K2, then adjust LRB to {thetaFullCaptured / 2f:F1} div.";
                UpdateTakeReadingButton("Record Half Reading", false);
            }
            else
            {
                statusMsg = $"K1 closed, K2 OPEN. Adjust HRB for a clear deflection, then capture theta. theta = {liveDeflection:F1} div";
                bool canCapture = liveDeflection > 1f && liveDeflection < fullScaleDivisions - 0.25f;
                UpdateTakeReadingButton("Capture Full Deflection", canCapture);
            }
        }
        else
        {
            string hint = "";
            if (thetaFullCaptured > 0)
            {
                float half = thetaFullCaptured / 2f;
                float diff = liveDeflection - half;
                if (Mathf.Abs(diff) <= HalfDeflectionTolerance) hint = "  HALF DEFLECTION REACHED: record now";
                else if (diff > 0) hint = "  (decrease LRB)";
                else hint = "  (increase LRB)";
                UpdateTakeReadingButton("Record Half Reading", Mathf.Abs(diff) <= HalfDeflectionTolerance);
            }
            else
            {
                hint = "  Open K2 first and capture full deflection.";
                UpdateTakeReadingButton("Capture Full Deflection", false);
            }
            statusMsg = $"K1 and K2 closed. Adjust LRB until theta = theta/2. theta = {liveDeflection:F1} div{hint}";
        }
        UpdateStatus(statusMsg);
        PushToMeters();
    }

    void PushToMeters()
    {
        if (galvo != null) galvo.SetCurrent(liveIG);
        UpdateSwitchVisual(k1Lever, k1 != null && k1.isClosed);
        UpdateSwitchVisual(k2Lever, k2 != null && k2.isClosed);
    }

    /// <summary>
    /// Take Reading workflow:
    ///   1. With only K1 closed: click to capture theta_full
    ///   2. Close K2, adjust LRB so theta = theta_full/2
    ///   3. Click again to record (R, theta_full, R_S, theta/2). R_G = R_S at that point.
    /// </summary>
    public void AddReading()
    {
        if (!wiresValid) { UpdateStatus("Wires not connected"); return; }
        if (k1 == null || !k1.isClosed) { UpdateStatus("Close K1 first"); return; }

        bool K2closed = (k2 != null) ? k2.isClosed : false;

        if (!K2closed)
        {
            if (liveDeflection <= 1f)
            {
                UpdateStatus("Deflection is too small. Adjust HRB until the galvanometer moves clearly, then capture theta.");
                return;
            }
            if (liveDeflection >= fullScaleDivisions - 0.25f)
            {
                UpdateStatus("Deflection is at full scale. Increase HRB slightly so the needle is clear but not pinned, then capture theta.");
                return;
            }
            thetaFullCaptured = liveDeflection;
            UpdateStatus($"Captured theta_full = {thetaFullCaptured:F1} div. Now close K2 and adjust LRB to half.");
            return;
        }

        if (thetaFullCaptured <= 0f)
        {
            UpdateStatus("Open K2 first, capture full deflection, then close K2.");
            return;
        }

        float targetHalf = thetaFullCaptured / 2f;
        if (Mathf.Abs(liveDeflection - targetHalf) > HalfDeflectionTolerance)
        {
            UpdateStatus($"Not ready to record. Adjust LRB until theta is about {targetHalf:F1} div; current theta = {liveDeflection:F1} div.");
            return;
        }

        float rs = lrb != null ? lrb.Resistance : 0f;
        if (rs <= 0.1f)
        {
            UpdateStatus("LRB is too close to 0 ohm. Increase LRB until half deflection is reached.");
            return;
        }

        Reading r = new Reading
        {
            index = readings.Count + 1,
            R = hrb != null ? hrb.Resistance : 0f,
            deflectionFull = thetaFullCaptured,
            Rs = rs,
            deflectionHalf = targetHalf,
            measuredRG = rs  // by half-deflection method, R_G = R_S
        };
        readings.Add(r);
        RefreshTable();
        thetaFullCaptured = 0f;       // reset for next pair of readings
        UpdateStatus("Reading recorded. Open K2 and start a new pair with different R.");
    }

    void RefreshTable()
    {
        if (readingsTable == null) return;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<mspace=0.55em><b><color=#FFFFFF>No  R     theta  Rs    theta/2  Rg=Rs</color></b></mspace>");
        sb.AppendLine("<mspace=0.55em><color=#FFFFFF>-----------------------------------------</color></mspace>");
        foreach (var r in readings)
            sb.AppendLine($"<mspace=0.55em><b><color=#FFFFFF>{r.index,-3} {r.R,-5:F0} {r.deflectionFull,5:F1} {r.Rs,-5:F1} {r.deflectionHalf,7:F1}  {r.measuredRG,-5:F1}</color></b></mspace>");
        readingsTable.text = sb.ToString();

        if (resultLabel != null && readings.Count > 0)
        {
            float sum = 0;
            foreach (var r in readings) sum += r.measuredRG;
            float meanRG = sum / readings.Count;
            resultLabel.text = $"<color=#FFFFFF><b>Mean R_G = {meanRG:F2} ohm</b>\n(from {readings.Count} reading{(readings.Count == 1 ? "" : "s")})</color>";
        }
    }

    public void ResetAll()
    {
        readings.Clear();
        thetaFullCaptured = 0f;
        if (readingsTable != null) readingsTable.text = "<b>No readings yet.</b>\n\n<b>When to add readings</b>\n1. K1 CLOSED, K2 OPEN: adjust HRB and capture theta.\n2. K1 CLOSED, K2 CLOSED: adjust LRB to theta/2.\n3. Click Record Half Reading.";
        if (resultLabel != null) resultLabel.text = "";
        if (k1 != null) k1.SetClosed(false);
        if (k2 != null) k2.SetClosed(false);
        UpdateStatus("Ready");
        UpdateTakeReadingButton("Capture Full Deflection", false);
    }

    void UpdateStatus(string msg) { if (statusLabel != null) statusLabel.text = $"<b>{msg}</b>"; }

    void HideUnusedMeterUI()
    {
        SetUIActive("PowerSupplyKnob", false);
        SetUIActive("PowerSupplyLabel", false);
        SetUIActive("VoltLabel", false);
        SetUIActive("VoltReadout", false);
        SetUIActive("AmpLabel", false);
        SetUIActive("AmpReadout", false);
    }

    void SetUIText(string objectName, string text)
    {
        var uiText = FindUI<TMP_Text>(objectName);
        if (uiText != null) uiText.text = text;
    }

    void SetUIActive(string objectName, bool active)
    {
        var go = GameObject.Find(objectName);
        if (go != null) go.SetActive(active);
    }

    void EnsureWireControlButtons()
    {
        Button source = takeReadingBtn != null ? takeReadingBtn : resetBtn;
        if (source == null) return;

        if (autoWireBtn == null)
            autoWireBtn = CreateControlButton(source, "AutoWireButton", "Auto Wire");

        if (clearWiresBtn == null)
            clearWiresBtn = CreateControlButton(source, "ClearWiresButton", "Clear Wires");

        PositionWireControlButton(autoWireBtn, source, 100f);
        PositionWireControlButton(clearWiresBtn, source, 50f);
    }

    Button CreateControlButton(Button source, string objectName, string label)
    {
        var clone = Instantiate(source.gameObject, source.transform.parent);
        clone.name = objectName;

        var button = clone.GetComponent<Button>();
        if (button == null) return null;

        button.onClick.RemoveAllListeners();
        SetButtonText(button, label);
        return button;
    }

    void PositionWireControlButton(Button button, Button source, float yOffset)
    {
        if (button == null || source == null) return;

        var rect = button.GetComponent<RectTransform>();
        if (rect == null) return;

        rect.SetSiblingIndex(Mathf.Max(0, source.transform.GetSiblingIndex()));
        if (source.TryGetComponent<RectTransform>(out var sourceRect))
        {
            rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, yOffset);
            rect.sizeDelta = new Vector2(sourceRect.sizeDelta.x, 40f);
        }
    }

    public void AutoWireCircuit()
    {
        var wireManager = FindAnyObjectByType<WireManager>();
        if (wireManager == null)
        {
            UpdateStatus("WireManager not found.");
            return;
        }

        if (!TryGet14_4Terminals(out var hrbA, out var hrbB, out var lrbA, out var lrbB))
        {
            UpdateStatus("Could not find HRB/LRB terminals for auto wiring.");
            return;
        }

        wireManager.ClearAllWires();

        var batteryPositive = battery != null ? battery.GetPositiveTerminal() : null;
        var batteryNegative = battery != null ? battery.GetNegativeTerminal() : null;
        if (batteryPositive == null || batteryNegative == null || galvoTerminalA == null || galvoTerminalB == null)
        {
            UpdateStatus("Could not find all battery/galvanometer terminals for auto wiring.");
            return;
        }

        if (k1 != null) k1.SetClosed(false);
        if (k2 != null) k2.SetClosed(false);
        thetaFullCaptured = 0f;

        var galvoLeftDrop = GetOrCreateRoutePoint("Route_GalvoLeftDrop", new Vector3(-1.2f, 0.78f, 0f));
        var galvoRightDrop = GetOrCreateRoutePoint("Route_GalvoRightDrop", new Vector3(1.35f, 0.78f, 0f));
        var shuntLeft = GetOrCreateRoutePoint("Route_ShuntLeft", new Vector3(-1.2f, 0.12f, 0f));
        var shuntRight = GetOrCreateRoutePoint("Route_ShuntRight", new Vector3(1.55f, 0.12f, 0f));
        var mainLeft = GetOrCreateRoutePoint("Route_MainLeft", new Vector3(-2.25f, -1.05f, 0f));
        var mainRight = GetOrCreateRoutePoint("Route_MainRight", new Vector3(1.55f, -1.05f, 0f));
        var batteryTop = GetOrCreateRoutePoint("Route_BatteryTop", new Vector3(2.15f, -0.58f, 0f));

        CreateWirePath(wireManager, leftJunction, galvoLeftDrop, galvoTerminalA);
        CreateWirePath(wireManager, galvoTerminalB, galvoRightDrop, rightJunction);

        CreateWirePath(wireManager, leftJunction, shuntLeft, k2TerminalA);
        CreateWirePath(wireManager, k2TerminalB, lrbA);
        CreateWirePath(wireManager, lrbB, shuntRight, rightJunction);

        CreateWirePath(wireManager, leftJunction, mainLeft, hrbA);
        CreateWirePath(wireManager, hrbB, k1TerminalA);
        CreateWirePath(wireManager, k1TerminalB, mainRight, batteryPositive);
        CreateWirePath(wireManager, batteryNegative, batteryTop, rightJunction);

        wiresValid = IsCircuitWired();
        UpdateStatus("Circuit wired through the galvanometer. Close K1, keep K2 open, and adjust HRB before capturing theta.");
    }

    public void ClearAllWires()
    {
        var wireManager = FindAnyObjectByType<WireManager>();
        if (wireManager == null)
        {
            UpdateStatus("WireManager not found.");
            return;
        }

        wireManager.ClearAllWires();
        wiresValid = false;
        thetaFullCaptured = 0f;
        liveIG = liveDeflection = 0f;

        if (k1 != null) k1.SetClosed(false);
        if (k2 != null) k2.SetClosed(false);

        PushToMeters();
        UpdateStatus("All wires cleared. Rewire manually or click Auto Wire.");
        UpdateTakeReadingButton("Wire Circuit", false);
    }

    void CreateWirePath(WireManager wireManager, params Transform[] points)
    {
        if (wireManager == null || points == null || points.Length < 2) return;

        for (int i = 0; i < points.Length - 1; i++)
            wireManager.CreateWire(points[i], points[i + 1]);
    }

    bool TryGet14_4Terminals(out Transform hrbA, out Transform hrbB, out Transform lrbA, out Transform lrbB)
    {
        hrbA = hrbB = lrbA = lrbB = null;
        foreach (var resistor in FindObjectsByType<Resistor>())
        {
            if (resistor == null) continue;
            if (resistor.name.Contains("HRB_Visual"))
            {
                hrbA = resistor.terminalA;
                hrbB = resistor.terminalB;
            }
            else if (resistor.name.Contains("LRB_Visual"))
            {
                lrbA = resistor.terminalA;
                lrbB = resistor.terminalB;
            }
        }

        return hrbA != null && hrbB != null && lrbA != null && lrbB != null;
    }

    void ArrangeSceneObjects()
    {
        if (battery != null)
        {
            battery.transform.position = new Vector3(2.15f, -1.05f, 0f);
            battery.transform.localScale = Vector3.one * 0.2f;
        }

        if (galvo != null)
        {
            galvo.transform.position = new Vector3(0.15f, 1.42f, 0f);
            galvo.transform.localScale = Vector3.one * 0.8f;
        }

        foreach (var resistor in FindObjectsByType<Resistor>())
        {
            if (resistor == null) continue;
            if (resistor.name.Contains("HRB_Visual"))
                SetVisualResistorPose(resistor, new Vector3(-1.95f, -1.05f, 0f));
            else if (resistor.name.Contains("LRB_Visual"))
                SetVisualResistorPose(resistor, new Vector3(0.95f, 0.08f, 0f));
        }
    }

    void SetVisualResistorPose(Resistor resistor, Vector3 position)
    {
        resistor.transform.position = position;
        resistor.transform.localScale = Vector3.one * 44f;
    }

    void CreateResistanceSceneLabels()
    {
        hrbSceneLabel = GetOrCreateSceneLabel("HRB_SceneResistanceLabel", new Vector3(-1.92f, -0.72f, -0.1f), 0.038f);
        lrbSceneLabel = GetOrCreateSceneLabel("LRB_SceneResistanceLabel", new Vector3(0.95f, 0.34f, -0.1f), 0.038f);
        RefreshResistanceSceneLabels();
    }

    TextMesh GetOrCreateSceneLabel(string name, Vector3 position, float characterSize)
    {
        var existing = GameObject.Find(name);
        var labelGO = existing != null ? existing : new GameObject(name);
        labelGO.transform.position = position;

        var text = labelGO.GetComponent<TextMesh>();
        if (text == null) text = labelGO.AddComponent<TextMesh>();
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = characterSize;
        text.fontSize = 28;
        text.color = Color.white;
        return text;
    }

    void RefreshResistanceSceneLabels()
    {
        if (hrbSceneLabel != null)
            hrbSceneLabel.text = $"HRB: {(hrb != null ? hrb.Resistance : 0f):F0} Ω";

        if (lrbSceneLabel != null)
            lrbSceneLabel.text = $"LRB: {(lrb != null ? lrb.Resistance : 0f):F0} Ω";
    }

    void CreateRoutingJunctions()
    {
        leftJunction = GetOrCreateRoutePoint("LeftCircuitJunction", new Vector3(-1.65f, 0.82f, 0f));
        rightJunction = GetOrCreateRoutePoint("RightCircuitJunction", new Vector3(1.85f, 0.82f, 0f));
    }

    Transform GetOrCreateRoutePoint(string name, Vector3 position)
    {
        var existing = GameObject.Find(name);
        var route = existing != null ? existing : new GameObject(name);
        route.transform.position = position;
        return route.transform;
    }

    void CacheGalvanometerTerminals()
    {
        if (galvo == null) return;

        var root = galvo.transform;
        galvoTerminalA = root.Find("TerminalLeft");
        galvoTerminalB = root.Find("TerminalRight");

        PreparePrefabTerminal(galvoTerminalA);
        PreparePrefabTerminal(galvoTerminalB);
    }

    void PreparePrefabTerminal(Transform terminal)
    {
        if (terminal == null) return;
        terminal.tag = "TerminalAnchor";
        var collider = terminal.GetComponent<Collider>();
        if (collider == null) collider = terminal.gameObject.AddComponent<SphereCollider>();
        if (collider is SphereCollider sphere) sphere.radius = Mathf.Max(sphere.radius, 0.35f);
    }

    void CreateSwitchVisuals()
    {
        if (GameObject.Find("K1_VisualSwitch") == null)
        {
            var k1Switch = CreateSwitchVisual("K1_VisualSwitch", "K1", new Vector3(-2.2f, 0.85f, 0f));
            k1TerminalA = k1Switch.terminalA;
            k1TerminalB = k1Switch.terminalB;
            k1Lever = k1Switch.lever;
        }
        else
        {
            CacheSwitchVisual("K1_VisualSwitch", out k1TerminalA, out k1TerminalB, out k1Lever);
        }
        SetSwitchPose("K1_VisualSwitch", new Vector3(-0.55f, -1.05f, 0f));

        if (GameObject.Find("K2_VisualSwitch") == null)
        {
            var k2Switch = CreateSwitchVisual("K2_VisualSwitch", "K2", new Vector3(-0.55f, 0.2f, 0f));
            k2TerminalA = k2Switch.terminalA;
            k2TerminalB = k2Switch.terminalB;
            k2Lever = k2Switch.lever;
        }
        else
        {
            CacheSwitchVisual("K2_VisualSwitch", out k2TerminalA, out k2TerminalB, out k2Lever);
        }
        SetSwitchPose("K2_VisualSwitch", new Vector3(-0.75f, 0.08f, 0f));
    }

    void SetSwitchPose(string objectName, Vector3 position)
    {
        var root = GameObject.Find(objectName);
        if (root != null) root.transform.position = position;
    }

    (Transform terminalA, Transform terminalB, Transform lever) CreateSwitchVisual(string objectName, string label, Vector3 position)
    {
        var root = new GameObject(objectName);
        root.transform.position = position;

        var baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBlock.name = "Base";
        baseBlock.transform.SetParent(root.transform, false);
        baseBlock.transform.localPosition = Vector3.zero;
        baseBlock.transform.localScale = new Vector3(1.05f, 0.07f, 0.28f);
        SetRendererColor(baseBlock, new Color(0.18f, 0.2f, 0.22f));

        var terminalA = CreateSwitchTerminal(root.transform, "TerminalA", new Vector3(-0.46f, 0.1f, 0f));
        var terminalB = CreateSwitchTerminal(root.transform, "TerminalB", new Vector3(0.46f, 0.1f, 0f));

        var lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lever.name = "Lever";
        lever.transform.SetParent(root.transform, false);
        lever.transform.localScale = new Vector3(0.56f, 0.045f, 0.07f);
        SetRendererColor(lever, new Color(0.95f, 0.8f, 0.22f));

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(root.transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 0.24f, -0.08f);
        var text = labelGO.AddComponent<TextMesh>();
        text.text = label;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = 0.055f;
        text.fontSize = 32;
        text.color = Color.white;

        UpdateSwitchVisual(lever.transform, false);
        return (terminalA.transform, terminalB.transform, lever.transform);
    }

    GameObject CreateSwitchTerminal(Transform parent, string name, Vector3 localPosition)
    {
        var terminal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        terminal.name = name;
        terminal.tag = "TerminalAnchor";
        terminal.transform.SetParent(parent, false);
        terminal.transform.localPosition = localPosition;
        terminal.transform.localScale = Vector3.one * 0.18f;
        SetRendererColor(terminal, new Color(0.95f, 0.75f, 0.18f));
        var collider = terminal.GetComponent<SphereCollider>();
        if (collider != null) collider.radius = 0.9f;
        return terminal;
    }

    void CacheSwitchVisual(string objectName, out Transform terminalA, out Transform terminalB, out Transform lever)
    {
        var root = GameObject.Find(objectName);
        terminalA = root != null ? root.transform.Find("TerminalA") : null;
        terminalB = root != null ? root.transform.Find("TerminalB") : null;
        lever = root != null ? root.transform.Find("Lever") : null;
    }

    void UpdateSwitchVisual(Transform lever, bool closed)
    {
        if (lever == null) return;
        lever.localScale = closed ? new Vector3(0.92f, 0.045f, 0.07f) : new Vector3(0.56f, 0.045f, 0.07f);
        lever.localPosition = closed ? new Vector3(0f, 0.145f, 0f) : new Vector3(-0.2f, 0.22f, 0f);
        lever.localRotation = Quaternion.Euler(0f, 0f, closed ? 0f : -32f);
        SetRendererColor(lever.gameObject, closed ? new Color(0.16f, 0.7f, 0.5f) : new Color(0.95f, 0.8f, 0.22f));
    }

    void SetRendererColor(GameObject go, Color color)
    {
        var renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;
        renderer.material.color = color;
    }

    void CreateLabBackground()
    {
        const string rootName = "GeneratedLabBackground";
        if (GameObject.Find(rootName) != null) return;

        var root = new GameObject(rootName);
        root.transform.position = Vector3.zero;

        CreateBackdropBlock(root.transform, "BackWall", new Vector3(0f, 0.55f, 1.35f), new Vector3(9.4f, 4.45f, 0.08f), new Color(0.72f, 0.82f, 0.86f));
        CreateBackdropBlock(root.transform, "LowerWallBand", new Vector3(0f, -0.58f, 1.26f), new Vector3(9.4f, 0.38f, 0.08f), new Color(0.55f, 0.66f, 0.68f));
        CreateBackdropBlock(root.transform, "LabBenchTop", new Vector3(0.05f, -1.5f, 0.82f), new Vector3(7.45f, 0.18f, 0.7f), new Color(0.34f, 0.31f, 0.27f));
        CreateBackdropBlock(root.transform, "LabBenchFront", new Vector3(0.05f, -1.82f, 1.1f), new Vector3(7.45f, 0.55f, 0.12f), new Color(0.42f, 0.47f, 0.48f));
        CreateBackdropBlock(root.transform, "Floor", new Vector3(0f, -2.25f, 1.42f), new Vector3(9.4f, 1.05f, 0.08f), new Color(0.45f, 0.43f, 0.4f));

        for (int i = -5; i <= 5; i++)
            CreateBackdropBlock(root.transform, $"FloorTileLineX_{i}", new Vector3(i * 0.9f, -2.25f, 1.18f), new Vector3(0.018f, 1f, 0.04f), new Color(0.34f, 0.34f, 0.33f));

        for (int i = 0; i < 5; i++)
            CreateBackdropBlock(root.transform, $"FloorTileLineY_{i}", new Vector3(0f, -1.85f - i * 0.22f, 1.17f), new Vector3(9.25f, 0.014f, 0.04f), new Color(0.34f, 0.34f, 0.33f));

        CreateBackdropBlock(root.transform, "WindowFrame", new Vector3(-2.25f, 1.45f, 1.15f), new Vector3(1.05f, 0.72f, 0.08f), new Color(0.85f, 0.89f, 0.9f));
        CreateBackdropBlock(root.transform, "WindowGlass", new Vector3(-2.25f, 1.45f, 1.08f), new Vector3(0.88f, 0.56f, 0.05f), new Color(0.55f, 0.72f, 0.84f));
        CreateBackdropBlock(root.transform, "WindowMidVertical", new Vector3(-2.25f, 1.45f, 1.03f), new Vector3(0.03f, 0.58f, 0.04f), new Color(0.9f, 0.94f, 0.95f));
        CreateBackdropBlock(root.transform, "WindowMidHorizontal", new Vector3(-2.25f, 1.45f, 1.03f), new Vector3(0.9f, 0.03f, 0.04f), new Color(0.9f, 0.94f, 0.95f));

        CreateBackdropBlock(root.transform, "Shelf", new Vector3(2.25f, 1.35f, 1.05f), new Vector3(1.65f, 0.08f, 0.12f), new Color(0.32f, 0.28f, 0.23f));
        CreateBackdropBlock(root.transform, "ShelfSupportLeft", new Vector3(1.5f, 1.15f, 1.05f), new Vector3(0.05f, 0.35f, 0.1f), new Color(0.32f, 0.28f, 0.23f));
        CreateBackdropBlock(root.transform, "ShelfSupportRight", new Vector3(3.0f, 1.15f, 1.05f), new Vector3(0.05f, 0.35f, 0.1f), new Color(0.32f, 0.28f, 0.23f));
        CreateLabBottle(root.transform, "BlueBottle", new Vector3(1.75f, 1.52f, 0.95f), new Color(0.18f, 0.46f, 0.72f));
        CreateLabBottle(root.transform, "GreenBottle", new Vector3(2.18f, 1.52f, 0.95f), new Color(0.2f, 0.62f, 0.38f));
        CreateLabBottle(root.transform, "AmberBottle", new Vector3(2.62f, 1.52f, 0.95f), new Color(0.75f, 0.48f, 0.18f));

        CreateBackdropBlock(root.transform, "SafetyPoster", new Vector3(-2.65f, 0.4f, 1.08f), new Vector3(0.62f, 0.7f, 0.05f), new Color(0.92f, 0.9f, 0.78f));
        CreateBackdropText(root.transform, "SafetyPosterText", "LAB", new Vector3(-2.65f, 0.43f, 0.95f), 0.045f, new Color(0.22f, 0.25f, 0.28f));
        CreateBackdropBlock(root.transform, "FormulaPoster", new Vector3(2.9f, 0.28f, 1.08f), new Vector3(0.72f, 0.58f, 0.05f), new Color(0.86f, 0.92f, 0.86f));
        CreateBackdropText(root.transform, "FormulaPosterText", "R = V / I", new Vector3(2.9f, 0.3f, 0.95f), 0.045f, new Color(0.18f, 0.32f, 0.23f));
    }

    GameObject CreateBackdropBlock(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
    {
        var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.position = position;
        block.transform.localScale = scale;
        SetRendererColor(block, color);
        var collider = block.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        return block;
    }

    void CreateLabBottle(Transform parent, string name, Vector3 position, Color color)
    {
        var body = CreateBackdropBlock(parent, name, position, new Vector3(0.18f, 0.34f, 0.08f), color);
        CreateBackdropBlock(body.transform, "Cap", position + new Vector3(0f, 0.21f, -0.02f), new Vector3(0.12f, 0.08f, 0.08f), new Color(0.88f, 0.88f, 0.82f));
        CreateBackdropBlock(body.transform, "Label", position + new Vector3(0f, -0.02f, -0.06f), new Vector3(0.14f, 0.11f, 0.03f), new Color(0.94f, 0.94f, 0.87f));
    }

    void CreateBackdropText(Transform parent, string name, string content, Vector3 position, float characterSize, Color color)
    {
        var textGO = new GameObject(name);
        textGO.transform.SetParent(parent, false);
        textGO.transform.position = position;
        var text = textGO.AddComponent<TextMesh>();
        text.text = content;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.characterSize = characterSize;
        text.fontSize = 32;
        text.color = color;
    }

    void StyleHalfDeflectionUI()
    {
        StylePanelText("RightTopPanel");
        StyleRightBottomPanelText();
        StylePanelText("LeftPanel");
        NormalizeLeftPanelButtons();
        SetButtonText(keyButtonRef, "K1: OPEN");
        SetButtonText(key2ButtonRef, "K2: OPEN");
        SetButtonText(autoWireBtn, "Auto Wire");
        SetButtonText(clearWiresBtn, "Clear Wires");
    }

    void NormalizeLeftPanelButtons()
    {
        var panel = GameObject.Find("LeftPanel");
        if (panel == null) return;

        foreach (var button in panel.GetComponentsInChildren<Button>(true))
        {
            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 42f);

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null) continue;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 20f;
            label.alignment = TextAlignmentOptions.Center;
            label.margin = new Vector4(6f, 0f, 6f, 0f);
        }
    }

    void StylePanelText(string panelName)
    {
        var panel = GameObject.Find(panelName);
        if (panel == null) return;
        foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            text.fontStyle |= FontStyles.Bold;
            if (text.name == "ReadingsTable" || text.gameObject.name == "ReadingsTable")
                text.fontSize = Mathf.Min(text.fontSize + 2f, 30f);
            else if (text.name == "StatusLabel" || text.gameObject.name == "StatusLabel")
                text.fontSize = Mathf.Min(text.fontSize + 2f, 24f);
            else
                text.fontSize += 5f;
        }
    }

    void StyleRightBottomPanelText()
    {
        var panel = GameObject.Find("RightBottomePanel");
        if (panel == null) return;

        foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            text.fontStyle |= FontStyles.Bold;
            text.color = Color.black;
            text.overflowMode = TextOverflowModes.Truncate;

            if (text.name == "ReadingsTable" || text.gameObject.name == "ReadingsTable")
            {
                // Table needs to be single-line per row, monospace, no auto-shrink
                text.enableAutoSizing = false;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.fontSize = 16f;
                text.lineSpacing = -2f;
                text.paragraphSpacing = 4f;
                text.alignment = TextAlignmentOptions.TopLeft;
            }
            else if (text.name == "ResultLabel" || text.gameObject.name == "ResultLabel")
            {
                text.enableAutoSizing = true;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.fontSize = 22f;
                text.fontSizeMin = 16f;
                text.fontSizeMax = 26f;
                text.lineSpacing = -2f;
            }
            else
            {
                text.enableAutoSizing = true;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.fontSize = 22f;
                text.fontSizeMin = 16f;
                text.fontSizeMax = 26f;
            }
        }
    }
    void RefreshKeyButtonLabels()
    {
        SetButtonText(keyButtonRef, $"K1: {((k1 != null && k1.isClosed) ? "CLOSED" : "OPEN")}");
        SetButtonText(key2ButtonRef, $"K2: {((k2 != null && k2.isClosed) ? "CLOSED" : "OPEN")}");
    }

    void UpdateTakeReadingButton(string text, bool interactable)
    {
        if (takeReadingBtn == null) return;
        takeReadingBtn.interactable = interactable;
        SetButtonText(takeReadingBtn, text);
    }

    void SetButtonText(Button button, string text)
    {
        if (button == null) return;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        label.text = text;
        label.fontStyle |= FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = Mathf.Max(label.fontSize, 20f);
    }

    T FindUI<T>(string n) where T : Component
    {
        var go = GameObject.Find(n);
        return go == null ? null : go.GetComponent<T>();
    }
}