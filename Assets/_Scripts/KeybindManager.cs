using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class KeybindManager : MonoBehaviour
{
    [System.Serializable]
    public class TabData
    {
        public string tabName;
        public Toggle tabToggle;           // The Toggle button itself (Defense Tower Tab / Resource Tower Tab)
        public BuildingData[] towerSlots;  // index 0 = key 1, index 1 = key 2, etc.
    }

    [Header("Tower Tabs")]
    [Tooltip("Assign the TOGGLE components (Defense Tower Tab, Resource Tower Tab) — NOT the page views.")]
    [SerializeField] private TabData[] tabs;

    [Header("Pause")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private SceneController sceneController;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private TowerSelectionUI _towerSelectionUI;
    private BuildingSelector _buildingSelector;
    private int _currentTabIndex = 0;
    private static readonly KeyCode[] NumberKeys =
    {
        KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
        KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6,
        KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9
    };

    void Start()
    {
        _towerSelectionUI = Object.FindFirstObjectByType<TowerSelectionUI>();
        _buildingSelector = Object.FindFirstObjectByType<BuildingSelector>();
        if (pausePanel != null) pausePanel.SetActive(false);

        // Let Unity's layout finish, then activate the first tab
        StartCoroutine(InitialTabSetup());
    }

    private IEnumerator InitialTabSetup()
    {
        yield return null;
        yield return null;
        ActivateTab(0);
    }

    void Update()
    {
        if (IsTyping()) return;
        HandleTabSwitch();
        HandleNumberKeys();
        HandleRightClickDeselect();
        HandlePause();
    }

    // ── Tab switching ─────────────────────────────────────────────────

    private void HandleTabSwitch()
    {
        if (!Input.GetKeyDown(KeyCode.Tab)) return;
        if (tabs == null || tabs.Length == 0) return;
        int dir = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? -1 : 1;
        ActivateTab((_currentTabIndex + dir + tabs.Length) % tabs.Length);
    }

    /// <summary>
    /// Activates a tab by programmatically setting its Toggle to isOn = true.
    /// This lets Unity's own Toggle Group handle showing/hiding the page views,
    /// exactly as if the player clicked the button.
    /// </summary>
    private void ActivateTab(int index)
    {
        if (tabs == null || index < 0 || index >= tabs.Length) return;
        _currentTabIndex = index;

        Toggle toggle = tabs[index].tabToggle;
        if (toggle != null && !toggle.isOn)
        {
            toggle.isOn = true;  // Toggle Group automatically turns off the others
        }
    }

    // ── Number keys 1-9 ───────────────────────────────────────────────

    private void HandleNumberKeys()
    {
        if (_towerSelectionUI == null || tabs == null) return;
        BuildingData[] slots = tabs[_currentTabIndex].towerSlots;
        if (slots == null) return;
        for (int i = 0; i < NumberKeys.Length && i < slots.Length; i++)
        {
            if (Input.GetKeyDown(NumberKeys[i]))
            {
                if (slots[i] != null)
                    _towerSelectionUI.SelectStructure(slots[i]);
                break;
            }
        }
    }

    // ── Right-click deselect ──────────────────────────────────────────

    private void HandleRightClickDeselect()
    {
        if (!Input.GetMouseButtonDown(1)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (TowerSelectionUI.SelectedStructureData != null) return;
        if (_buildingSelector != null && BuildingSelector.SelectedBuilding != null)
            _buildingSelector.Deselect();
    }

    // ── ESC pause ─────────────────────────────────────────────────────

    private void HandlePause()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (GameManager.Instance.currentState == GameManager.GameState.Paused)
            GameManager.Instance.ResumeGame();
        else
            GameManager.Instance.PauseGame();
    }

    public void Resume()
    {
        GameManager.Instance.ResumeGame();
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        if (sceneController != null)
            sceneController.SceneChange(mainMenuSceneName);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private bool IsTyping()
    {
        var sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (sel == null) return false;
        return sel.GetComponent<TMPro.TMP_InputField>() != null
            || sel.GetComponent<UnityEngine.UI.InputField>() != null;
    }
}