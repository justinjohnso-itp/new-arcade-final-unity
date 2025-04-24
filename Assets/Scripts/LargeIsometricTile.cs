using UnityEngine;
using UnityEngine.Tilemaps;

// Makes this scriptable object appear in the Assets -> Create menu
[CreateAssetMenu(fileName = "New Large Isometric Tile", menuName = "Tiles/Large Isometric Tile")]
public class LargeIsometricTile : TileBase
{
    [Header("Tile Settings")]
    [Tooltip("The main sprite for the large tile.")]
    public Sprite largeSprite;

    [Tooltip("The type of collider to generate for this tile.")]
    public Tile.ColliderType colliderType = Tile.ColliderType.Sprite;

    [Header("Dimensions")]
    [Tooltip("How many cells wide the tile occupies.")]
    public int width = 1;
    [Tooltip("How many cells high the tile occupies.")]
    public int height = 1;

    // Provides rendering data for the tilemap.
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = largeSprite;
        tileData.color = Color.white;
        tileData.colliderType = colliderType;
        // LockTransform keeps the sprite from rotating with the cell.
        tileData.flags = TileFlags.LockTransform;
        // Start with identity transform; adjust if sprite pivot requires offset.
        tileData.transform = Matrix4x4.identity;
    }

    // Refreshes the tile and potentially neighbors if needed.
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        // For simplicity, just refresh the base position.
        // Expand the refresh area if the tile visually affects neighbors.
        tilemap.RefreshTile(position);
    }

     // Runs when the tile is added at runtime or map loads.
    public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
    {
        return true;
    }
}
