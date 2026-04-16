using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Header("Panel Reference")]
    public GameObject towerInfoPanel;

    [Header("Buttons")]
    public Button upgradeButton;
    public Button sellButton;

    [Header("Settings")]
    public LayerMask towerLayer;

    [Header("Text Fields")]
    public TextMeshProUGUI towerNameText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI damageText;
    public TextMeshProUGUI attackCDText;
    public TextMeshProUGUI rangeText;
    public TextMeshProUGUI projectileSpeedText;
    public TextMeshProUGUI nutrientsText;
    public TextMeshProUGUI hydrationText;
    public TextMeshProUGUI upgradeCostText;

    void Awake() => instance = this;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // If the panel is hidden, do nothing
            if (!towerInfoPanel.activeSelf) return;

            // Do not close if clicking UI elements (buttons/panel)
            if (EventSystem.current.IsPointerOverGameObject()) return;

            // Do not close if clicking a structure
            if (IsClickingStructure()) return;

            HideInfo();
        }
    }

    private bool IsClickingStructure()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // Check for 2D colliders
        RaycastHit2D hit2D = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, towerLayer);
        if (hit2D.collider != null) return true;

        // Check for 3D colliders
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, 100f, towerLayer)) return true;

        return false;
    }

    public void DisplayInfoSO(StructureData data)
    {
        if (data == null) return;

        // Ensure the panel is visible
        towerInfoPanel.SetActive(true);
        
        // Clear old text and hide unused buttons
        ResetFields();

        // Show buttons
        if (upgradeButton != null) upgradeButton.gameObject.SetActive(true);
        if (sellButton != null) sellButton.gameObject.SetActive(true);

        // Fill data based on type
        HandleDataTypes(data);
    }

    private void HandleDataTypes(StructureData data)
    {
        if (data is DefenseData def)
        {
            SetField(towerNameText, def.StructureName);
            SetField(hpText, $"HP: {def.MaxHP}");
            SetField(damageText, $"Damage: {def.damage}");
            SetField(attackCDText, $"CD: {def.attackCooldown}s");
            SetField(rangeText, $"Range: {def.range}");
            SetField(projectileSpeedText, $"Proj Speed: {def.projectileSpeed}");
            SetField(upgradeCostText, $"Upgrade: {def.UpgradeCost}");
        }
        else if (data is HeartData heart)
        {
            SetField(towerNameText, heart.StructureName);
            SetField(hpText, $"HP: {heart.MaxHP}");
            SetField(upgradeCostText, $"Upgrade: {heart.UpgradeCost}");
        }
        else if (data is ResourceData res)
        {
            SetField(towerNameText, res.StructureName);
            SetField(hpText, $"HP: {res.MaxHP}");
            SetField(nutrientsText, $"Nutrients: {res.nutrientsPerWave}");
            SetField(hydrationText, $"Hydration Boost: {res.hydrationCapacityBoost}");
            SetField(upgradeCostText, $"Upgrade: {res.UpgradeCost}");
        }
        else if (data is WallData wall)
        {
            SetField(towerNameText, wall.StructureName);
            SetField(hpText, $"HP: {wall.MaxHP}");
            SetField(upgradeCostText, $"Upgrade: {wall.UpgradeCost}");
        }
    }

    public void HideInfo()
    {
        towerInfoPanel.SetActive(false);
        ResetFields();
    }

    private void SetField(TextMeshProUGUI field, string textValue)
    {
        if (field != null)
        {
            field.gameObject.SetActive(true);
            field.text = textValue;
        }
    }

    private void ResetFields()
    {
        towerNameText.gameObject.SetActive(false);
        hpText.gameObject.SetActive(false);
        damageText.gameObject.SetActive(false);
        attackCDText.gameObject.SetActive(false);
        rangeText.gameObject.SetActive(false);
        projectileSpeedText.gameObject.SetActive(false);
        nutrientsText.gameObject.SetActive(false);
        hydrationText.gameObject.SetActive(false);
        
        if (upgradeCostText != null) upgradeCostText.gameObject.SetActive(false);
        if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
        if (sellButton != null) sellButton.gameObject.SetActive(false);
    }
}