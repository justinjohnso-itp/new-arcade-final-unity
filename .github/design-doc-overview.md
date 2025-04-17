# **Unity Development Guide and Game Design Document: Steampunk Delivery Arcade**

This document provides a technical development approach and a Game Design Document (GDD) for creating an isometric infinite-scrolling delivery game in Unity 2D, designed for a custom arcade cabinet with steering wheel, rotary wheel, and button controls. It is tailored for a developer with beginner-level Unity experience aiming for a rapid prototype.

## **Part 1: Recommended Development Approach for Core Gameplay Prototype**

This section outlines a step-by-step technical approach for building the core gameplay loop, focusing on achieving a playable prototype quickly.

### **1.1 Project Setup: Isometric Foundation**

The foundation of the game lies in correctly setting up the isometric perspective using Unity's Tilemap system.

* **Create Isometric Tilemap:** Navigate to GameObject \> 2D Object and select Isometric Tilemap. While Isometric Z as Y Tilemap allows for height variations by offsetting tiles based on their Z-position, the standard Isometric Tilemap is simpler for a vertically scrolling game without complex verticality and sufficient for the initial prototype.1  
* **Grid Configuration:**  
  * A Grid GameObject is automatically created. Ensure its Cell Layout is set to Isometric.  
  * Set the Cell Size property on the Grid component to match the dimensions of the isometric tile sprites provided by the art team. For true isometric projection, the Y value is often set to 0.57735, while dimetric uses 0.5.1 Consult the art assets for the correct dimensions.  
* **Graphics Settings for Sorting:** Correct rendering order is crucial for the isometric illusion.  
  * Go to Edit \> Project Settings \> Graphics.  
  * Under Camera Settings, set Transparency Sort Mode to Custom Axis.  
  * Set the Transparency Sort Axis to (X: 0, Y: 1, Z: 0). This ensures that objects with higher Y-positions in the Scene are rendered behind those with lower Y-positions, creating the necessary pseudo-depth for a standard isometric view.1 If Isometric Z as Y were used, this axis would need to be (0, 1, \-0.26) to account for Z-position height.1

### **1.2 Core Mechanic 1: Infinite Scrolling Environment (Chunk-Based Procedural Generation)**

To create the infinite scrolling effect common in games like *Paperboy* and *Icy Tower*, a chunk-based procedural generation system is recommended.

* **Chunk Prefabs:**  
  * Design reusable sections of the road/environment as distinct prefabs. Each prefab represents a "chunk" of the level.  
  * Each chunk prefab should contain an Isometric Tilemap layer for the road and surrounding scenery, configured with the project's Grid settings.  
  * Include potential spawn points for delivery zones and obstacles within these prefabs.  
  * Ensure each chunk prefab has a defined height or length.  
* **Generation Logic:**  
  * Create a LevelGenerator script (or similar).  
  * Maintain a list of active chunk GameObjects.  
  * As the player moves upwards (or the camera scrolls), detect when the player is approaching the end of the currently visible chunks.  
  * When a threshold is crossed, instantiate a new chunk prefab from a predefined list (can be randomized later for variety 4) and position it directly ahead of the last active chunk.  
  * Simultaneously, detect when chunks have scrolled sufficiently far behind the player (off-screen). Destroy these old chunks to maintain performance.5 This involves checking the chunk's position relative to the camera or a "destroyer" trigger volume placed behind the camera.5  
  * This continuous process of spawning ahead and destroying behind creates the illusion of an infinite road.6  
* **Initial Implementation:** Start by simply spawning copies of the *same* chunk prefab repeatedly to get the core scrolling mechanic working. Introduce variation and different chunk types later. The core principle is similar to parallax background scrolling, but applied to tilemap chunks in the game world.8

### **1.3 Core Mechanic 2: Player Truck Movement (Rigidbody Physics, Steering Input, Isometric Translation)**

Player movement involves combining automatic forward motion with player-controlled steering, translated correctly for the isometric perspective.

* **Player Setup:**  
  * Create a PlayerController C\# script.  
  * Attach this script to the player truck GameObject.  
  * Add a Rigidbody2D component to the truck.9 In the Rigidbody2D settings:  
    * Set Body Type to Dynamic (or Kinematic if opting for non-physics movement, though Dynamic is generally preferred for collision handling).  
    * Set Gravity Scale to 0, as gravity is not applicable in this top-down/isometric context.10  
    * Under Constraints, enable Freeze Rotation Z to prevent the truck from spinning unexpectedly on the 2D plane.10  
  * Add a Collider2D component (e.g., BoxCollider2D, PolygonCollider2D) to the truck GameObject. This collider will interact with obstacles and delivery zone triggers.9 The Rigidbody2D component manages the movement and collision response based on these colliders.9  
* **Steering Input:**  
  * Utilize Unity's new Input System (UnityEngine.InputSystem). Create an Input Action Asset (.inputactions file).  
  * Define an Action Map (e.g., "Gameplay").  
  * Within this map, create an Action named "Steer" with an Action Type of Value and a Control Type of Axis.12  
  * Attempt to bind this "Steer" action directly to the physical steering wheel's horizontal axis input. This can be done in the Input Action editor's binding properties. Use the Input Debugger (Window \> Analysis \> Input Debugger) to identify the correct control path for the connected steering wheel HID.13 The pre-start Input Configuration Manager might also help identify axes, though it relies on the older Input Manager setup.13  
  * If direct binding proves difficult or unreliable (common with non-standard controllers), prepare for the Arduino \+ Serial communication fallback detailed in Section 1.6.16  
* **Isometric Movement Logic:**  
  * Implement movement logic within the FixedUpdate() method of the PlayerController script. FixedUpdate is recommended for physics-based operations to ensure consistent behavior regardless of frame rate.17  
  * Read the current value of the "Steer" action (e.g., steerAction.ReadValue\<float\>()). This should yield a value typically between \-1 (full left) and 1 (full right).  
  * Define variables for moveSpeed (controlled by steering input) and forwardSpeed (automatic, increases over time).  
  * Calculate the desired movement vector. The challenge here is translating the 1D steering input (screen-relative left/right) and the automatic forward motion into a combined velocity vector appropriate for the isometric world space. Applying raw input axes directly often leads to unnatural diagonal movement. Explicit conversion is needed.  
  * A potential starting point for the calculation within FixedUpdate:  
    C\#  
    float steerInput \= steerAction.ReadValue\<float\>(); // Get value from \-1 to 1  
    // forwardSpeed should increase over time based on game logic  
    Vector2 rawMovement \= new Vector2(steerInput \* moveSpeed, forwardSpeed);

    // \--- Isometric Translation \---  
    // This is a simplified example; the exact transformation depends  
    // on the isometric angle. A common approach involves rotating  
    // the input vector or using a transformation matrix.  
    // See \[37\] for a matrix example. For a basic start:  
    // Assuming standard isometric projection where world Y maps roughly to screen Y,  
    // and world X maps diagonally. This needs refinement\!  
    // A potential simple (but likely needing adjustment) conversion:  
    Vector2 isometricMovement \= new Vector2(rawMovement.x \- rawMovement.y \* 0.5f, (rawMovement.x \+ rawMovement.y) \* 0.25f); // Placeholder math \- adjust based on visual testing\!  
    // Or, more simply, treat X as screen-X and Y as screen-Y, then adjust speeds:  
    // Vector2 isometricMovement \= rawMovement; // Adjust moveSpeed and forwardSpeed until it 'looks right'

    // Apply movement to the Rigidbody2D  
    rb.velocity \= isometricMovement; // Where rb is the Rigidbody2D component

  * Apply the calculated isometricMovement vector to the Rigidbody2D component, typically by setting its velocity property (rb.velocity \= isometricMovement;).10 Using MovePosition is an alternative, especially if not using physics forces extensively, but velocity often feels more natural for continuous motion.17 Fine-tuning the translation calculation and speed values will be crucial during prototyping to achieve the desired feel.

### **1.4 Core Mechanic 3: Managing Packages (Inventory Data, Visual Stack UI, Rotary Input Mapping, Cycling/Highlighting)**

This involves managing the package data, displaying it visually, and allowing the player to cycle through it using the rotary wheel.

* **Inventory Data Structure:**  
  * Implement a simple data structure within an InventoryManager script (or directly in PlayerController for the prototype). A List\<PackageType\> is suitable, where PackageType could be an enum defining the different symbols (e.g., enum PackageType { Gear, Bolt, Pipe }) or, more flexibly, a ScriptableObject containing symbol sprite, name, etc.  
  * For the initial prototype, populate this list with a predefined set of packages in the Start() method for testing. The actual acquisition mechanic (proposed in the GDD) can be added later.  
* **Visual Stack UI:**  
  * Utilize Unity's UI (UGUI) system. Ensure a Canvas object exists in the scene.  
  * Create a Panel GameObject as a child of the Canvas, positioned on the right side of the screen. This panel will contain the package icons.  
  * Attach a Vertical Layout Group component to this panel.18 This component automatically arranges its child UI elements in a vertical column. Configure its Padding and Spacing properties to achieve the desired look.18 Set Child Alignment (e.g., to Lower Center or Middle Center) based on how the stack should align within the panel.18  
  * Create a prefab for the individual package display element. This prefab typically consists of a GameObject with an Image component to show the package symbol.  
  * In the InventoryManager script, write code to instantiate these package prefabs as children of the inventory panel GameObject. The Vertical Layout Group will automatically position them.18 The number of instantiated prefabs should match the number of items in the inventory data structure.  
  * To ensure the container panel resizes correctly as packages are added or removed, add a Content Size Fitter component to the inventory panel GameObject. Set its Vertical Fit property to Preferred Size.20 This addresses common issues where layout groups might not update their size correctly with dynamic content.22  
* **Rotary Input:**  
  * Similar to the steering wheel, first attempt mapping using the Input System. Create a "CycleInventory" action. The rotary encoder might register as a continuous Axis or as discrete Button presses for clockwise/counter-clockwise turns, depending on its hardware implementation.25  
  * **Crucially, the game requires discrete cycling (move selection up/down one slot per "click" of the encoder).** If the encoder registers as an Axis, the input handling script must detect when the axis value crosses a certain threshold *and* was previously below it, triggering a single cycle action. It should then ignore further axis changes until the value returns below the threshold and crosses it again. This prevents a single turn from rapidly cycling through the entire inventory. A Value type action with processors like Normalize might be useful.27  
  * Given the non-standard nature of rotary encoders, direct mapping might be challenging. Arduino \+ Serial communication is a highly probable requirement.25 The Arduino code would read the encoder's A/B phase signals 26, detect rotation direction, and send simple messages like "UP" or "DOWN" over the serial port. The Unity script (using the non-blocking method from Section 1.6) would then receive these messages.  
* **Cycling and Highlighting Logic:**  
  * Implement the inventory cycling logic, likely within the InventoryManager or PlayerController script's Update() method (as input is typically checked here).  
  * Maintain an integer variable, selectedIndex, representing the index of the currently selected package in the inventory list (which corresponds to the item in the delivery slot).  
  * When "CycleInventory" input (either from Input System or serial message) is detected:  
    * Increment or decrement selectedIndex.  
    * Implement wrap-around logic: If decrementing below 0, wrap to inventory.Count \- 1\. If incrementing above inventory.Count \- 1, wrap to 0\. Use the modulo operator for clean wrapping: selectedIndex \= (selectedIndex \+ direction \+ inventory.Count) % inventory.Count; (where direction is \+1 or \-1).  
  * Update the visual UI stack to reflect the new selection. The simplest approach for a stack where the *bottom* item is selected is to visually reorder the UI GameObjects in the hierarchy under the Vertical Layout Group. The GameObject corresponding to inventory\[selectedIndex\] should always be moved to the last position in the hierarchy (which the Vertical Layout Group typically renders at the bottom, depending on settings).  
  * Highlight the UI element representing the selected package (the one at the bottom/delivery slot). Store references to the instantiated package UI Image or GameObject components. Change the color, sprite, or enable/disable a separate "highlight" overlay image/GameObject on the selected element.29 An Animator component on the package prefab could also be triggered to play a highlight animation.29

### **1.5 Core Mechanic 4: Making Deliveries (Zone Spawning, Collision Detection, Matching Logic)**

This covers spawning delivery targets and handling the core interaction of matching packages to zones.

* **Delivery Zone Spawning:**  
  * Integrate delivery zone spawning into the chunk generation system (Section 1.2). Define specific Transform points within the road chunk prefabs where zones can appear.  
  * When the LevelGenerator spawns a new chunk, use a random chance (or logic based on difficulty/progression) to decide whether to instantiate a delivery zone prefab at one of its designated spawn points.  
  * The delivery zone prefab should contain:  
    * Its visual representation (e.g., a sprite or effect indicating the required package symbol).  
    * A Collider2D component (e.g., BoxCollider2D) with the Is Trigger property enabled. This allows detecting overlap without causing physics collisions.  
    * A script (e.g., DeliveryZone) holding data about the required PackageType.  
* **Collision Detection:**  
  * Implement the OnTriggerEnter2D(Collider2D other) method within the PlayerController script. This Unity message function is automatically called when the truck's collider (attached to the Rigidbody2D) enters another collider marked as a trigger.  
  * Inside OnTriggerEnter2D, check if the other.gameObject that the truck collided with is a delivery zone. This can be done by checking its tag (e.g., other.CompareTag("DeliveryZone")) or by attempting to get the DeliveryZone component (other.GetComponent\<DeliveryZone\>()\!= null).  
* **Matching Logic:**  
  * If the collision is with a valid DeliveryZone:  
    * Retrieve the required PackageType from the DeliveryZone script on other.gameObject.  
    * Retrieve the currently selected PackageType from the player's inventory using the selectedIndex (e.g., inventory\[selectedIndex\]).  
    * Compare the required type and the selected type.  
    * **If they match (Successful Delivery):**  
      * Trigger success feedback: Add points to the score, play a success sound effect, show a visual effect (e.g., particle burst).  
      * Remove the delivered package from the inventory data structure: inventory.RemoveAt(selectedIndex).  
      * **Crucially:** Update the inventory UI to reflect the removal. Destroy the corresponding package UI GameObject. Ensure selectedIndex remains valid (e.g., if the last item was delivered, adjust index or handle empty inventory state).  
    * **If they do not match (Incorrect Delivery):**  
      * Trigger failure feedback: Deduct points (optional), play an error sound, maybe a small visual indicator (e.g., red flash).  
      * Do *not* remove the package from the inventory. The player must continue driving and potentially cycle to the correct package if they re-enter the zone or encounter another matching one later.  
  * This sequence—detecting zone entry via trigger, comparing selected inventory item to zone requirement, and updating game state (score, inventory, UI) based on match/mismatch—forms the heart of the delivery gameplay loop.

### **1.6 Handling Custom Arcade Controls (Input System Configuration, Serial Communication Considerations)**

Connecting and reliably reading input from the custom steering wheel, rotary encoder, and button is critical and potentially the most complex part for a beginner.

* **Prioritize Unity Input System:**  
  * Always attempt to map the controls using Unity's built-in Input System first, as it's the most integrated and often simplest solution if it works.13  
  * Focus on the steering wheel and the button, as these are more likely to conform to standard HID (Human Interface Device) protocols that the Input System understands.14  
  * Use the Input Debugger extensively (Window \> Analysis \> Input Debugger) to see if the devices are recognized and what control paths they expose when manipulated. Try binding actions directly to these paths.  
  * If standard bindings fail, consider creating a custom Device Layout.15 This involves defining a C\# struct representing the device's data format (state struct) and a class inheriting from InputDevice that uses this struct. Annotations like \[InputControl\] map struct fields to controls (buttons, axes).15 This is an advanced technique and likely too time-consuming for the initial prototype deadline, but it's the "proper" Input System way to handle truly custom hardware.  
* **Arduino \+ Serial Communication as Fallback/Primary for Rotary:**  
  * If direct mapping fails, especially for the rotary encoder which often uses non-standard communication 25, the common fallback is using an Arduino microcontroller.  
  * **Concept:** The Arduino board connects to the physical controls (potentiometer for steering wheel 16, rotary encoder 25, button). Arduino code reads the state of these sensors continuously. When a change is detected (wheel turned, encoder clicked, button pressed), the Arduino sends a simple message (a string like "LEFT", "RIGHT", "UP", "DOWN", "BTN\_PRESS" or numerical values) over the USB serial connection.  
  * **Unity Side:** A C\# script in Unity needs to:  
    * Include the System.IO.Ports namespace.  
    * Create a SerialPort object, specifying the correct COM port name (e.g., "COM3" on Windows, "/dev/ttyACM0" on Linux \- this must match the port assigned by the OS to the Arduino) and the baud rate (which must match the rate set in the Arduino code, e.g., 9600 or 115200).16  
    * Open the connection (sp.Open()).  
    * Read incoming data (sp.ReadLine(), sp.ReadByte(), sp.ReadExisting()).  
  * **CRITICAL: Avoid Blocking the Main Thread:** Directly calling ReadLine() or Read() in Unity's Update() or FixedUpdate() loop is highly problematic. These methods *block* execution until data arrives or a timeout occurs. This will freeze the entire game, making it unresponsive.31  
  * **Best Practice \- Asynchronous Reading:** The recommended approach is to perform serial port reading on a separate background thread.31  
    1. Create a dedicated thread in your Unity script (System.Threading.Thread).  
    2. The thread's main loop continuously attempts to read from the serial port.  
    3. When data is received, the background thread should not directly interact with Unity game objects or APIs (as this is not thread-safe). Instead, it places the received data (e.g., the string "UP") into a thread-safe queue, such as System.Collections.Queue.Synchronized(new System.Collections.Queue()).32  
    4. Back in the main Unity thread (e.g., in Update()), the script checks if there is any data in the queue. If so, it dequeues the message and processes it (e.g., updates the selectedIndex based on an "UP" message).  
    5. This asynchronous pattern prevents the game from freezing while waiting for serial data.32 Resources like Alan Zucconi's tutorial on asynchronous serial communication are valuable references.32  
  * **Potential Serial Pitfalls:**  
    * **COM Port:** Finding the correct port name is essential.16  
    * **Baud Rate:** Must match between Arduino and Unity.30  
    * **Permissions:** On some OSes (like Linux), permissions might be needed to access the serial port.34  
    * **Arduino Reset:** Arduinos often reset when a serial connection is opened, requiring a delay before communication can start.30  
    * **API Compatibility:** Unity's Player Settings might need the API Compatibility Level set to .NET Framework (or .NET Standard 2.1 depending on Unity version and specific needs) for System.IO.Ports to be available and work correctly.16  
    * **Read Method:** ReadLine() waits for a newline character. Ensure the Arduino sends one (Serial.println()). ReadByte() or ReadExisting() might be alternatives depending on the data format.31  
    * **Serial Monitor:** The Arduino IDE's Serial Monitor cannot be open at the same time Unity tries to access the port.16  
    * **DTR/RTS:** Sometimes setting sp.DtrEnable \= true; and sp.RtsEnable \= true; is necessary for the connection to establish correctly, particularly on Linux.34  
    * **Cross-Platform:** Serial port names and behavior can differ between Windows, macOS, and Linux.36  
* **Input Testing Plan:** Due to the complexity and potential issues with custom controls, **immediately dedicate time to testing input mapping**. Create a minimal test scene with scripts solely focused on reading the steering wheel, rotary encoder, and button. Test both direct Input System binding *and* the Arduino/Serial approach. Log the received values to the console. This early test will determine the most viable path forward for the prototype and prevent wasted effort building game logic around assumptions that might prove incorrect.  
  **Proposed Input Mapping Plan:**

| Physical Control | Intended Action | Unity Input System Action (Target) | Potential Fallback (Serial/Arduino) | Notes |
| :---- | :---- | :---- | :---- | :---- |
| Steering Wheel | Steer Truck Left/Right | "Steer" (Axis) | Arduino Potentiometer \-\> Serial 16 | Test direct HID mapping first.13 Needs \-1 to 1 range. |
| Rotary Wheel | Cycle Inventory Up/Down | "CycleInventory" (Button \+/-?) | Arduino Encoder \-\> Serial "Up"/"Down" 28 | Needs discrete steps. Serial likely required. Input System may see axis. |
| Button | Deliver Package / Action | "Deliver" (Button) | Arduino Button \-\> Serial "Pressed" | Standard button, test direct mapping first. |

This plan provides a structured approach to tackling the unique input hardware, prioritizing the integrated solution while acknowledging the high likelihood of needing the Arduino/Serial fallback, especially for the rotary encoder. It guides the essential immediate testing required.

## **Part 2: Game Design Document (GDD)**

This document outlines the design for the Steampunk Delivery Arcade game, focusing on the core elements needed for the initial prototype and addressing the user's open questions.

### **2.1 Game Overview**

* **Title:** *SteamWheel Deliveries* (Working Title \- Replaces "The New Arcade")  
* **Genre:** Isometric Infinite Scroller, Arcade Delivery/Action  
* **Target Audience:** Players at the physical NYU Tisch ITP arcade cabinet installation, fans of classic arcade games (*Paperboy*), potentially casual indie game players interested in unique control schemes (*Wilmot's Warehouse* sorting aspect).  
* **Core Pillars:** Fast-paced Delivery Action, Tactical Inventory Management, Steampunk Aesthetic, Unique Physical Arcade Interface.  
* **Pitch:** Pilot your clanking steampunk delivery truck through an infinitely scrolling cityscape\! Use a real steering wheel to weave through traffic and hazards, expertly spin a rotary dial to sort your ever-growing stack of packages, and make timely deliveries by matching symbols to zones. Keep the deliveries flowing before your truck breaks down or your cargo hold overflows in this unique arcade challenge\!

### **2.2 Core Gameplay Loop**

1. **Drive:** The truck moves forward automatically on an isometric scrolling road. Speed gradually increases.  
2. **Steer:** Player uses the physical steering wheel to control the truck's horizontal position, navigating lanes and avoiding obstacles.  
3. **Observe:** Upcoming delivery zones appear along the route, displaying specific package symbols.  
4. **Sort:** Player uses the physical rotary wheel to cycle through the package inventory stack (visualized on the right), bringing the required package to the designated "delivery slot" (bottom of the stack).  
5. **Deliver:** Player steers the truck into the delivery zone *while* the correctly matching package is selected in the delivery slot.  
6. **Manage:** New packages are periodically added to the inventory stack (acquisition method defined below).  
7. **Survive:** Avoid colliding with obstacles and manage inventory efficiently.  
8. **Repeat:** Continue driving, sorting, and delivering as difficulty ramps up, aiming for the highest score before a loss condition is met.

### **2.3 Mechanics Deep Dive**

* **Driving:**  
  * **Control:** Physical steering wheel input dictates horizontal position/velocity.  
  * **Speed:** Automatic forward velocity. Starts at a base value and increases steadily based on distance traveled or time elapsed. This serves as a primary difficulty driver. Power-ups affecting speed are a potential future addition but not core for the prototype.  
* **Inventory Management:**  
  * **UI:** A visual stack of package icons displayed vertically on the right side of the screen.18 The bottom-most icon is highlighted, representing the package currently ready for delivery.  
  * **Control:** Physical rotary wheel cycles the selection. Each distinct "click" or detected step of the encoder moves the selection up or down one position in the stack, with wrap-around. Requires careful input handling to ensure discrete steps (see Section 1.4).  
  * **Package Acquisition (Proposal):** Packages periodically 'teleport' into the *top* of the inventory stack via a steampunk-themed visual effect (e.g., a portal, a pneumatic tube). The rate at which new packages arrive increases as the game progresses (linked to score or time), adding pressure to the sorting task.  
    * *Justification:* This method is simple to implement for a prototype, avoids complex pickup mechanics requiring precise driving, fits the steampunk theme ("teleportation" or automated loading), and keeps the player's focus on the core loop of sorting and delivering under time pressure. It draws inspiration from the abstract item management of *Wilmot's Warehouse*.  
* **Delivery:**  
  * **Trigger:** Successfully entering a delivery zone's trigger collider 5 while the package in the delivery slot matches the zone's required symbol.  
  * **Outcome:** Score points, package removed from inventory, success feedback (visual/audio).  
* **Scoring (Proposal):**  
  * **Successful Delivery:** \+100 points (base value). Consider adding a small bonus based on current speed or a streak bonus for consecutive successful deliveries without errors.  
  * **Incorrect Delivery Attempt:** Entering a zone with the wrong package selected results in a penalty: \-50 points. Include clear negative feedback (sound, brief visual effect like a red X). The package is *not* removed.  
  * **Missed Zone:** Driving past a zone without entering it results in no immediate score penalty for the prototype. This keeps the initial experience less punishing. Future iterations could track misses for difficulty or end-of-shift evaluation.  
  * **Obstacle Collision:** Lose one life (see Loss Condition). No direct score penalty, but losing lives limits playtime.  
    * *Justification:* Clear positive reinforcement for the core action. Moderate penalty for errors encourages careful sorting. Avoiding penalties for simply missing zones keeps the focus on active engagement rather than perfection, suitable for an arcade setting.  
* **Difficulty Scaling (Proposal):**  
  * **Primary:** Automatic increase in forward forwardSpeed over time/distance.  
  * **Secondary:**  
    * Increased Package Spawn Rate: Inventory fills faster, demanding quicker sorting and delivery.  
    * Increased Delivery Zone Frequency: Less downtime between required actions.  
    * Obstacle Density/Complexity: More hazards appear, requiring more skillful driving.  
    * (Advanced) Pattern Complexity: Introduce sequences of zones requiring more rapid or complex inventory cycling.  
    * *Justification:* Provides multiple scaling vectors for a smoother difficulty curve. Speed and package rate are fundamental and relatively easy to tune initially. Obstacles add direct driving challenge.  
* **Obstacles (Proposal):**  
  * **Type 1 (Damaging):** Static objects (e.g., crates dropped from above, potholes, stalled steampunk contraptions) and moving objects (e.g., cross-traffic automatons, rogue gears). Collision results in losing a life and brief invulnerability/visual feedback.  
  * **Type 2 (Disruptive \- Themed):** Obstacles that interact with the inventory mechanic. Example: Driving through a "magnetic field" or hitting a specific "jolt" obstacle could randomly shuffle the order of 2-3 packages within the inventory stack (excluding the currently selected one perhaps). Collision results in the shuffle effect and maybe a distinct sound, but *no life loss*.  
    * *Justification:* Damaging obstacles provide standard arcade challenge. Disruptive obstacles introduce a unique mechanical twist directly tied to the inventory sorting, fitting the theme ("steampunk malfunction") and adding gameplay depth beyond simple avoidance, inspired by the desire for mechanics to make sense within the world.  
* **Win/Loss Conditions (Proposal):**  
  * **Loss Condition:** The game ends when the player runs out of lives. Start with 3 lives. Lives are lost by colliding with damaging obstacles (Type 1). Display a "Game Over" screen with the final score and options to retry or return to the title screen.  
    * *Justification:* Standard, clear, and proven arcade loss condition. Easy to implement and understand.  
  * **Win Condition:** As an infinite scroller, the primary goal is achieving the highest possible score. There is no predefined "end" to the game.  
    * **Structural Element \- "Shifts":** To provide structure and short-term goals, the game can be divided into "Shifts" or "Days." A shift might end after reaching a certain score, distance, or number of successful deliveries. Completing a shift could display a "Shift Complete" screen with shift score, potentially award bonus points, and then transition seamlessly into the next shift, which starts at a higher base difficulty (speed, spawn rates).  
    * *Justification:* High score focus aligns with the infinite scroller genre and arcade inspiration (*Paperboy*, *Icy Tower*). "Shifts" provide measurable progress and reward without needing a finite win state, offering satisfying milestones during gameplay.  
* **Level Structure / Progression (Proposal):**  
  * **Environment:** Single, infinitely scrolling isometric track generated procedurally using chunks (Section 1.2).  
  * **Biome Changes (Icy Tower Inspiration):** Implement visual changes to the environment based on progression milestones (e.g., score thresholds like every 10,000 points, or shift completion). Swap the Tilemap assets, background elements, and potentially obstacle types used by the LevelGenerator. Example progression: Urban Cobblestone \-\> Industrial Factory Zone \-\> High-Altitude Sky-Docks \-\>???  
    * *Justification:* Directly addresses the user's stated inspiration from *Icy Tower*'s biome changes. Provides significant visual variety and reinforces the player's sense of progress within the infinite structure. Requires coordination with the art team for multiple asset sets.

### **2.4 Controls**

* **Arcade Cabinet (Primary):**  
  * **Steering Wheel:** Controls the truck's horizontal movement on the screen. Analog input mapped to position or velocity.  
  * **Rotary Wheel:** Cycles the highlighted item in the inventory stack. Input must be processed as discrete steps (up/down).  
  * **Button:** *Proposal:* Use the button to **confirm** a delivery *when inside a delivery zone*. While entering the zone with the correct item selected could be sufficient, requiring a button press adds a deliberate action and utilizes the provided hardware. Alternatively, reserve the button for activating potential future power-ups. *Decision for Prototype:* Implement delivery confirmation via button press. If inside a zone with the correct item selected, pressing the button triggers the delivery success logic. This makes the button integral to the core loop.  
* **Standalone PC (Keyboard Fallback):**  
  * **Steering:** A / D keys or Left Arrow / Right Arrow keys.  
  * **Inventory Cycle:** W / S keys or Up Arrow / Down Arrow keys.  
  * **Deliver/Action:** Spacebar or Enter key.  
  * *Note:* Keyboard controls provide functional parity for testing and potential standalone play but will lack the tactile feel and nuance of the physical arcade controls. The design should acknowledge this difference.

### **2.5 User Interface (UI)**

* **HUD (In-Game):**  
  * **Score:** Prominently displayed (e.g., Top Center). Use a steampunk-style font/counter.  
  * **Lives:** Displayed clearly (e.g., Top Left/Right using heart icons or truck icons).  
  * **Inventory Stack:** Vertical list of package icons on the right side. Bottom item clearly highlighted (e.g., brighter, outline, different background).29 UI elements should visually represent the package symbols. Use a Vertical Layout Group.18  
  * **Current Speed (Optional):** A small gauge or numerical display (Top/Bottom Corner).  
  * **Upcoming Zone Indicator (Optional):** A subtle off-screen marker or arrow indicating the symbol of the next approaching zone could be helpful but adds complexity; omit for initial prototype.  
* **Screen Flow:**  
  1. **Title Screen:** Game Title (e.g., *SteamWheel Deliveries*), "Press Button to Start" prompt, Display of Highest Score achieved locally. Steampunk background/theming.  
  2. **Game Screen:** Main gameplay view with the HUD active.  
  3. **Pause Screen:** Activated by a designated pause input (e.g., Esc key on PC, potentially a hidden button on cabinet?). Displays "Paused", options for "Resume" and "Quit to Title". Game state is frozen.  
  4. **Game Over Screen:** Appears when lives reach zero. Displays "Game Over\!", Final Score achieved, "Press Button to Retry", "Quit to Title".  
  5. **(Optional) Shift Complete Screen:** Appears after meeting shift criteria. Displays "Shift Complete\!", Score for the shift, Bonus points awarded, "Press Button for Next Shift".  
* **Style Notes:** Consistent steampunk aesthetic across all UI elements. Use textures suggesting brass, copper, wood, and rivets. Employ analogue gauge styles for numerical displays where appropriate. Font choice should be readable yet thematic (e.g., Victorian-inspired serif or sans-serif). Ensure high contrast and readability, especially given the potentially fast-paced gameplay.

### **2.6 Art & Audio Notes**

* **Art:**  
  * **Style:** 2D Isometric pixel art or vector art with a clear steampunk theme. Focus on mechanical details, gears, steam effects, Victorian architectural elements.  
  * **Assets Needed:** Player truck sprite (with potential damage states?), diverse package symbol sprites, delivery zone visuals matching symbols, multiple tile sets for different biomes (city, factory, skyways, etc.), obstacle sprites (static and animated), UI elements (buttons, panels, fonts, icons), visual effects (delivery success, failure, obstacle collision, package teleport spawn).  
  * **Priority:** Placeholder assets are needed immediately for all core gameplay elements (truck, basic road tile, one package type, one zone type) to enable prototype development. Final art to be provided by teammate.  
* **Audio:**  
  * **Music:** Upbeat, slightly mechanical-sounding background track with a steampunk feel. Music could potentially increase in tempo or intensity as game speed increases or during later shifts.  
  * **Sound Effects (SFX):** Critical for feedback. Need sounds for: Successful Delivery (positive chime/kerching), Incorrect Delivery (error buzz/clunk), Obstacle Collision (crash/bang), Inventory Cycle (click/ratchet sound), Package Spawn (teleport whoosh/pneumatic hiss), Button Press (mechanical click), Truck Engine (chugging sound, pitch potentially tied to speed), UI Navigation sounds.

### **2.7 Technical Considerations Summary**

* **Engine:** Unity 2D (User specified Unity 6).  
* **Core Technologies:** Isometric Tilemaps (Grid, Tilemap, potentially TilemapRenderer), 2D Physics (Rigidbody2D, Collider2D), UGUI (Canvas, Vertical Layout Group, Image, Text, Button, Content Size Fitter), Input System (InputActionAsset, potentially custom layouts or serial communication).  
* **Input:** Heavy reliance on custom arcade hardware. Requires robust solution for reading steering wheel, rotary encoder (discrete steps), and button. Prioritize Input System mapping, but prepare for Arduino/Serial communication (with asynchronous reading) as a likely necessity, especially for the encoder. Standalone keyboard fallback is required.  
* **Procedural Generation:** Chunk-based system for infinite scrolling environment. Needs logic for spawning/despawning chunks and integrating delivery zones/obstacles.  
* **Platform:** Custom Arcade Cabinet (Primary target), PC Standalone (Secondary target for development, testing, potential future release).

## **Conclusion & Next Steps**

This document provides a technical roadmap and design foundation for the *SteamWheel Deliveries* arcade game. The recommended approach prioritizes getting a functional core loop working quickly for the upcoming playtesting deadline.

* **Technical Focus:** Implement the standard Isometric Tilemap setup, a basic chunk-spawning system for infinite scrolling, Rigidbody2D for player movement (with iterative refinement of the isometric input translation), a Vertical Layout Group-based UI for the inventory stack, and trigger-based collision detection for deliveries.  
* **Design Decisions:** The proposed solutions—teleporting package acquisition, high-score focus with optional "Shifts," lives for loss condition, scaling difficulty via speed/spawn rates/obstacles, and biome changes for visual progression—aim for simplicity suitable for rapid prototyping while fitting the game's theme and arcade goals.  
* **Immediate Priority: Input Testing:** The most significant technical risk lies in interfacing with the custom arcade controls. **It is strongly recommended to immediately implement the Input Testing Plan (Section 1.6).** Build a separate small test scene to determine whether the steering wheel, rotary encoder, and button can be reliably read via the standard Input System or if the Arduino/Serial communication path (including asynchronous reading) is necessary. Resolving this uncertainty is paramount before investing significant time in other game systems.  
* **Prototype Scope:** For the initial playable prototype due next Thursday, focus *exclusively* on the core loop elements detailed in Part 1:  
  1. Basic isometric environment scrolling (even with repeating chunks).  
  2. Truck movement controlled by the steering wheel (get *some* form of translation working).  
  3. Inventory UI stack displaying placeholder packages.  
  4. Inventory cycling controlled by the rotary wheel (discrete steps).  
  5. Spawning simple delivery zones.  
  6. Basic delivery logic (trigger detection, symbol matching, item removal on success).  
  7. Placeholder score/feedback.  
* **Living Document:** Treat this GDD as a starting point. Use feedback from playtesting the prototype to refine mechanics, scoring, difficulty, and UI. Update the document as the design evolves.

This project presents unique challenges, particularly with the custom hardware integration, but also offers a great opportunity to create a distinctive arcade experience. Good luck with the development\!

#### **Works cited**

1. Creating an Isometric Tilemap \- Unity \- Manual, accessed April 16, 2025, [https://docs.unity3d.com/es/2018.4/Manual/Tilemap-Isometric-CreateIso.html](https://docs.unity3d.com/es/2018.4/Manual/Tilemap-Isometric-CreateIso.html)  
2. Create an Isometric Tilemap \- Unity \- Manual, accessed April 16, 2025, [https://docs.unity3d.com/6000.0/Documentation/Manual/tilemaps/work-with-tilemaps/isometric-tilemaps/create-isometric-tilemap.html](https://docs.unity3d.com/6000.0/Documentation/Manual/tilemaps/work-with-tilemaps/isometric-tilemaps/create-isometric-tilemap.html)  
3. Creating an Isometric Tilemap \- Unity \- Manual, accessed April 16, 2025, [https://docs.unity3d.com/es/2019.4/Manual/Tilemap-Isometric-CreateIso.html](https://docs.unity3d.com/es/2019.4/Manual/Tilemap-Isometric-CreateIso.html)  
4. Endless Runner Level Generator in Unity Tutorial (Spawn Level Parts FOREVER), accessed April 16, 2025, [https://m.youtube.com/watch?v=NtY\_R0g8L8E\&t=8m05s](https://m.youtube.com/watch?v=NtY_R0g8L8E&t=8m05s)  
5. Procedural Generation: Endless Runner Unity Tutorial (Updated 2023\) \- YouTube, accessed April 16, 2025, [https://m.youtube.com/watch?v=Ldyw5IFkEUQ\&pp=ygULI2NvcmVtdW5pdHk%3D](https://m.youtube.com/watch?v=Ldyw5IFkEUQ&pp=ygULI2NvcmVtdW5pdHk%3D)  
6. Tile Map & Spawn in 2d endless runner game unity | Basic tutorial \- YouTube, accessed April 16, 2025, [https://www.youtube.com/watch?v=pDbnSwZpWkE](https://www.youtube.com/watch?v=pDbnSwZpWkE)  
7. Endless Runner: Procedural Track Generation (Unity Tutorial) \- YouTube, accessed April 16, 2025, [https://www.youtube.com/watch?v=FjD\_DwbYYcs](https://www.youtube.com/watch?v=FjD_DwbYYcs)  
8. Infinite Parallax Scrolling Background \- Unity 2D Complete Tutorial \- YouTube, accessed April 16, 2025, [https://www.youtube.com/watch?v=AoRBZh6HvIk](https://www.youtube.com/watch?v=AoRBZh6HvIk)  
9. Introduction to Rigidbody 2D \- Unity \- Manual, accessed April 16, 2025, [https://docs.unity3d.com/6000.0/Documentation/Manual/2d-physics/rigidbody/introduction-to-rigidbody-2d.html](https://docs.unity3d.com/6000.0/Documentation/Manual/2d-physics/rigidbody/introduction-to-rigidbody-2d.html)  
10. Character Movement Unity. Isometric 2D Games \- YouTube, accessed April 16, 2025, [https://www.youtube.com/watch?v=zsgA1KaSlVE](https://www.youtube.com/watch?v=zsgA1KaSlVE)  
11. Mouse Click Movement in Isometric Tilemap \- Unity Tutorial \- YouTube, accessed April 16, 2025, [https://www.youtube.com/watch?v=b0AQg5ZTpac](https://www.youtube.com/watch?v=b0AQg5ZTpac)  
12. Input Manager \- Unity \- Manual, accessed April 16, 2025, [https://docs.unity3d.com/Manual/class-InputManager.html](https://docs.unity3d.com/Manual/class-InputManager.html)  
13. Using Steering Wheel in Unity \- Game Development Stack Exchange, accessed April 16, 2025, [https://gamedev.stackexchange.com/questions/126271/using-steering-wheel-in-unity](https://gamedev.stackexchange.com/questions/126271/using-steering-wheel-in-unity)  
14. Supported Input Devices | Input System | 1.3.0 \- Unity \- Manual, accessed April 16, 2025, [https://docs.unity3d.com/Packages/com.unity.inputsystem@1.3/manual/SupportedDevices.html](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.3/manual/SupportedDevices.html)  
15. Devices | Input System | 1.0.2 \- Unity \- Manual, accessed April 16, 2025, [https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Devices.html](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Devices.html)  
16. Arduino Steering Wheel for Unity : 6 Steps \- Instructables, accessed April 16, 2025, [https://www.instructables.com/Arduino-Steering-Wheel-for-Unity/](https://www.instructables.com/Arduino-Steering-Wheel-for-Unity/)  
17. UNITY PLAYER CONTROLLER QUESTION: For a top down shooter/bullet hell, is there a difference between Ridigbody2d's velocity and moveposition for player movement? : r/gamedev \- Reddit, accessed April 16, 2025, [https://www.reddit.com/r/gamedev/comments/108nbi4/unity\_player\_controller\_question\_for\_a\_top\_down/](https://www.reddit.com/r/gamedev/comments/108nbi4/unity_player_controller_question_for_a_top_down/)  
18. Vertical Layout Group | Unity UI | 1.0.0, accessed April 16, 2025, [https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-VerticalLayoutGroup.html](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-VerticalLayoutGroup.html)  
19. Unity \+ Ink: Part 3: Building an Interface \- Digital Ephemera, accessed April 16, 2025, [https://videlais.com/2019/07/09/unity-ink-part-3-building-an-interface/](https://videlais.com/2019/07/09/unity-ink-part-3-building-an-interface/)  
20. How change expanding direction of VerticalLayoutGroup in Unity without rotating?, accessed April 16, 2025, [https://stackoverflow.com/questions/55279138/how-change-expanding-direction-of-verticallayoutgroup-in-unity-without-rotating](https://stackoverflow.com/questions/55279138/how-change-expanding-direction-of-verticallayoutgroup-in-unity-without-rotating)  
21. Decomposing the Unity UI Automatic Layout System including Vertical, Horizontal, and Grid Layout Groups \- Thor Projects, accessed April 16, 2025, [https://thorprojects.com/2020/08/25/decomposing-the-unity-ui-automatic-layout-system-including-vertical-horizontal-and-grid-layout-groups/](https://thorprojects.com/2020/08/25/decomposing-the-unity-ui-automatic-layout-system-including-vertical-horizontal-and-grid-layout-groups/)  
22. The VLG component does not expand regardless of the length of the child \- Stack Overflow, accessed April 16, 2025, [https://stackoverflow.com/questions/68741641/the-vlg-component-does-not-expand-regardless-of-the-length-of-the-child](https://stackoverflow.com/questions/68741641/the-vlg-component-does-not-expand-regardless-of-the-length-of-the-child)  
23. Conflict between component prefabs and Unity UI serialization bug \- Ask \- GameDev.tv, accessed April 16, 2025, [https://community.gamedev.tv/t/conflict-between-component-prefabs-and-unity-ui-serialization-bug/231422](https://community.gamedev.tv/t/conflict-between-component-prefabs-and-unity-ui-serialization-bug/231422)  
24. I hate UI : r/Unity3D \- Reddit, accessed April 16, 2025, [https://www.reddit.com/r/Unity3D/comments/1292ecj/i\_hate\_ui/](https://www.reddit.com/r/Unity3D/comments/1292ecj/i_hate_ui/)  
25. How To Make Gaming Steering Wheel Using Rotary Encoder Module (KY-040) \- YouTube, accessed April 16, 2025, [https://www.youtube.com/watch?v=n6ScGobQycw](https://www.youtube.com/watch?v=n6ScGobQycw)  
26. Rotary encoder steering wheel \- General Guidance \- Arduino Forum, accessed April 16, 2025, [https://forum.arduino.cc/t/rotary-encoder-steering-wheel/681839](https://forum.arduino.cc/t/rotary-encoder-steering-wheel/681839)  
27. Unity Input System in Unity 6 (1/7): Input Action Editor \- YouTube, accessed April 16, 2025, [https://www.youtube.com/watch?v=TiTKAseu17A](https://www.youtube.com/watch?v=TiTKAseu17A)  
28. Extend an existing Library \- Using an Encoder in Unity \- Uduino ..., accessed April 16, 2025, [https://www.youtube.com/watch?v=qLKactcRtMI](https://www.youtube.com/watch?v=qLKactcRtMI)  
29. Unity UI and Object Highlighting Inventory Items After Pickup \- Adventure Creator, accessed April 16, 2025, [https://adventurecreator.org/forum/discussion/9852/unity-ui-and-object-highlighting-inventory-items-after-pickup](https://adventurecreator.org/forum/discussion/9852/unity-ui-and-object-highlighting-inventory-items-after-pickup)  
30. Serial Port reading with Unity 5 \- Interfacing w/ Software on the Computer \- Arduino Forum, accessed April 16, 2025, [https://forum.arduino.cc/t/serial-port-reading-with-unity-5/328446](https://forum.arduino.cc/t/serial-port-reading-with-unity-5/328446)  
31. Best practice for reading serial port in Unity3D version 2020.3.30f1? \- Stack Overflow, accessed April 16, 2025, [https://stackoverflow.com/questions/71344914/best-practice-for-reading-serial-port-in-unity3d-version-2020-3-30f1](https://stackoverflow.com/questions/71344914/best-practice-for-reading-serial-port-in-unity3d-version-2020-3-30f1)  
32. Asynchronous Serial Communication \- Alan Zucconi, accessed April 16, 2025, [https://www.alanzucconi.com/2016/12/01/asynchronous-serial-communication/](https://www.alanzucconi.com/2016/12/01/asynchronous-serial-communication/)  
33. Need Help with Serial Port Communication. : r/Unity3D \- Reddit, accessed April 16, 2025, [https://www.reddit.com/r/Unity3D/comments/5iu70j/need\_help\_with\_serial\_port\_communication/](https://www.reddit.com/r/Unity3D/comments/5iu70j/need_help_with_serial_port_communication/)  
34. Unity Serial.ReadLine() issue \- Stack Overflow, accessed April 16, 2025, [https://stackoverflow.com/questions/64809695/unity-serial-readline-issue](https://stackoverflow.com/questions/64809695/unity-serial-readline-issue)  
35. If you \*must\* use .NET System.IO.Ports.SerialPort \- Sparx Engineering, accessed April 16, 2025, [https://sparxeng.com/blog/software/must-use-net-system-io-ports-serialport](https://sparxeng.com/blog/software/must-use-net-system-io-ports-serialport)  
36. Unity \+ Serial Port (Part 2\) \- YouTube, accessed April 16, 2025, [https://www.youtube.com/watch?v=8GajTIYrVpQ](https://www.youtube.com/watch?v=8GajTIYrVpQ)  
37. Isometric Character Controller in Unity \- YouTube, accessed April 16, 2025, [https://www.youtube.com/watch?v=8ZxVBCvJDWk](https://www.youtube.com/watch?v=8ZxVBCvJDWk)