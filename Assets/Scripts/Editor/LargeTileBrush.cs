#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor.Tilemaps;

[CreateAssetMenu(fileName = "New Large Tile Brush", menuName = "Brushes/Large Tile Brush")]
[CustomGridBrush(false, true, false, "Large Tile Brush")]
public class LargeTileBrush : GridBrush
{
    public LargeIsometricTile tileToPaint;

    public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
        var largeTile = tileToPaint;

        if (largeTile == null)
        {
            Debug.LogWarning("LargeTileBrush: No LargeIsometricTile assigned to the brush asset. Cannot paint.");
            return;
        }

        Tilemap tilemap = brushTarget?.GetComponent<Tilemap>();
        if (tilemap == null) return;

        Vector3Int min = position;
        BoundsInt area = new BoundsInt(min, new Vector3Int(largeTile.width, largeTile.height, 1));

        // Clear the area before placing the new tile
        TileBase[] nullTiles = new TileBase[area.size.x * area.size.y * area.size.z];
        tilemap.SetTilesBlock(area, nullTiles);

        // Set the main tile at the anchor position.
        // We use SetTile directly here instead of base.Paint to ensure
        // we are placing the specific 'tileToPaint'.
        tilemap.SetTile(position, largeTile);

        Debug.Log($"Painting Large Tile '{largeTile.name}' at {position}, covering area: {area}");
    }

    public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
    {
         Tilemap tilemap = brushTarget?.GetComponent<Tilemap>();
         if (tilemap == null) return;

         var existingTile = tilemap.GetTile(position);
         var largeExistingTile = existingTile as LargeIsometricTile;

         // Erase the block only if the tile at the anchor position matches the tile this brush paints.
         if (largeExistingTile != null && largeExistingTile == tileToPaint)
         {
             Vector3Int min = position;
             BoundsInt area = new BoundsInt(min, new Vector3Int(largeExistingTile.width, largeExistingTile.height, 1));

             TileBase[] nullTiles = new TileBase[area.size.x * area.size.y * area.size.z];
             tilemap.SetTilesBlock(area, nullTiles);
             Debug.Log($"Erasing Large Tile '{largeExistingTile.name}' covering area: {area}");
         }
         else
         {
             // Fallback to default single-cell erase behavior.
             base.Erase(gridLayout, brushTarget, position);
         }
    }
}
#endif
