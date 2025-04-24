using UnityEngine;
using UnityEngine.Tilemaps;

// Makes this scriptable object appear in the Assets -> Create menu
[CreateAssetMenu(fileName = "New Large Isometric Tile", menuName = "Tiles/Large Isometric Tile")]
public class LargeIsometricTile : TileBase
{
    [Header("Tile Settings")]
    [Tooltip("The main sprite for the large tile (e.g., a 5x5 building footprint).")]
    public Sprite largeSprite;

    [Tooltip("The type of collider to generate for this tile.")]
    public Tile.ColliderType colliderType = Tile.ColliderType.Sprite;

    [Header("Dimensions")]
    [Tooltip("How many cells wide the tile occupies.")]
    public int width = 1; // Default to 1, set in Inspector
    [Tooltip("How many cells high the tile occupies.")]
    public int height = 1; // Default to 1, set in Inspector

    // This is the core method. It tells the Tilemap what to render for a specific cell.
    // For a large tile, we provide the same data for all cells it's supposed to cover,
    // but the TilemapRenderer is smart enough to usually only draw the sprite once
    // when anchored correctly.
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        // Assign the large sprite
        tileData.sprite = largeSprite;

        // Set default color (can be customized)
        tileData.color = Color.white;

        // Set collider type based on the public variable
        tileData.colliderType = colliderType;

        // Important flags for isometric or custom behavior:
        // LockTransform keeps the sprite from rotating with the cell (usually desired)
        tileData.flags = TileFlags.LockTransform;

        // Default transform matrix (identity).
        // You might need to adjust this if your sprite pivot isn't
        // centered or if you want precise offset control relative to the anchor cell.
        // Start with identity and adjust if needed based on sprite pivot and grid settings.
        tileData.transform = Matrix4x4.identity;
    }

    // Optional: RefreshTile can be used if the tile needs to affect neighbors,
    // but for a simple large static sprite, it's often not strictly necessary.
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        // Refresh the area covered by the tile if needed.
        // For a 5x5 tile, you might refresh a 6x6 or larger area
        // if it could visually affect neighbors.
        // For simplicity, just refresh the base position for now.
        tilemap.RefreshTile(position);
    }

     // Optional: StartUp runs when the tile is added at runtime or map loads.
     // Not typically used for controlling editor painting behavior.
    public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
    {
        // Return true to indicate successful startup.
        return true;
    }
}
