using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("The Transform of the player to follow")]
    public Transform target;

    [Tooltip("Offset from the player's position (typically Z = -10 for 2D)")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }
}
