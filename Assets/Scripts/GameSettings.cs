using UnityEngine;

/// Global game settings accessible across all scripts.
public static class GameSettings
{
    /// Direction of forward travel: 15° more east than true northeast (30° from east axis instead of 45°).
  
    public static readonly Vector3 ForwardDirection;
    /// 2D version of forward direction (for 2D components).
  
    public static readonly Vector2 ForwardDirection2D;
    
    static GameSettings()
    {
        // Calculate direction: From east axis
        float angleDegrees = 26.6f;
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        
        // Create both 3D and 2D versions
        ForwardDirection = new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0f).normalized;
        ForwardDirection2D = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)).normalized;
    }
}
