## April 24, 2025: Adding Buildings, Inventory, and Scoring

Continuing from the [previous session](https://justin-itp.notion.site/Final-Isometric-movement-procedural-tile-generation-1d89127f465d8056b807ffd7a9a89609?pvs=4) where I got basic isometric player movement and procedural road chunk generation working, this session focused on expanding the level generation to include buildings with delivery zones, implementing an inventory system for the player to carry items, and adding a scoring mechanism.

### Enabling Variable-Sized Tiles for Buildings

To spawn buildings, which are larger than the standard 1x1 road tiles, I first needed a way to handle these variable-sized assets within Unity's Tilemap system. The standard brush isn't designed for this.

I created a new script `Assets/Scripts/LargeIsometricTile.cs` inheriting from `TileBase`. The main change was overriding `GetTileData` to apply a scaling transformation based on the tile's `width` and `height` properties.

```csharp
// In LargeIsometricTile.cs
public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
{
    // ... (base setup)
    var m = tileData.transform;
    // Scale based on public width/height fields
    m.SetTRS(Vector3.zero, Quaternion.Euler(0f, 0f, 0f), new Vector3(width, height, 1f));
    tileData.transform = m;
    // ... (color, flags, collider)
}
```

To paint these in the editor, I made a custom brush (`Assets/Scripts/Editor/LargeTileBrush.cs`) and its editor script (`Assets/Scripts/Editor/LargeTileBrushEditor.cs`). The brush overrides `Paint` to check if the selected tile is a `LargeIsometricTile` and calculates an adjusted placement position based on its size before painting.

```csharp
// In LargeTileBrush.cs
public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
{
    var largeTile = brushCell.tile as LargeIsometricTile;
    if (largeTile != null && brushTarget != null)
    {
        // Adjust position based on tile size and isometric layout
        Vector3Int adjustedPosition = CalculateAdjustedPosition(position, largeTile);
        base.Paint(gridLayout, brushTarget, adjustedPosition);
    }
    else
    {
        base.Paint(gridLayout, brushTarget, position);
    }
}
```

*(Note: Figuring out the `CalculateAdjustedPosition` logic took some fiddling with the isometric offsets.)*

The editor script just provides an inspector preview.

### Expanding Procedural Generation: Buildings & Obstacles

With the large tile system in place, I expanded the existing `Assets/Scripts/LevelGenerator.cs`. The basic chunk spawning/despawning logic from the previous session remained, but I added functionality to populate the road chunks.

I added logic to spawn building prefabs (`Assets/Prefabs/Buildings/BlueHouse.prefab`, etc.) within designated areas (`BuildingSpawnArea_Top`, `BuildingSpawnArea_Bottom`) on the road chunks. These areas are defined by `PolygonCollider2D`s attached to child GameObjects within the road chunk prefab. The spawning logic tries to place buildings near the road edge using rejection sampling within the polygon collider and enforces a minimum spacing (`minBuildingSpacing`).

I also added obstacle spawning, similar to buildings. Road chunks have an `ObstacleSpawnArea` (also a `PolygonCollider2D`). The `SpawnObstaclesOnChunk` method picks random points within this area using `OverlapPoint` and instantiates obstacle prefabs (`Assets/Prefabs/Obstacle.prefab`) up to `maxObstaclesPerChunk`.

A simple `Assets/Scripts/Obstacle.cs` script was added to the obstacle prefab for collision handling later.

### Player Movement & Camera (No Changes)

The player movement (`Assets/Scripts/PlayerController.cs`) using `Rigidbody2D` and isometric direction vectors from `Assets/Scripts/GameSettings.cs`, as well as the camera follow script (`Assets/Scripts/CameraFollow.cs`), were already implemented in the previous session and didn't require changes here.

### Inventory System (Data, Logic, UI)

Deliveries require items, so I built an inventory system.

1.  **Item Data:** Created `Assets/Scripts/InventoryItemData.cs`, a `ScriptableObject` to define item properties like `itemName`, `icon` (Sprite), `itemColor`, `canStack`, and `maxStackSize`. These can be created as assets via `Assets > Create > Inventory > Item Data`.
2.  **Core Logic:** Created `Assets/Scripts/InventoryManager.cs` as a Singleton. It holds the actual inventory data in a `List<InventorySlotData>`. `InventorySlotData` is a simple class holding an `InventoryItemData` reference and a `quantity`.
    *   `AddItem`: Handles adding items, checking stackability (`itemData.canStack`), finding existing stacks with space, or adding to a new slot if `inventorySlots.Count < maxInventorySlots`.
    *   `RemoveItem`: Removes items, prioritizing later slots.
    *   `RotateInventory`: Shifts items up/down in the list.
    *   `ShuffleInventory`: Randomizes item order.
    *   `RemoveOldestItemAndScore`: Removes the item at index 0 (used for deliveries).
    *   `OnInventoryChanged`: A `System.Action` event triggered whenever the inventory changes.
3.  **UI Display:** Created `Assets/Scripts/InventoryUI.cs`. It references a parent `Transform` (`slotsParent`) and an `Assets/Prefabs/InventorySlot.prefab`. When `InventoryManager.OnInventoryChanged` is invoked, `UpdateUI` clears all existing slot GameObjects under `slotsParent` and reinstantiates new ones based on the current data from `InventoryManager.GetInventorySlots()`.
4.  **Slot Prefab:** The `InventorySlot.prefab` has the `Assets/Scripts/InventorySlot.cs` script. This script updates the `Image` for the icon and a `TextMeshProUGUI` for the quantity (only shown if `canStack` and `quantity > 1`). It also handles changing its background color for highlighting.
5.  **Player Input:** Added `Assets/Scripts/PlayerInventoryController.cs` which listens for specific key presses (W/S/Space) to call `InventoryManager.Instance.RotateInventory()` or `ShuffleInventory()`.

### Delivery Zones & Buildings

With items possible, I added delivery points.

In `LevelGenerator.cs`, I added logic to spawn building prefabs (`Assets/Prefabs/Buildings/BlueHouse.prefab`, etc.) within designated areas (`BuildingSpawnArea_Top`, `BuildingSpawnArea_Bottom`) on the road chunks. Similar to obstacles, these areas are defined by `PolygonCollider2D`s. The spawning logic tries to place buildings near the road edge and enforces a minimum spacing (`minBuildingSpacing`).

Each building prefab has an inactive child `GameObject` named `DeliveryZone_Placeholder`. This placeholder has the `Assets/Scripts/DeliveryZone.cs` script attached.

The `DeliveryZone` script has an `ActivateZone(Color requiredColor)` method. When called, it sets its `RequiredColor` property, activates its `GameObject`, and sets the color of a `SpriteRenderer` (`zoneVisual`) to the `requiredColor`.

Back in `LevelGenerator.cs`, after a building is instantiated, `TryActivateDeliveryZone` is called. This function checks:
*   If the current group of chunks has reached its zone limit (`zonesInCurrentGroup < maxZonesPerGroup`).
*   Gets the unique colors of items currently in the player's inventory using `InventoryManager.Instance.GetInventorySlots()...Select(...).Distinct()`. (This required adding `using System.Linq;`).
*   If colors are available and a random chance (`deliveryZoneActivationChance`) passes, it picks a random color *from the available inventory colors*, finds the `DeliveryZone_Placeholder` child, gets the `DeliveryZone` component, and calls `ActivateZone` with the chosen color.

```csharp
// In LevelGenerator.cs - TryActivateDeliveryZone()
List<Color> availableColors = currentInventory
    .Where(slotData => slotData?.itemData != null)
    .Select(slotData => slotData.itemData.itemColor)
    .Distinct()
    .ToList();

if (availableColors.Count > 0 && Random.value <= deliveryZoneActivationChance)
{
    // ... find placeholder ...
    Color chosenColor = availableColors[Random.Range(0, availableColors.Count)];
    zoneScript.ActivateZone(chosenColor);
    zonesInCurrentGroup++;
}
```

The actual delivery interaction happens when the player enters the zone's trigger. I added logic (likely intended for `DeliveryZone.OnTriggerEnter2D`, though might need review) where the zone checks if the player `HasPackage()` (method added to `PlayerController`). If yes, it calls `InventoryManager.Instance.RemoveOldestItemAndScore(RequiredColor)`. This method removes the item at index 0 and compares its `itemColor` to the zone's `RequiredColor` to determine the score to add.

### Scoring System

Finally, I implemented scoring.

Created `Assets/Scripts/ScoreManager.cs` as another Singleton. It holds `currentScore` and has an `AddScore(int amount)` method. It also has an `Action<int> OnScoreChanged` event.

Created `Assets/Scripts/ScoreUI.cs`. It gets a reference to a `TextMeshProUGUI` component. In `Start`, it subscribes to `ScoreManager.Instance.OnScoreChanged` and updates the text immediately. The `UpdateScoreText(int newScore)` method formats the display (`"Score: {newScore}"`).

Integrated scoring into gameplay:
*   In `InventoryManager.RemoveOldestItemAndScore`, `scoreManager.AddScore()` is called with 100 points for a correct color match and 10 points for a mismatch.
*   In `PlayerController.OnCollisionEnter2D`, if the collision is with an object tagged "Obstacle", `scoreManager.AddScore(-5)` is called.

### Code Cleanup

Lastly, I went through all the scripts created (`LevelGenerator.cs`, `PlayerController.cs`, `InventoryManager.cs`, `ScoreManager.cs`, etc.) and cleaned up comments, removing redundant explanations, commented-out code, and simplifying summaries to improve readability.
