using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AchievementPopup : MonoBehaviour
{
    public static AchievementPopup Instance;

    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text achievementNameText;
    [SerializeField] private TMP_Text achievementDescriptionText;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private RectTransform _panelRect;
    private Vector2 _visiblePosition;
    private Vector2 _hiddenPosition;
    private Coroutine _slideCoroutine;

    private Queue<AchievementDefinition> _queue = new Queue<AchievementDefinition>();
    private bool _isShowing = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        _panelRect = popupPanel.GetComponent<RectTransform>();
        _visiblePosition = _panelRect.anchoredPosition;
        popupPanel.SetActive(false);
    }

    private void Start()
    {
        // Hidden position — slide in from top
        _hiddenPosition = _visiblePosition + new Vector2(0f, _panelRect.rect.height + 50f);
        _panelRect.anchoredPosition = _hiddenPosition;
    }

    private void OnEnable()  => AchievementManager.OnAchievementUnlocked += Enqueue;
    private void OnDisable() => AchievementManager.OnAchievementUnlocked -= Enqueue;

    private void Enqueue(AchievementDefinition def)
    {
        _queue.Enqueue(def);
        if (!_isShowing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        _isShowing = true;

        while (_queue.Count > 0)
        {
            AchievementDefinition def = _queue.Dequeue();

            achievementNameText.text        = def.Name;
            achievementDescriptionText.text = def.Description;

            yield return StartCoroutine(SlideTo(_hiddenPosition, _visiblePosition)); // slide in
            yield return new WaitForSecondsRealtime(displayDuration);
            yield return StartCoroutine(SlideTo(_visiblePosition, _hiddenPosition)); // slide out

            popupPanel.SetActive(false);
        }

        _isShowing = false;
    }

    private IEnumerator SlideTo(Vector2 from, Vector2 to)
    {
        popupPanel.SetActive(true);
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; // works during pause/timescale 0
            float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
            _panelRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        _panelRect.anchoredPosition = to;
    }
}