
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeItemUI : MonoBehaviour
{
    public enum UpgradeType
    {
        Speed,
        Battery,
        Power,
        Durability
    }

    [Header("Upgrade Configuration")]
    [SerializeField] private UpgradeType upgradeType = UpgradeType.Speed;
    [SerializeField] private string upgradeName = "SPEED";
    [SerializeField] private Sprite icon;
    [SerializeField] private int startingLevel = 1;
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private int baseCost = 100;
    [SerializeField] private float costMultiplier = 1.5f;
    [SerializeField] private float upgradeValuePerLevel = 0.1f;

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Image progressFill;

    private int level;

    public int Level => level;
    public int MaxLevel => maxLevel;
    public UpgradeType Type => upgradeType;
    public float CurrentValue => GetUpgradeValue();

    private void Awake()
    {
        // Load the saved level. If this is a new game, use the Inspector starting level.
        level = GameSaveSystem.GetUpgradeLevel(upgradeType, startingLevel);
        level = Mathf.Clamp(level, startingLevel, maxLevel);
        Refresh();
    }

    public void Refresh()
    {
        level = Mathf.Clamp(level, startingLevel, maxLevel);

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (nameText != null)
            nameText.text = upgradeName;

        if (levelText != null)
            levelText.text = level >= maxLevel ? "MAX" : "LV. " + level;

        int cost = GetCurrentCost();

        if (costText != null)
            costText.text = level >= maxLevel ? "MAX" : cost.ToString();

        if (valueText != null)
        {
            float value = GetUpgradeValue();
            string unit = GetValueUnit();
            string displayText;

            if (level >= maxLevel)
                displayText = $"+{value:F1}{unit} MAX";
            else
                displayText = $"+{value:F1}{unit}";

            valueText.text = displayText;

            if (level >= maxLevel)
                valueText.color = Color.green;
            else if (level >= maxLevel * 0.7f)
                valueText.color = Color.yellow;
            else
                valueText.color = Color.white;
        }

        if (progressFill != null)
        {
            float progress = (float)(level - startingLevel) / (maxLevel - startingLevel);
            progressFill.fillAmount = Mathf.Clamp01(progress);
        }

        if (upgradeButton != null)
        {
            bool canAfford = DroneGameUIManager.Instance != null &&
                             DroneGameUIManager.Instance.GetCoins() >= cost;
            bool canUpgrade = level < maxLevel;

            upgradeButton.interactable = canUpgrade && canAfford;

            TMP_Text buttonText = upgradeButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                if (level >= maxLevel)
                    buttonText.text = "MAX";
                else
                    buttonText.text = $"UPGRADE\n{cost} coins";
            }

            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(Upgrade);
        }
    }

    private string GetValueUnit()
    {
        switch (upgradeType)
        {
            case UpgradeType.Speed: return "x";
            case UpgradeType.Battery: return "%";
            case UpgradeType.Power: return "kg";
            case UpgradeType.Durability: return "%";
            default: return "";
        }
    }

    public float GetUpgradeValue()
    {
        int levels = Mathf.Max(0, level - startingLevel);
        float baseValue = 1f;

        switch (upgradeType)
        {
            case UpgradeType.Speed:
                baseValue = 1f + (levels * upgradeValuePerLevel);
                break;
            case UpgradeType.Battery:
                baseValue = 100f + (levels * 15f);
                break;
            case UpgradeType.Power:
                baseValue = 1f + (levels * 0.5f);
                break;
            case UpgradeType.Durability:
                baseValue = 100f + (levels * 5f);
                break;
        }
        return baseValue;
    }

    public int GetCurrentCost()
    {
        int levelsBought = Mathf.Max(0, level - startingLevel);
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, levelsBought));
    }

    public void Upgrade()
    {
        if (level >= maxLevel) return;

        int cost = GetCurrentCost();

        if (DroneGameUIManager.Instance == null) return;

        if (!DroneGameUIManager.Instance.TrySpendCoins(cost))
        {
            Debug.Log($"Not enough coins! Need {cost}, have {DroneGameUIManager.Instance.GetCoins()}");
            return;
        }

        level++;
        level = Mathf.Clamp(level, startingLevel, maxLevel);

        GameSaveSystem.SetUpgradeLevel(upgradeType, level);
        GameSaveSystem.Save();

        Refresh();

        if (DroneGameUIManager.Instance != null)
            DroneGameUIManager.Instance.OnUpgradesChanged();
    }

    public void SetLevel(int newLevel)
    {
        level = Mathf.Clamp(newLevel, startingLevel, maxLevel);

        GameSaveSystem.SetUpgradeLevel(upgradeType, level);
        GameSaveSystem.Save();

        Refresh();
    }
}