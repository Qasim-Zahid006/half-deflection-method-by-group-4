# Experiment 14.4 — Resistance of a Galvanometer by the Half-Deflection Method

An interactive 3D virtual physics lab built in **Unity 6 (6000.4.8f1)** with the **Universal Render Pipeline (URP)**. Students wire up a galvanometer circuit, take readings, and measure the internal resistance ($R_g$) of a galvanometer using the classic half-deflection method — then test their understanding in a built-in quiz.

> *Built by **Group 4**.*

---

## 📑 Contents
- [Experiment Theory](#-experiment-theory--derivation)
- [How It Works (Simulation Model)](#-how-it-works-simulation-model)
- [Scenes](#-scenes)
- [Project Architecture (Scripts)](#-project-architecture-scripts)
- [Controls](#-controls)
- [How to Perform the Experiment](#-how-to-perform-the-experiment)
- [Building](#-building)
- [Tech Stack](#-tech-stack)
- [Known Limitations](#-known-limitations--future-work)

---

## 📖 Experiment Theory & Derivation

The half-deflection method finds the resistance of a sensitive galvanometer ($G$) without connecting an ohmmeter directly across its delicate coil.

### The Circuit
1. A cell/battery of EMF ($V$).
2. A **High Resistance Box (HRB, $R$)** in series to limit current and protect the galvanometer.
3. A main key ($K_1$) controlling the primary loop.
4. A **Low Resistance Box (LRB, $R_s$)** connected in parallel across the galvanometer as a shunt.
5. A shunt key ($K_2$) controlling the parallel branch.

```
       +----[ HRB (R) ]----( K1 )----+
       |                             |
  ( Battery )                        |
       |                   +---[ Galvanometer (Rg) ]---+
       |                   |                           |
       +-------------------+---( K2 )----[ LRB (Rs) ]---+
```

### Derivation

**Case 1 — $K_1$ closed, $K_2$ open.** All current flows through the galvanometer:

$$I_g = \frac{V}{R + R_g + r}$$

Since deflection $\theta$ is proportional to the current ($I_g = k\theta$), this gives the **full deflection** $\theta$.

**Case 2 — $K_1$ closed, $K_2$ closed.** The shunt $R_s$ is now in parallel with $R_g$:

$$R_p = \frac{R_g R_s}{R_g + R_s}, \qquad I_{total} = \frac{V}{R + R_p}$$

By the current-divider rule the galvanometer current becomes:

$$I'_g = I_{total} \times \frac{R_s}{R_s + R_g}$$

Adjust $R_s$ until the deflection is exactly **half** ($\theta' = \theta/2$, i.e. $I'_g = I_g/2$). With $R$ chosen large so the total current stays nearly constant:

$$\frac{1}{2} = \frac{R_s}{R_s + R_g} \implies \boxed{R_g = R_s}$$

So at half deflection, **the galvanometer resistance equals the LRB reading**.

---

## ⚙️ How It Works (Simulation Model)

The circuit is solved every frame in `HalfDeflectionController.SolveAndDisplay()` using the exact equations above (not a general nodal solver — see [Limitations](#-known-limitations--future-work)):

- **Hidden, randomized $R_g$** — the true galvanometer resistance is randomized between **40 Ω and 120 Ω** at scene start, so the value must actually be measured each run (it is logged to the console only for debugging).
- **Supply** — battery EMF defaults to **3 V** with a small internal resistance.
- **Deflection scale** — the needle reads up to **30 divisions** at the full-scale current (**5 mA**); deflection is clamped to the scale and the needle turns **red on overload**.
- **Damped-spring needle** — `Galvanometer.cs` animates the needle with a spring–damper equation so it oscillates and settles like a real meter.
- **Half-deflection tolerance** — a reading can only be recorded when the deflection is within **±0.75 div** of $\theta/2$; the status line tells you whether to increase or decrease the LRB.
- **Auto-averaging** — recorded rows ($R$, $\theta$, $R_s$, $\theta/2$, $R_g{=}R_s$) are tabulated and the **mean $R_g$** is computed live.

---

## 🎬 Scenes

| Scene | Purpose |
|-------|---------|
| `MainMenu.unity` | Entry menu — **Practical**, **Coursework**, **Quit**. Legacy experiment buttons (14.1–14.3) are hidden by `MainMenuController`. |
| `HalfDeflectionScene.unity` | The interactive experiment: wiring, keys, sliders, galvanometer, observation table. The lab bench, walls, shelf, switches and labels are **generated procedurally in code**. |
| `CourseWork.unity` | Theory tabs (Definition / Formulas / Procedure / Overview) **plus** a randomized multiple-choice quiz. |

---

## 🧩 Project Architecture (Scripts)

All gameplay scripts live in `Assets/Scripts/`:

| Script | Responsibility |
|--------|----------------|
| `HalfDeflectionController.cs` | The core controller — solves the circuit, drives the galvanometer, manages the Capture/Record workflow, builds the procedural lab, switches, junctions and UI, auto-wiring, and the observation table. |
| `Galvanometer.cs` | Damped-spring needle physics, mA readout, and overload coloring. |
| `Battery.cs` | EMF + internal resistance source with positive/negative terminals. |
| `ResistanceBox.cs` | Slider-driven variable resistance (HRB 0–10 kΩ, LRB 0–500 Ω) with mouse-wheel fine adjustment. |
| `Resistor.cs` | Fixed-value resistor visual used for the HRB/LRB blocks. |
| `KeyComponent.cs` | Open/close switch logic for $K_1$ and $K_2$. |
| `WireManager.cs` | Interactive click-to-connect wiring, snapping, deletion, and the programmatic auto-wire API. |
| `ConnectingWIre.cs` | Renders a sagging catenary wire between two terminals with energized coloring. |
| `MainMenuController.cs` | Wires the main-menu buttons and hides legacy practicals. |
| `CourseworkController.cs` | The quiz: picks **5 of 10** questions at random; **3/5 required to pass**. |
| `CourseworkTabs.cs` | Theory content tabs for the coursework scene. |
| `MobileMaterialRepair.cs` | Runtime URP/mobile shader-repair helper. |
| `Editor/PracticalBuildPipeline.cs` | One-click build menu items (Editor only). |

---

## 🎮 Controls

| Action | Input |
|--------|-------|
| Start a wire | **Left-click** a terminal anchor |
| Complete a wire | **Left-click** a second terminal (snaps within 0.45 m) |
| Cancel a wire | **Esc** |
| Delete a wire | **Right-click** the wire |
| Adjust HRB / LRB | Drag the slider, or **hover + scroll wheel** |
| Toggle keys | Click the **K1 / K2** buttons |
| Shortcut wiring | **Auto Wire** / **Clear Wires** buttons |

---

## 🔬 How to Perform the Experiment

1. Launch and select **PRACTICAL**.
2. Wire the circuit, or click **Auto Wire** to connect it instantly:
   * **Main loop:** Battery (+) → $K_1$ → HRB → left junction
   * **Galvanometer branch:** left junction → Galvanometer → right junction
   * **Shunt branch:** left junction → $K_2$ → LRB → right junction
   * **Return:** right junction → Battery (−)
3. Keep $K_2$ **OPEN**, close $K_1$.
4. Adjust the **HRB** slider until the needle gives a clear, even deflection (not pinned at full scale).
5. Click **Capture Full Deflection** to record $\theta$.
6. Close $K_2$ — the deflection drops.
7. Adjust the **LRB** slider until the deflection is exactly **half** of $\theta$ (the status line guides you).
8. Click **Record Half Reading** — the row is added with $R_g = R_s$.
9. Open $K_2$, change the HRB for a new initial deflection, and repeat.
10. After three or more rows, read the **Mean $R_g$** at the bottom of the table.

Then open **COURSEWORK** to review the theory tabs and pass the quiz (≥ 3/5).

---

## 🛠️ Building

Open the project in Unity 6 and use the **Build** menu (added by `PracticalBuildPipeline.cs`):

- **Build → Final Practical → Build WebGL**
- **Build → Final Practical → Build Android APK**
- **Build → Final Practical → Build Windows App**
- **Build → Final Practical → Build Demo Package** (all three)

Builds output to a `Builds/` folder.

---

## 🧰 Tech Stack

- **Unity** `6000.4.8f1`
- **Universal Render Pipeline** `17.4.0`
- **Input System** `1.19.0` (new input system)
- **uGUI** `2.0.0` + **TextMesh Pro**

---

## ⚠️ Known Limitations & Future Work

1. **Fixed-topology solver** — connections are validated via a terminal hash set, but the equations are solved for the known 14.4 network rather than by general nodal analysis.
2. **Primitive procedural props** — the lab bench, shelf, bottles and posters are built from Unity primitives rather than imported models.
3. **No wire-to-wire collision** — wires can visually cross without interacting.
4. **Voltmeter variant** — extending the half-deflection method to voltmeter resistance is planned but not implemented.
