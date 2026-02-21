using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Boundaries")]
    [Tooltip("The furthest left the camera can go")]
    public float minX = -10f;
    [Tooltip("The furthest right the camera can go")]
    public float maxX = 10f;
    [Tooltip("The furthest down the camera can go")]
    public float minY = -10f;
    [Tooltip("The furthest up the camera can go")]
    public float maxY = 10f;

    private Vector3 dragOrigin;
    private Camera cam;

    private void Start()
    {
        // Cache the camera component attached to this GameObject
        cam = GetComponent<Camera>(); 
    }

    // Using LateUpdate for camera movement ensures the player/game logic moves first, 
    // preventing jitteriness.
    private void LateUpdate()
    {
        PanCamera();
    }

    private void PanCamera()
    {
        // 1. When the player FIRST clicks, record that exact point in the game world
        if (Input.GetMouseButtonDown(0))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        // 2. While the player HOLDS the button, calculate the difference and move
        if (Input.GetMouseButton(0))
        {
            // Calculate how far the mouse has moved from the original click point
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);

            // Calculate where the camera WANTS to go based on the drag
            Vector3 targetPosition = cam.transform.position + difference;

            // 3. Clamp the target position so it doesn't exceed your map bounds
            float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
            float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);

            // Apply the new, clamped position back to the camera. 
            // We keep the original Z position so we don't accidentally move the camera forward/backward.
            cam.transform.position = new Vector3(clampedX, clampedY, targetPosition.z);
        }
    }
}