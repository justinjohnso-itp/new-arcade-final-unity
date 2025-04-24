#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine.Tilemaps;

// This attribute lets Unity know to use this class to draw the Inspector for LargeTileBrush assets.
[CustomEditor(typeof(LargeTileBrush))]
public class LargeTileBrushEditor : GridBrushEditor
{
    private LargeTileBrush largeBrush { get { return target as LargeTileBrush; } }

    // Override OnInspectorGUI to draw the custom Inspector
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector fields (like the 'Tile To Paint' field)
        base.OnInspectorGUI();

        // Add a separator for clarity
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Assigned Tile Info", EditorStyles.boldLabel);

        // Check if a tile is assigned to the brush
        if (largeBrush.tileToPaint != null)
        {
            // Display read-only info about the assigned tile
            EditorGUILayout.LabelField("Name:", largeBrush.tileToPaint.name);
            EditorGUILayout.LabelField("Dimensions:", $"{largeBrush.tileToPaint.width} x {largeBrush.tileToPaint.height}");

            // Show a preview of the tile's sprite
            if (largeBrush.tileToPaint.largeSprite != null)
            {
                EditorGUILayout.LabelField("Sprite Preview:");
                // Calculate rect for preview, maintaining aspect ratio
                float aspectRatio = (float)largeBrush.tileToPaint.largeSprite.rect.width / largeBrush.tileToPaint.largeSprite.rect.height;
                float previewHeight = 100f;
                float previewWidth = previewHeight * aspectRatio;
                Rect previewRect = EditorGUILayout.GetControlRect(false, previewHeight, GUILayout.Width(previewWidth));
                
                // Draw the sprite preview
                if (Event.current.type == EventType.Repaint)
                {
                    var tex = largeBrush.tileToPaint.largeSprite.texture;
                    var texCoords = largeBrush.tileToPaint.largeSprite.textureRect;
                    texCoords.x /= tex.width;
                    texCoords.y /= tex.height;
                    texCoords.width /= tex.width;
                    texCoords.height /= tex.height;
                    GUI.DrawTextureWithTexCoords(previewRect, tex, texCoords, true);
                }
            }
            else
            {
                EditorGUILayout.LabelField("Sprite Preview:", "(No sprite assigned)");
            }

            // Add a button to quickly select the tile asset in the Project window
            if (GUILayout.Button("Select Tile Asset"))
            {
                Selection.activeObject = largeBrush.tileToPaint;
                EditorGUIUtility.PingObject(largeBrush.tileToPaint);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No LargeIsometricTile assigned to this brush. Assign a tile to the 'Tile To Paint' field above.", MessageType.Warning);
        }
    }

    // You might override other methods like OnPaintInspectorGUI if you want 
    // custom controls specifically when painting in the scene view, but 
    // OnInspectorGUI affects the Inspector when the brush *asset* is selected.
}
#endif
