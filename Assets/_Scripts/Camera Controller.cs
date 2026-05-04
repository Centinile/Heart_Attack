using UnityEngine;
using UnityEngine.EventSystems;

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
    [SerializeField] private float zoomLerpSpeed = 10f;

    [Header("WASD Pan Settings")]
    [SerializeField] private float wasdPanSpeed = 8f;

    private Vector3 dragOrigin;
    private Camera cam;
    private float targetZoom;
    private bool isPanning = false;

    private void Start()
    {
        cam = GetComponent<Camera>();
        targetZoom = cam.orthographicSize;
    }

    private void LateUpdate()
    {
        WASDPan();
        MouseDragPan();
        ZoomCamera();
    }

    // ── WASD panning ────────────────────────────────────────────────────

    private void WASDPan()
    {
        // Don't move camera if typing in a UI field
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            var sel = EventSystem.current.currentSelectedGameObject;
            if (sel.GetComponent<TMPro.TMP_InputField>() != null ||
                sel.GetComponent<UnityEngine.UI.InputField>() != null)
                return;
        }

        float h = 0f;
        float v = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h =  1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  v = -1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    v =  1f;

        if (h == 0f && v == 0f) return;

        // Scale speed by current zoom so panning feels consistent at all zoom levels
        float scaledSpeed = wasdPanSpeed * (cam.orthographicSize / 5f);

        Vector3 move = new Vector3(h, v, 0f) * scaledSpeed * Time.deltaTime;
        Vector3 target = cam.transform.position + move;

        cam.transform.position = new Vector3(
            Mathf.Clamp(target.x, minX, maxX),
            Mathf.Clamp(target.y, minY, maxY),
            cam.transform.position.z);
    }

    // ── Right-click drag pan (unchanged) ────────────────────────────────

    private void MouseDragPan()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                isPanning = false;
                return;
            }

            isPanning = true;
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1) && isPanning)
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 targetPosition = cam.transform.position + difference;

            cam.transform.position = new Vector3(
                Mathf.Clamp(targetPosition.x, minX, maxX),
                Mathf.Clamp(targetPosition.y, minY, maxY),
                targetPosition.z);
        }

        if (Input.GetMouseButtonUp(1))
            isPanning = false;
    }

    // ── Scroll zoom (unchanged) ─────────────────────────────────────────

    private void ZoomCamera()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (scrollInput != 0)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            targetZoom -= scrollInput * zoomSensitivity;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomLerpSpeed);
    }
}