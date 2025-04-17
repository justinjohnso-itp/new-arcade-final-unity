# README

## Overview

This project is an isometric infinite-scrolling delivery game designed for a custom arcade cabinet. Pilot a truck, use a physical steering wheel to navigate, a rotary wheel to sort packages, and a button to make deliveries against the clock.

**Core Loop:** Drive -> Observe Zones -> Sort Packages -> Steer -> Deliver -> Survive -> Repeat!

**Genre:** Arcade Delivery / Action, Isometric Infinite Scroller
**Platform:** Custom Arcade Cabinet (Primary), PC Standalone (Secondary)

## Key Features

*   **Physical Arcade Controls:** Designed around a steering wheel, rotary encoder, and button.
*   **Isometric Infinite Scrolling:** Procedurally generated track using tilemap chunks.
*   **Package Sorting Mechanic:** Use the rotary wheel to manage a visual inventory stack and match packages to delivery zones.
*   **Increasing Difficulty:** Game speed and challenge ramp up over time.

## Technical Stack

*   **Engine:** Unity (Targeting Unity 6)
*   **Core:** Unity 2D, Isometric Tilemaps, Rigidbody2D Physics, UGUI, Input System
*   **Input Handling:** Unity Input System (primary attempt), potential Arduino + Serial Communication (especially for rotary encoder).

## Setup & Development Instructions

### Prerequisites

*   **Unity Hub:** Install Unity Hub ([https://unity.com/download](https://unity.com/download)).
*   **Unity Editor:** Use Unity Hub to install the recommended Unity version for this project (confirm version, likely a 2022.3 LTS or newer, potentially Unity 6 if specified). Ensure "Linux Build Support (IL2CPP)" or relevant platform support is included if building for a Linux-based cabinet OS.
*   **Git & Git LFS:** Install Git ([https://git-scm.com/](https://git-scm.com/)) and Git Large File Storage ([https://git-lfs.github.com/](https://git-lfs.github.com/)) for handling large assets.
*   **(Optional - For Arcade Controls):** Arduino IDE ([https://www.arduino.cc/en/software](https://www.arduino.cc/en/software)) if using Arduino for input handling.

### Cloning the Repository

```bash
# Clone the repository
git clone <repository-url>
cd <repository-folder>

# Pull large files managed by Git LFS
git lfs pull
```

### Developer Setup

1.  **Open Project:** Launch Unity Hub, click "Open", and navigate to the cloned repository folder. Unity will import the project.
2.  **Core Scripts:** Primary game logic resides in `Assets/Scripts/`. Key files include `PlayerController.cs`, `LevelGenerator.cs`, `InventoryManager.cs` (or similar).
3.  **Input System:**
    *   Familiarize yourself with the Input Action Asset located likely in `Assets/Settings/` or `Assets/Input/`.
    *   **CRITICAL:** Test input mapping early. Use the Input Debugger (`Window > Analysis > Input Debugger`) to check if arcade controls are recognized.
    *   If using Arduino/Serial:
        *   Ensure the correct COM port and baud rate are set in the relevant Unity script (e.g., `SerialController.cs`).
        *   Upload the corresponding `.ino` sketch to the Arduino board.
        *   Refer to the asynchronous serial reading implementation (likely using `System.IO.Ports` and `System.Threading`).
4.  **Running:** Press the Play button in the Unity Editor to run the game. Use keyboard fallbacks (A/D, W/S, Space) if arcade controls are not connected/configured.
5.  **Debugging:** Use `Debug.Log()` statements in scripts and view output in the Unity Console window (`Window > General > Console`).

### Designer / Artist Setup

1.  **Open Project:** Launch Unity Hub, click "Open", and navigate to the cloned repository folder. Unity will import the project.
2.  **Scene View:** Use the Scene view (`Window > General > Scene`) to navigate the game world visually.
3.  **Tilemaps & Tiles:**
    *   Environment chunks are built using Isometric Tilemaps. Find Tile Palettes under `Window > 2D > Tile Palette`.
    *   Tile assets (sprites) are likely located in `Assets/Art/Tiles` or `Assets/Tiles`.
    *   Modify existing chunk prefabs or create new ones using the Tile Palette.
4.  **Prefabs:**
    *   **Chunks:** Reusable level sections are located in `Assets/Prefabs/Chunks/` (or similar). Edit these to change layout, add scenery, or define spawn points.
    *   **Gameplay Elements:** Prefabs for Delivery Zones, Obstacles, Packages are likely in `Assets/Prefabs/Gameplay/`.
    *   **UI:** UI elements (inventory items, HUD components) are in `Assets/Prefabs/UI/`.
5.  **UI (UGUI):**
    *   The Heads-Up Display (HUD) and Inventory Stack are built using Unity's UI system. Find the Canvas object in the Hierarchy view.
    *   Modify UI layout, images, and text components directly in the Scene view or Prefab editor. The Inventory Stack likely uses a `Vertical Layout Group`.
6.  **Art Assets:** Place new or updated art assets (sprites, textures) into appropriate folders under `Assets/Art/`. Ensure import settings (e.g., Pixels Per Unit, Sprite Mode) are correct.
7.  **Running:** Press the Play button in the Unity Editor to see changes in action.
