using UnityEngine;
using UnityEngine.EventSystems; // Required for UI detection

public class CameraController : MonoBehaviour
{
    [Header("Camera Boundaries")]
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -10f;
    public float maxY = 10f;

    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 2f;
    [SerializeField] private float maxZoom = 8f;
    [SerializeField] private float zoomSensitivity = 2f;
    [SerializeField] private float zoomLerpSpeed = 10f; // For smoothness

    private Vector3 dragOrigin;
    private Camera cam;
    private float targetZoom;
    private bool isPanning = false; // Tracks if a valid pan was initiated

    private void Start()
    {
        cam = GetComponent<Camera>();
        targetZoom = cam.orthographicSize;
    }

    private void LateUpdate()
    {
        PanCamera();
        ZoomCamera();
    }

    private void PanCamera()
    {
        // Start pan on Right Click (1)
        if (Input.GetMouseButtonDown(1))
        {
            // Block panning if hovering over a UI element
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                isPanning = false;
                return;
            }

            isPanning = true;
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        // Continue pan on Right Click hold, but ONLY if we started a valid pan
        if (Input.GetMouseButton(1) && isPanning)
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 targetPosition = cam.transform.position + difference;

            float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
            float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);

            cam.transform.position = new Vector3(clampedX, clampedY, targetPosition.z);
        }

        // Stop panning when Right Click is released
        if (Input.GetMouseButtonUp(1))
        {
            isPanning = false;
        }
    }

    private void ZoomCamera()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (scrollInput != 0)
        {
            // Optional: Block zooming if hovering over a UI element (useful for UI scroll lists)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // Calculate new target zoom
            targetZoom -= scrollInput * zoomSensitivity;
            // Clamp target zoom within limits
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // Smoothly transition to the target zoom
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomLerpSpeed);
    }
}