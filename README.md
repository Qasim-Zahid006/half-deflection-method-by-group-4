# Experiment 14.4: Resistance of Galvanometer by Half-Deflection Method

An interactive 3D virtual laboratory simulator built in Unity using the Universal Render Pipeline (URP). This simulator allows students to perform the classic physics experiment to measure the internal resistance ($R_g$) of a galvanometer, record data, and test their theoretical knowledge.

---

## 📖 Experiment Theory & Derivation

The half-deflection method is a standard laboratory procedure used to find the resistance of a sensitive galvanometer ($G$).

### The Circuit Setup
The circuit consists of:
1. A cell/battery of EMF ($V$).
2. A High Resistance Box (HRB, $R$) in series with the battery to limit current.
3. A main key ($K_1$) controlling the primary circuit loop.
4. A Low Resistance Box (LRB, $R_s$) connected in parallel across the galvanometer to act as a shunt.
5. A shunt key ($K_2$) controlling the parallel shunt branch.

```
       +----[ HRB (R) ]----( K1 )----+
       |                             |
  ( Battery )                        |
       |                   +---[ Galvanometer (Rg) ]---+
       |                   |                           |
       +-------------------+---( K2 )----[ LRB (Rs) ]---+
```

### Derivation of Formula
* **Case 1: $K_1$ is Closed and $K_2$ is Open**
  The current flows only through the main loop and passes entirely through the galvanometer. The current through the galvanometer ($I_g$) is given by:
  $$I_g = \frac{V}{R + R_g + R_{int}}$$
  *(where $R_{int}$ is the internal resistance of the battery, which is typically negligible).*
  
  Since the needle deflection ($\theta$) is directly proportional to the current flowing through it:
  $$I_g = k \theta = \frac{V}{R + R_g}$$
  *(where $k$ is the figure of merit of the galvanometer).*

* **Case 2: $K_1$ is Closed and $K_2$ is Closed**
  Closing $K_2$ shunts the galvanometer with the low resistance $R_s$. The total resistance of the parallel combination is:
  $$R_p = \frac{R_g R_s}{R_g + R_s}$$
  
  The total current in the circuit becomes:
  $$I_{total} = \frac{V}{R + R_p}$$
  
  Using the current divider rule, the current passing through the galvanometer ($I'_g$) is:
  $$I'_g = I_{total} \times \frac{R_s}{R_s + R_g} = \frac{V}{R + \frac{R_g R_s}{R_g + R_s}} \times \frac{R_s}{R_s + R_g}$$
  
  If the series resistance $R$ is chosen to be extremely high ($R \gg R_g$ and $R \gg R_s$), the total resistance remains approximately dominated by $R$. Thus, $I_{total} \approx I_g$, meaning the total current is nearly constant.
  
  We adjust the shunt resistance $R_s$ until the galvanometer deflection becomes exactly half of the initial deflection ($\theta' = \theta / 2$):
  $$I'_g = \frac{I_g}{2}$$
  
  Substituting the current divider relation:
  $$\frac{I_g}{2} = I_g \times \frac{R_s}{R_s + R_g} \implies \frac{1}{2} = \frac{R_s}{R_s + R_g}$$
  
  Cross-multiplying gives:
  $$R_s + R_g = 2R_s \implies R_g = R_s$$
  
  Therefore, when half-deflection is achieved under high series resistance, **the resistance of the galvanometer is equal to the resistance set in the Low Resistance Box (LRB)**.

---

## 🛠️ Features Completed

### 1. Circuit Solver & Needle Physics
* **Accurate Simulation**: Calculates real-time current based on battery EMF ($V$), series HRB ($R$), shunt LRB ($R_s$), and internal resistance.
* **Spring-Damped Needle**: Needle rotation uses a spring-damper equation ($F = -kx - c\dot{x}$) to mimic physical needle oscillations and settling times.
* **Randomized Internal Resistance**: The true $R_g$ is randomized between $40\Omega$ and $120\Omega$ at scene start, forcing students to perform the experiment rather than memorizing a fixed number.
* **Overload Warning**: High current turns the needle red and pins it to max scale.

### 2. Interactive Controls & UI
* **Sliders & Scroll Support**: Slider controls for HRB ($0 - 10,000\Omega$) and LRB ($0 - 500\Omega$) with mouse scroll-wheel support for precise resistance adjustments.
* **Observation Table**: A scrollable UI panel logs observations and calculates the final mean galvanometer resistance automatically.
* **Interactive Wiring**: Drag-and-drop wire connection between terminal pins with a snapping radius to prevent frustration.
* **Auto-Wire Utility**: A debug/assist button that automatically wires the correct circuit layout instantly.

### 3. Procedural 3D Environment
* Programmatically generated lab bench, tiled floor, shelf, backdrop window, and safety posters.
* Animated 3D keys ($K_1$, $K_2$) showing open and closed copper key lever rotations.

### 4. Coursework Quiz Scene
* A randomized 5-question multiple-choice quiz checking the student's theoretical and practical understanding of the experiment.
* Passing threshold of $3/5$ correct answers is required to exit and return to the main menu.

### 5. Build Pipeline
* Editor script `PracticalBuildPipeline.cs` compiles Standalone Windows, WebGL, and Android APK builds with single-click menu items.

---

## 🔧 Visual Noise & Shader Glitch Fixes (Completed)

We addressed visual bugs that interfered with the rendering pipeline:
1. **Dither Grid Noise Fix**: Removed rendering noise (interlaced dot patterns) by disabling URP compilation dither overrides on standard shaders.
2. **Confusing Wire Particles ("Shadder Thing")**: Fully disabled the green matrix-style particle flow system that instantiated along the wires, resolving shader errors and screen clutter while keeping wire colors.
3. **URP Mobile Shader Repair**: Disabled the `MobileMaterialRepair` runtime generator that was causing incorrect shader overrides.

---

## ⚠️ Project Limitations & Future Work

1. **Hardcoded Topology Solver**: The circuit solver checks connections using a terminal hash set but solves equations based on a fixed hardcoded network. It does not perform general nodal matrix analysis.
2. **Primitive 3D Models**: Background elements are generated out of basic Unity primitive blocks (Cubes and Spheres) instead of high-fidelity imported 3D models.
3. **No Wire-to-Wire Collision**: Connecting wires do not have physics colliders, allowing them to pass through one another if crossed.
4. **Voltmeter Calibration**: Expanding the tool to support voltmeter resistance measurement using half-deflection is planned but currently uncompleted.

---

## 🎮 How to Perform the Experiment

1. Launch the simulator and select **PRACTICAL**.
2. Connect wires between all terminals:
   * **Main Loop**: Battery (+) $\rightarrow$ $K_1$ $\rightarrow$ HRB $\rightarrow$ Junction Left.
   * **Galvanometer Branch**: Junction Left $\rightarrow$ Galvanometer $\rightarrow$ Junction Right.
   * **Shunt Branch**: Junction Left $\rightarrow$ $K_2$ $\rightarrow$ LRB $\rightarrow$ Junction Right.
   * **Return**: Junction Right $\rightarrow$ Battery (-).
   *(Or simply click **Auto Wire** to connect everything instantly).*
3. Keep $K_2$ **OPEN** and close $K_1$.
4. Drag the **HRB Slider** (or hover and scroll) until the galvanometer needle deflects to a high even division (e.g., $30\text{ div}$).
5. Click **Capture Full Deflection**.
6. Close the key $K_2$. The deflection will drop.
7. Adjust the **LRB Slider** (or hover and scroll) until the galvanometer deflection becomes exactly half of the captured value (e.g., $15\text{ div}$).
8. Click **Record Half Reading** to add the data to the observation table.
9. Open $K_2$, change the HRB resistance to obtain a different initial deflection (e.g., $24\text{ div}$), and repeat the steps.
10. Once at least three rows are added, note the **Mean $R_g$** displayed at the bottom of the table.
