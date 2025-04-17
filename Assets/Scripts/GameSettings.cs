using UnityEngine;

/// Global game settings accessible across all scripts.
public static class GameSettings
{
    public static readonly Vector2 ForwardDirection;
    public static readonly Vector2 IsometricYDirection;
    
    static GameSettings()
    {
        // Standard isometric projection uses 2:1 ratio (x:y)
        // This creates a 26.57° angle from horizontal (not 30° which would be a true dimetric projection)
        Vector2 isoXAxis = new Vector2(2f, 1f).normalized;
        Vector2 isoYAxis = new Vector2(2f, -1f).normalized; // Perpendicular to X in isometric view
        
        ForwardDirection = isoXAxis;
        IsometricYDirection = isoYAxis;
    }
}
