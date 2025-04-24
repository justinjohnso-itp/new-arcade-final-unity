#if UNITY_EDITOR // Important: Editor scripts only run in the Unity Editor
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.Tilemaps; // Use this namespace for GridBrush

[CreateAssetMenu(fileName = "New Large Tile Brush", menuName = "Brushes/Large Tile Brush")]
[CustomGridBrush(false, true, false, "Large Tile Brush")] // Defines brush behavior
public class LargeTileBrush : GridBrush
{
    // Assign the specific LargeIsometricTile asset this brush instance will paint via the Inspector.
    public LargeIsometricTile tileToPaint;

    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        // Use the tile assigned to this brush instance
        var largeTile = tileToPaint;

        if (largeTile == null)
        {
            Debug.LogWarning("LargeTileBrush: No LargeIsometricTile assigned to the brush asset in the Inspector. Cannot paint.");
            return; // Exit if no tile is assigned to the brush
        }

        Tilemap tilemap = brushTarget?.GetComponent<Tilemap>();
        if (tilemap == null) return;

        // Calculate the area the tile will cover based on its dimensions
        // Assuming 'position' is the anchor point (e.g., bottom-left). Adjust if needed.
        Vector3Int min = position;
        BoundsInt area = new BoundsInt(min, new Vector3Int(largeTile.width, largeTile.height, 1));

        // --- Clear the area first (Recommended) ---
        // Create an array of null tiles matching the size of the area
        TileBase[] nullTiles = new TileBase[area.size.x * area.size.y * area.size.z];
        tilemap.SetTilesBlock(area, nullTiles); // Clear the block before painting the anchor

        // --- Set the main tile at the anchor position ---
        // We use SetTile directly here instead of base.Paint to ensure
        // we are placing the specific 'tileToPaint'.
        tilemap.SetTile(position, largeTile);

        Debug.Log($"Painting Large Tile '{largeTile.name}' at {position}, covering area: {area}");
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
         Tilemap tilemap = brushTarget?.GetComponent<Tilemap>();
         if (tilemap == null) return;

         // Get the tile *currently on the map* at the erase position
         var existingTile = tilemap.GetTile(position);

         // Check if the existing tile is the one this brush is configured to paint
         // This assumes the 'position' is the anchor point of the large tile.
         var largeExistingTile = existingTile as LargeIsometricTile;

         // Erase the block only if the tile at the position matches the tile this brush is set up to paint.
         if (largeExistingTile != null && largeExistingTile == tileToPaint)
         {
             // If we are erasing the anchor of the correct LargeIsometricTile
             // Calculate bounds and clear the whole block
             Vector3Int min = position; // Assuming position is the anchor
             BoundsInt area = new BoundsInt(min, new Vector3Int(largeExistingTile.width, largeExistingTile.height, 1));

             // Create an array of null tiles matching the size of the area
             TileBase[] nullTiles = new TileBase[area.size.x * area.size.y * area.size.z];
             tilemap.SetTilesBlock(area, nullTiles); // Clear the block
             Debug.Log($"Erasing Large Tile '{largeExistingTile.name}' covering area: {area}");
         }
         else
         {
             // If it's not our large tile's anchor, or not the tile associated with this brush,
             // just erase the single cell using default behavior.
             base.Erase(gridLayout, brushTarget, position);
         }
    }
}
#endif
