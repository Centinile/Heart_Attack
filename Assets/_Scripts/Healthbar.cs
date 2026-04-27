using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject barRoot; // The root object to show/hide

    [Header("Settings")]
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color lowColor = Color.red;
    [SerializeField] private float lowThreshold = 0.3f; // Below this % = red

    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        Hide(); // Always start hidden
    }

    void LateUpdate()
    {
        // Keep healthbar facing the camera (important for world space canvas)
        if (mainCam != null)
            transform.rotation = mainCam.transform.rotation;
    }

    public void UpdateBar(float current, float max)
    {
        if (max <= 0) return;

        float percent = Mathf.Clamp01(current / max);

        if (percent >= 1f)
        {
            Hide();
            return;
        }

        Show();
        fillImage.fillAmount = percent;
        fillImage.color = Color.Lerp(lowColor, fullColor, 
            Mathf.InverseLerp(0f, lowThreshold, percent) );
    }

    private void Show() => barRoot.SetActive(true);
    private void Hide() => barRoot.SetActive(false);
}