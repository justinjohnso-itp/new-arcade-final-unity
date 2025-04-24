using UnityEngine;

/// Global game settings accessible across all scripts.
public static class GameSettings
{
    public static readonly Vector2 ForwardDirection;
    public static readonly Vector2 IsometricYDirection;
    
    static GameSettings()
    {
        // Standard isometric projection (2:1 ratio)
        Vector2 isoXAxis = new Vector2(2f, 1f).normalized;
        Vector2 isoYAxis = new Vector2(2f, -1f).normalized; // Perpendicular in isometric view
        
        ForwardDirection = isoXAxis;
        IsometricYDirection = isoYAxis;
    }
}
