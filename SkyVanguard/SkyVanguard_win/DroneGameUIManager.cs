
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Main UI controller for the drone delivery game.
/// Connect your already-created UI panels/buttons to this script in the Inspector.
///
/// Main flow:
/// Main Menu -> Mission Select -> Level Info -> Game HUD -> Level Complete.
/// Also handles Upgrade, Settings and Daily Reward popups.
/// </summary>
public class DroneGameUIManager : MonoBehaviour
{
    public static DroneGameUIManager Instance { get; private set; }

    [Serializable]
    public class MissionDefinition
    {
        public string missionName = "Mission 1";
        public Sprite previewSprite;
        [TextArea] public string objective = "Deliver the parcel";
        public int missions = 1;
        public float timeLimit = 180f;
        public int reward = 100;
        [Range(0, 3)] public int defaultStars = 0;
        public bool unlocked = true;
        public float requiredSpeed = 1f;
        public float requiredBattery = 100f;
        public float requiredPower = 1f;
        public bool requireUpgrades = false;
    }

    [Header("Screen Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject missionSelectPanel;
    [SerializeField] private GameObject levelInfoPanel;
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject dailyRewardPanel;
    [SerializeField] private GameObject gameHUDPanel;
    [SerializeField] private GameObject levelCompletePanel;

    [Header("Mission Select")]
    [SerializeField] private MissionCardUI[] missionCardPrefabs;
    [SerializeField] private MissionDefinition[] missions;
    [SerializeField] private Button missionSelectBackButton;

    [Header("Mission Card Scroll")]
    [Tooltip("The Content RectTransform inside the Mission Scroll View.")]
    [SerializeField] private RectTransform missionContent;
    [Tooltip("Button that shows the previous mission card.")]
    [SerializeField] private Button missionLeftButton;
    [Tooltip("Button that shows the next mission card.")]
    [SerializeField] private Button missionRightButton;

    private int missionDisplayIndex;
    private List<MissionCardUI> activeMissionCards = new List<MissionCardUI>();

    [Header("Level Info")]
    [SerializeField] private Image levelPreviewImage;
    [SerializeField] private TMP_Text levelTitleText;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text missionsText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private TMP_Text requirementsText;
    [SerializeField] private Button levelInfoPlayButton;
    [SerializeField] private Button levelInfoBackButton;

    [Header("Common Currency")]
    [SerializeField] private TMP_Text[] coinTexts;
    [SerializeField] private TMP_Text[] diamondTexts;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button mainPlayButton;
    [SerializeField] private Button mainUpgradeButton;
    [SerializeField] private Button mainSettingsButton;
    [SerializeField] private Button mainDailyRewardButton;

    [Header("Upgrade")]
    [SerializeField] private UpgradeItemUI[] upgradeItems;
    [SerializeField] private Button upgradeBackButton;
    [SerializeField] private TMP_Text upgradeStatsText;

    [Header("Settings")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Button settingsCloseButton;

    [Header("Daily Reward")]
    [SerializeField] private DailyRewardDayUI[] dailyRewardDays;
    [SerializeField] private Button dailyRewardCloseButton;
    [SerializeField] private int[] dailyRewards = { 100, 150, 200, 300, 400, 600, 1000, 1200, 1500, 2000 };
    [SerializeField] private TMP_Text dailyStreakText;

    [Header("Game HUD")]
    [SerializeField] private TMP_Text hudTimerText;
    [SerializeField] private TMP_Text hudObjectiveText;
    [SerializeField] private TMP_Text hudMoneyText;
    [SerializeField] private Slider deliveryProgressSlider;
    [SerializeField] private Button hudPauseButton;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button pauseResumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseSettingsButton;
    [SerializeField] private Button pauseHomeButton;

    [Header("Drone Status")]
    [SerializeField] private TMP_Text batteryPercentageText;
    [SerializeField] private Slider batterySlider;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private float batteryDrainRate = 0.05f;

    [Header("Level Complete")]
    [SerializeField] private TMP_Text completeTitleText;
    [SerializeField] private TMP_Text completeTimeText;
    [SerializeField] private TMP_Text completeRewardText;
    [SerializeField] private Image[] completeStars;
    [SerializeField] private Sprite starOnSprite;
    [SerializeField] private Sprite starOffSprite;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button nextButton;

    [Header("Gameplay")]
    [SerializeField] private SimpleDroneController drone;
    [SerializeField] private DroneDeliveryManager deliveryManager;
    [SerializeField] private bool lockCursorInGameplay = false;

    [Header("Starting Values")]
    [SerializeField] private int startingCoins = 2500;
    [SerializeField] private int startingDiamonds = 120;

    private int coins;
    private int diamonds;
    private int selectedMissionIndex;
    private int highestUnlockedMission;

    // Best stars saved for each mission.
    private int[] missionStars;

    private float missionTimer;
    private bool missionRunning;
    private bool missionCompleted;
    private float lastDeliveryProgress;

    private float currentBattery = 100f;
    private bool isBatteryDepleting = true;

    // Daily Reward tracking - FIXED
    private int currentDailyDay = 0;
    private bool[] dailyRewardClaimed; // Track each day individually
    private int dailyStreak = 0;

    private Dictionary<UpgradeItemUI.UpgradeType, float> upgradeValues = new Dictionary<UpgradeItemUI.UpgradeType, float>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        LoadProgress();

        WireButtons();
        RefreshCurrency();
        RefreshSettings();
        UpdateUpgradeValues();
        RefreshUpgradeUI();
        RefreshMissionCards();
        RefreshDailyRewards();
        UpdateDailyStreakDisplay();
        ShowOnly(mainMenuPanel);
    }

    // ============ SAVE / LOAD ============

    private void LoadProgress()
    {
        coins = GameSaveSystem.GetCoins(startingCoins);
        diamonds = GameSaveSystem.GetDiamonds(startingDiamonds);

        highestUnlockedMission = GameSaveSystem.GetHighestUnlockedMission(0);

        if (missions != null && missions.Length > 0)
        {
            highestUnlockedMission = Mathf.Clamp(
                highestUnlockedMission,
                0,
                missions.Length - 1
            );

            missionStars = new int[missions.Length];

            for (int i = 0; i < missions.Length; i++)
            {
                int defaultStars = missions[i] != null
                    ? missions[i].defaultStars
                    : 0;

                missionStars[i] = GameSaveSystem.GetMissionStars(
                    i,
                    defaultStars
                );
            }
        }
        else
        {
            missionStars = new int[0];
        }

        dailyRewardClaimed = GameSaveSystem.GetDailyClaimed(
            dailyRewards != null ? dailyRewards.Length : 0
        );

        currentDailyDay = GameSaveSystem.GetDailyCurrentDay(0);
        dailyStreak = GameSaveSystem.GetDailyStreak(0);

        if (dailyRewards != null && dailyRewards.Length > 0)
        {
            currentDailyDay = Mathf.Clamp(
                currentDailyDay,
                0,
                dailyRewards.Length - 1
            );
        }
        else
        {
            currentDailyDay = 0;
        }
    }

    private void SaveProgress()
    {
        GameSaveSystem.SetCoins(coins);
        GameSaveSystem.SetDiamonds(diamonds);
        GameSaveSystem.SetHighestUnlockedMission(highestUnlockedMission);

        if (missionStars != null)
        {
            for (int i = 0; i < missionStars.Length; i++)
                GameSaveSystem.SetMissionStars(i, missionStars[i]);
        }

        GameSaveSystem.SetDailyCurrentDay(currentDailyDay);
        GameSaveSystem.SetDailyStreak(dailyStreak);
        GameSaveSystem.SetDailyClaimed(dailyRewardClaimed);

        GameSaveSystem.Save();
    }

    public int GetMissionStars(int missionIndex)
    {
        if (missionStars == null ||
            missionIndex < 0 ||
            missionIndex >= missionStars.Length)
            return 0;

        return missionStars[missionIndex];
    }

    private void Start()
    {
        if (drone == null)
            drone = FindObjectOfType<SimpleDroneController>();

        if (deliveryManager == null)
            deliveryManager = FindObjectOfType<DroneDeliveryManager>();

        UpdateHUD();
        ResetBattery();
        ApplyUpgradesToDrone();
    }

    private void Update()
    {
        if (!missionRunning || missionCompleted)
            return;

        missionTimer += Time.deltaTime;
        UpdateHUD();
        UpdateBattery();
        UpdateSpeed();

        MissionDefinition mission = GetSelectedMission();
        if (mission != null && mission.timeLimit > 0f && missionTimer >= mission.timeLimit)
        {
            missionRunning = false;
            missionCompleted = true;
            ShowLevelComplete(0);
        }

        if (currentBattery <= 0f && isBatteryDepleting)
        {
            isBatteryDepleting = false;
            missionRunning = false;
            missionCompleted = true;
            ShowLevelComplete(0);
            Debug.Log("Battery depleted! Mission failed.");
        }
    }

    private void WireButtons()
    {
        AddListener(mainPlayButton, OpenMissionSelect);
        AddListener(mainUpgradeButton, OpenUpgrade);
        AddListener(mainSettingsButton, OpenSettings);
        AddListener(mainDailyRewardButton, OpenDailyReward);

        AddListener(missionSelectBackButton, OpenMainMenu);
        AddListener(missionLeftButton, ShowPreviousMissionCard);
        AddListener(missionRightButton, ShowNextMissionCard);
        AddListener(levelInfoBackButton, OpenMissionSelect);
        AddListener(levelInfoPlayButton, StartSelectedMission);

        AddListener(upgradeBackButton, OpenMainMenu);
        AddListener(settingsCloseButton, CloseSettings);
        AddListener(dailyRewardCloseButton, CloseDailyReward);

        AddListener(hudPauseButton, PauseGame);
        AddListener(pauseResumeButton, ResumeGame);
        AddListener(pauseRestartButton, RestartSelectedMission);
        AddListener(pauseSettingsButton, OpenSettingsFromPause);
        AddListener(pauseHomeButton, ReturnHomeFromGame);

        AddListener(homeButton, ReturnHomeFromComplete);
        AddListener(restartButton, RestartSelectedMission);
        AddListener(nextButton, NextMission);
    }

    private void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    public void OpenMainMenu()
    {
        StopMissionTimer();
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        ShowOnly(mainMenuPanel);
        ResetBattery();
    }

    public void OpenMissionSelect()
    {
        Time.timeScale = 1f;
        RefreshMissionCards();
        ShowOnly(missionSelectPanel);
        ResetBattery();
    }

    public void SelectMission(int index)
    {
        if (index < 0 || index >= missions.Length)
            return;

        if (index > highestUnlockedMission)
        {
            Debug.Log("Mission is locked.");
            return;
        }

        selectedMissionIndex = index;
        missionDisplayIndex = index;
        PopulateLevelInfo();
        ShowOnly(levelInfoPanel);
        ResetBattery();
    }

    public void OpenLevelInfo(int index)
    {
        SelectMission(index);
    }

    private void PopulateLevelInfo()
    {
        MissionDefinition mission = GetSelectedMission();
        if (mission == null) return;

        if (levelTitleText != null)
            levelTitleText.text = mission.missionName;

        if (objectiveText != null)
            objectiveText.text = mission.objective;

        if (missionsText != null)
            missionsText.text = mission.missions.ToString();

        if (timeText != null)
            timeText.text = FormatTime(mission.timeLimit);

        if (rewardText != null)
            rewardText.text = mission.reward.ToString();

        if (levelPreviewImage != null)
        {
            levelPreviewImage.sprite = mission.previewSprite;
            levelPreviewImage.enabled = mission.previewSprite != null;
        }

        if (requirementsText != null)
        {
            bool hasRequirements = CheckMissionRequirements(mission, out string requirements);
            requirementsText.text = requirements;
            requirementsText.color = hasRequirements ? Color.green : Color.red;

            if (levelInfoPlayButton != null)
                levelInfoPlayButton.interactable = hasRequirements;
        }
    }

    private bool CheckMissionRequirements(MissionDefinition mission, out string requirements)
    {
        requirements = "";

        if (!mission.requireUpgrades)
        {
            requirements = "No special requirements.\nPress Play to start!";
            return true;
        }

        float speed = GetUpgradeValue(UpgradeItemUI.UpgradeType.Speed);
        float battery = GetUpgradeValue(UpgradeItemUI.UpgradeType.Battery);
        float power = GetUpgradeValue(UpgradeItemUI.UpgradeType.Power);

        bool speedOk = speed >= mission.requiredSpeed;
        bool batteryOk = battery >= mission.requiredBattery;
        bool powerOk = power >= mission.requiredPower;

        requirements = "Requirements to play this mission:\n";
        requirements += $"• Speed: {speed:F1}x / Required: {mission.requiredSpeed:F1}x {(speedOk ? "✓" : "✗")}\n";
        requirements += $"• Battery: {battery:F0}% / Required: {mission.requiredBattery:F0}% {(batteryOk ? "✓" : "✗")}\n";
        requirements += $"• Power: {power:F1}kg / Required: {mission.requiredPower:F1}kg {(powerOk ? "✓" : "✗")}\n";

        if (speedOk && batteryOk && powerOk)
            requirements += "\n✅ All requirements met!";
        else
            requirements += "\n⚠️ Upgrade your drone to meet requirements!";

        return speedOk && batteryOk && powerOk;
    }

    public void StartSelectedMission()
    {
        MissionDefinition mission = GetSelectedMission();
        if (mission == null)
        {
            Debug.LogError("No mission selected!");
            return;
        }

        if (!CheckMissionRequirements(mission, out string reqText))
        {
            Debug.LogWarning($"Cannot start mission - requirements not met!\n{reqText}");
            if (requirementsText != null)
            {
                requirementsText.text = reqText;
                requirementsText.color = Color.red;
            }
            return;
        }

        Debug.Log($"Starting mission: {mission.missionName}");

        missionTimer = 0f;
        missionRunning = true;
        missionCompleted = false;
        lastDeliveryProgress = 0f;
        ResetBattery();

        if (deliveryManager != null)
            deliveryManager.ResetDelivery();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
        ShowOnly(gameHUDPanel);
        UpdateHUD();

        if (lockCursorInGameplay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        ApplyUpgradesToDrone();
    }

    public void OpenUpgrade()
    {
        RefreshUpgradeUI();
        ShowOnly(upgradePanel);
        UpdateUpgradeStatsDisplay();
    }

    public void OpenSettings()
    {
        RefreshSettings();
        ShowOnly(settingsPanel);
    }

    public void CloseSettings()
    {
        SaveSettings();
        if (pausePanel != null && pausePanel.activeSelf)
        {
            ShowOnly(gameHUDPanel);
            if (pausePanel != null) pausePanel.SetActive(true);
        }
        else
        {
            OpenMainMenu();
        }
    }

    public void OpenSettingsFromPause()
    {
        RefreshSettings();
        ShowOnly(settingsPanel);
        Time.timeScale = 0f;
    }

    // ============ DAILY REWARD METHODS - FIXED ============

    public void OpenDailyReward()
    {
        RefreshDailyRewards();
        ShowOnly(dailyRewardPanel);
        UpdateDailyStreakDisplay();
    }

    public void CloseDailyReward()
    {
        OpenMainMenu();
    }

    public void ClaimDailyReward(int dayIndex)
    {
        // Check if day index is valid
        if (dayIndex < 0 || dayIndex >= dailyRewards.Length)
        {
            Debug.LogError($"Invalid day index: {dayIndex}");
            return;
        }

        // Check if this day is already claimed
        if (dailyRewardClaimed[dayIndex])
        {
            Debug.Log($"Day {dayIndex + 1} already claimed!");
            return;
        }

        // Check if this day is the current available day
        if (dayIndex != currentDailyDay)
        {
            Debug.Log($"Cannot claim day {dayIndex + 1}. Current available day is {currentDailyDay + 1}");
            return;
        }

        // Get reward amount
        int reward = dailyRewards[dayIndex];

        // Add reward
        AddCoins(reward);

        // Mark this day as claimed
        dailyRewardClaimed[dayIndex] = true;

        // Increase streak
        dailyStreak++;

        // Move to next day (find next unclaimed day)
        int nextDay = currentDailyDay + 1;
        bool foundNext = false;
        for (int i = nextDay; i < dailyRewards.Length; i++)
        {
            if (!dailyRewardClaimed[i])
            {
                currentDailyDay = i;
                foundNext = true;
                break;
            }
        }

        // If all days are claimed, stay on last day
        if (!foundNext)
        {
            currentDailyDay = dailyRewards.Length - 1;
            // All days are claimed, show completed state
        }

        // Refresh UI
        RefreshDailyRewards();
        UpdateDailyStreakDisplay();

        SaveProgress();

        Debug.Log($"Claimed daily reward: {reward} coins! Streak: {dailyStreak}, Next day: {currentDailyDay + 1}");
    }

    private void RefreshDailyRewards()
    {
        if (dailyRewardDays == null || dailyRewardDays.Length == 0)
        {
            Debug.LogWarning("No daily reward days assigned in the Inspector!");
            return;
        }

        int daysToShow = Mathf.Min(dailyRewardDays.Length, dailyRewards.Length);

        for (int i = 0; i < daysToShow; i++)
        {
            if (dailyRewardDays[i] == null)
            {
                Debug.LogWarning($"Daily reward day {i} is null!");
                continue;
            }

            int reward = i < dailyRewards.Length ? dailyRewards[i] : 100;

            // Determine state for this day - FIXED LOGIC
            bool isClaimed = dailyRewardClaimed[i];
            bool isLocked = i > currentDailyDay;
            bool isAvailable = i == currentDailyDay && !isClaimed;

            // If this day is before current day and not claimed, it should be claimed
            if (i < currentDailyDay && !isClaimed)
            {
                // This shouldn't happen, but just in case
                dailyRewardClaimed[i] = true;
                isClaimed = true;
            }

            // Setup the day UI
            dailyRewardDays[i].Setup(
                i,
                reward,
                isLocked,
                isAvailable,
                isClaimed,
                ClaimDailyReward
            );
        }

        // Update any extra days beyond the rewards array (if any)
        for (int i = daysToShow; i < dailyRewardDays.Length; i++)
        {
            if (dailyRewardDays[i] != null)
            {
                dailyRewardDays[i].Setup(
                    i,
                    0,
                    true, // locked
                    false, // not available
                    false, // not claimed
                    null
                );
            }
        }
    }

    private void UpdateDailyStreakDisplay()
    {
        if (dailyStreakText != null)
        {
            dailyStreakText.text = $"🔥 Streak: {dailyStreak} days";

            // Color based on streak
            if (dailyStreak >= 7)
                dailyStreakText.color = Color.yellow;
            else if (dailyStreak >= 3)
                dailyStreakText.color = Color.green;
            else
                dailyStreakText.color = Color.white;
        }
    }

    private void InitializeDailyRewards()
    {
        // Initialize daily reward claimed array
        dailyRewardClaimed = new bool[dailyRewards.Length];
        for (int i = 0; i < dailyRewardClaimed.Length; i++)
        {
            dailyRewardClaimed[i] = false;
        }
        currentDailyDay = 0;
        dailyStreak = 0;
        RefreshDailyRewards();
        UpdateDailyStreakDisplay();
    }

    // ============ UPGRADE METHODS ============

    public void BuyUpgrade(int index)
    {
        if (index < 0 || index >= upgradeItems.Length)
            return;

        UpgradeItemUI item = upgradeItems[index];
        if (item == null) return;

        int currentLevel = item.Level;
        int maxLevel = item.MaxLevel;
        if (currentLevel >= maxLevel) return;

        int cost = item.GetCurrentCost();
        if (coins < cost)
        {
            Debug.Log("Not enough coins.");
            return;
        }

        coins -= cost;
        item.SetLevel(currentLevel + 1);
        RefreshCurrency();
        RefreshUpgradeUI();
        UpdateUpgradeValues();
        ApplyUpgradesToDrone();
        UpdateUpgradeStatsDisplay();

        SaveProgress();
    }

    private void ApplyUpgradesToDrone()
    {
        if (drone == null) return;

        float speedMultiplier = GetUpgradeValue(UpgradeItemUI.UpgradeType.Speed);
        float batteryCapacity = GetUpgradeValue(UpgradeItemUI.UpgradeType.Battery);
        float powerMultiplier = GetUpgradeValue(UpgradeItemUI.UpgradeType.Power);
        float durability = GetUpgradeValue(UpgradeItemUI.UpgradeType.Durability);

        drone.SetSpeedMultiplier(speedMultiplier);
        drone.SetBatteryCapacity(batteryCapacity);
        drone.SetPowerMultiplier(powerMultiplier);
        drone.SetDurability(durability);

        Debug.Log($"Upgrades Applied - Speed: {speedMultiplier}x, Battery: {batteryCapacity}%, Power: {powerMultiplier}x, Durability: {durability}%");
    }

    private void RefreshUpgradeUI()
    {
        if (upgradeItems == null) return;

        foreach (UpgradeItemUI item in upgradeItems)
            if (item != null) item.Refresh();
    }

    private void UpdateUpgradeStatsDisplay()
    {
        if (upgradeStatsText == null) return;

        float speed = GetUpgradeValue(UpgradeItemUI.UpgradeType.Speed);
        float battery = GetUpgradeValue(UpgradeItemUI.UpgradeType.Battery);
        float power = GetUpgradeValue(UpgradeItemUI.UpgradeType.Power);
        float durability = GetUpgradeValue(UpgradeItemUI.UpgradeType.Durability);

        upgradeStatsText.text = $"Drone Stats\n" +
                                $"Speed: {speed:F1}x\n" +
                                $"Battery: {battery:F0}%\n" +
                                $"Power: {power:F1}kg\n" +
                                $"Durability: {durability:F0}%";
    }

    private void UpdateUpgradeValues()
    {
        upgradeValues.Clear();

        foreach (UpgradeItemUI item in upgradeItems)
        {
            if (item != null)
                upgradeValues[item.Type] = item.CurrentValue;
        }
    }

    public float GetUpgradeValue(UpgradeItemUI.UpgradeType type)
    {
        if (upgradeValues.TryGetValue(type, out float value))
            return value;
        return 1f;
    }

    public void OnUpgradesChanged()
    {
        UpdateUpgradeValues();
        ApplyUpgradesToDrone();
        UpdateUpgradeStatsDisplay();
    }

    // ============ CURRENCY METHODS ============

    public bool TrySpendCoins(int amount)
    {
        if (amount <= 0) return true;
        if (coins < amount) return false;

        coins -= amount;
        RefreshCurrency();
        GameSaveSystem.SetCoins(coins);
        GameSaveSystem.Save();
        return true;
    }

    public void AddCoins(int amount)
    {
        coins = Mathf.Max(0, coins + amount);
        RefreshCurrency();
        GameSaveSystem.SetCoins(coins);
        GameSaveSystem.Save();
    }

    public void AddDiamonds(int amount)
    {
        diamonds = Mathf.Max(0, diamonds + amount);
        RefreshCurrency();
        GameSaveSystem.SetDiamonds(diamonds);
        GameSaveSystem.Save();
    }

    private void RefreshCurrency()
    {
        foreach (TMP_Text text in coinTexts)
            if (text != null) text.text = coins.ToString();

        foreach (TMP_Text text in diamondTexts)
            if (text != null) text.text = diamonds.ToString();

        if (hudMoneyText != null)
            hudMoneyText.text = coins.ToString();
    }

    public int GetCoins() => coins;
    public int GetDiamonds() => diamonds;

    // ============ MISSION METHODS ============

    private void RefreshMissionCards()
    {
        if (missionContent == null)
        {
            Debug.LogError("Mission Content is not assigned!");
            return;
        }

        if (missionCardPrefabs == null || missionCardPrefabs.Length == 0)
        {
            Debug.LogError("Mission Card Prefabs are not assigned!");
            return;
        }

        if (missions == null || missions.Length == 0)
        {
            Debug.LogError("Missions are not assigned!");
            return;
        }

        foreach (Transform child in missionContent)
            DestroyImmediate(child.gameObject);
        activeMissionCards.Clear();

        int missionCount = Mathf.Min(missions.Length, missionCardPrefabs.Length);

        for (int i = 0; i < missionCount; i++)
        {
            if (missionCardPrefabs[i] == null) continue;

            MissionCardUI cardInstance = Instantiate(missionCardPrefabs[i], missionContent);
            RectTransform rect = cardInstance.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one;
            }

            activeMissionCards.Add(cardInstance);

            MissionDefinition data = missions[i];
            bool unlocked = i <= highestUnlockedMission && data.unlocked;

            cardInstance.Setup(
                i,
                data.missionName,
                data.previewSprite,
                data.reward,
                GetMissionStars(i),
                unlocked,
                SelectMission
            );

            cardInstance.gameObject.SetActive(true);
        }

        missionDisplayIndex = 0;
        UpdateCardVisibility();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(missionContent);
        UpdateMissionNavigationButtons();
    }

    private void UpdateCardVisibility()
    {
        if (activeMissionCards == null || activeMissionCards.Count == 0) return;

        for (int i = 0; i < activeMissionCards.Count; i++)
        {
            if (activeMissionCards[i] != null)
                activeMissionCards[i].gameObject.SetActive(i == missionDisplayIndex);
        }
    }

    public void ShowNextMissionCard()
    {
        int count = activeMissionCards.Count;
        if (count <= 0) return;

        missionDisplayIndex = Mathf.Clamp(missionDisplayIndex + 1, 0, count - 1);
        UpdateCardVisibility();
        UpdateMissionNavigationButtons();
    }

    public void ShowPreviousMissionCard()
    {
        int count = activeMissionCards.Count;
        if (count <= 0) return;

        missionDisplayIndex = Mathf.Clamp(missionDisplayIndex - 1, 0, count - 1);
        UpdateCardVisibility();
        UpdateMissionNavigationButtons();
    }

    private void UpdateMissionNavigationButtons()
    {
        int count = activeMissionCards.Count;

        if (missionLeftButton != null)
            missionLeftButton.interactable = count > 0 && missionDisplayIndex > 0;

        if (missionRightButton != null)
            missionRightButton.interactable = count > 0 && missionDisplayIndex < count - 1;
    }

    private MissionDefinition GetSelectedMission()
    {
        if (missions == null || missions.Length == 0) return null;

        if (selectedMissionIndex < 0 || selectedMissionIndex >= missions.Length)
            selectedMissionIndex = 0;

        return missions[selectedMissionIndex];
    }

    // ============ BATTERY METHODS ============

    private void ResetBattery()
    {
        float batteryCapacity = GetUpgradeValue(UpgradeItemUI.UpgradeType.Battery);
        currentBattery = batteryCapacity;
        isBatteryDepleting = true;
        UpdateBatteryUI();
    }

    private void UpdateBattery()
    {
        if (!missionRunning || missionCompleted || !isBatteryDepleting)
            return;

        float drainMultiplier = 1f;
        float durability = GetUpgradeValue(UpgradeItemUI.UpgradeType.Durability) / 100f;

        float efficiencyBonus = 1f - ((durability - 1f) * 0.5f);
        efficiencyBonus = Mathf.Clamp(efficiencyBonus, 0.5f, 1f);

        if (drone != null && drone.IsFlying)
            drainMultiplier = 1.2f;

        float actualDrainRate = batteryDrainRate * drainMultiplier * efficiencyBonus;
        currentBattery -= actualDrainRate * Time.deltaTime;

        float maxBattery = GetUpgradeValue(UpgradeItemUI.UpgradeType.Battery);
        currentBattery = Mathf.Clamp(currentBattery, 0f, maxBattery);

        UpdateBatteryUI();

        if (currentBattery <= 20f && currentBattery > 15f)
        {
            Debug.LogWarning("Battery low! " + Mathf.RoundToInt(currentBattery) + "% remaining");
        }
    }

    private void UpdateBatteryUI()
    {
        float maxBattery = GetUpgradeValue(UpgradeItemUI.UpgradeType.Battery);
        float percentage = (currentBattery / maxBattery) * 100f;

        if (batteryPercentageText != null)
        {
            batteryPercentageText.text = Mathf.RoundToInt(percentage) + "%";

            if (percentage <= 20f)
                batteryPercentageText.color = Color.red;
            else if (percentage <= 50f)
                batteryPercentageText.color = Color.yellow;
            else
                batteryPercentageText.color = Color.green;
        }

        if (batterySlider != null)
        {
            batterySlider.value = percentage / 100f;

            Image sliderFill = batterySlider.fillRect?.GetComponent<Image>();
            if (sliderFill != null)
            {
                if (percentage <= 20f)
                    sliderFill.color = Color.red;
                else if (percentage <= 50f)
                    sliderFill.color = Color.yellow;
                else
                    sliderFill.color = Color.green;
            }
        }
    }

    // ============ SPEED METHODS ============

    private void UpdateSpeed()
    {
        if (speedText == null) return;

        float currentSpeed = 0f;

        if (drone != null)
        {
            Rigidbody rb = drone.GetComponent<Rigidbody>();
            if (rb != null)
                currentSpeed = rb.velocity.magnitude;
        }

        float speedMultiplier = GetUpgradeValue(UpgradeItemUI.UpgradeType.Speed);
        float displaySpeed = currentSpeed * speedMultiplier;

        speedText.text = displaySpeed.ToString("F1") + " m/s";

        if (displaySpeed > 8f)
            speedText.color = Color.green;
        else if (displaySpeed > 3f)
            speedText.color = Color.yellow;
        else
            speedText.color = Color.white;
    }

    // ============ HUD METHODS ============

    private void UpdateHUD()
    {
        if (!missionRunning) return;

        MissionDefinition mission = GetSelectedMission();

        if (hudTimerText != null)
        {
            float remaining = mission != null && mission.timeLimit > 0f
                ? Mathf.Max(0f, mission.timeLimit - missionTimer)
                : missionTimer;

            hudTimerText.text = FormatTime(remaining);
        }

        if (hudObjectiveText != null && mission != null)
        {
            if (deliveryManager == null)
                hudObjectiveText.text = mission.objective;
            else
            {
                switch (deliveryManager.State)
                {
                    case DroneDeliveryManager.DeliveryState.WaitingForPickup:
                        hudObjectiveText.text = "PICK UP THE PARCEL";
                        break;
                    case DroneDeliveryManager.DeliveryState.CarryingParcel:
                        hudObjectiveText.text = "DELIVER THE PARCEL";
                        break;
                    case DroneDeliveryManager.DeliveryState.Delivered:
                        hudObjectiveText.text = "DELIVERY COMPLETE!";
                        break;
                }
            }
        }

        if (hudMoneyText != null)
            hudMoneyText.text = coins.ToString();

        float progress = 0f;

        if (deliveryManager != null)
            progress = deliveryManager.GetDeliveryProgress();

        lastDeliveryProgress = Mathf.MoveTowards(lastDeliveryProgress, progress, Time.deltaTime * 3f);

        if (deliveryProgressSlider != null)
            deliveryProgressSlider.value = lastDeliveryProgress;
    }

    // ============ LEVEL COMPLETE METHODS ============

    public void NotifyDeliveryCompleted()
    {
        if (missionCompleted) return;

        missionRunning = false;
        missionCompleted = true;
        isBatteryDepleting = false;

        MissionDefinition mission = GetSelectedMission();
        int reward = mission != null ? mission.reward : 100;

        // Keep the current reward behavior: every successful delivery pays.
        AddCoins(reward);

        if (selectedMissionIndex >= highestUnlockedMission &&
            selectedMissionIndex < missions.Length - 1)
        {
            highestUnlockedMission = selectedMissionIndex + 1;
        }

        int stars = CalculateStars(
            missionTimer,
            mission != null ? mission.timeLimit : 180f
        );

        // Save the best star result for this mission.
        if (missionStars != null &&
            selectedMissionIndex >= 0 &&
            selectedMissionIndex < missionStars.Length)
        {
            missionStars[selectedMissionIndex] = Mathf.Max(
                missionStars[selectedMissionIndex],
                stars
            );
        }

        SaveProgress();
        ShowLevelComplete(stars);
    }

    public void ShowLevelComplete(int stars)
    {
        MissionDefinition mission = GetSelectedMission();

        if (completeTitleText != null)
            completeTitleText.text = "LEVEL COMPLETE";

        if (completeTimeText != null)
            completeTimeText.text = "TIME TAKEN     " + FormatTime(missionTimer);

        if (completeRewardText != null)
            completeRewardText.text = "REWARD         " + (mission != null ? mission.reward : 100);

        if (completeStars != null)
        {
            for (int i = 0; i < completeStars.Length; i++)
            {
                if (completeStars[i] == null) continue;
                completeStars[i].sprite = i < stars ? starOnSprite : starOffSprite;
                completeStars[i].enabled = completeStars[i].sprite != null;
            }
        }

        ShowOnly(levelCompletePanel);
    }

    private int CalculateStars(float time, float limit)
    {
        float speedBoost = GetUpgradeValue(UpgradeItemUI.UpgradeType.Speed);
        float adjustedLimit = limit / (1f + (speedBoost - 1f) * 0.5f);

        if (adjustedLimit <= 0f) return 3;
        if (time <= adjustedLimit * 0.40f) return 3;
        if (time <= adjustedLimit * 0.70f) return 2;
        return 1;
    }

    // ============ GAME STATE METHODS ============

    public void PauseGame()
    {
        if (!missionRunning) return;

        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        if (pausePanel != null) pausePanel.transform.SetAsLastSibling();
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void ReturnHomeFromGame()
    {
        Time.timeScale = 1f;
        StopMissionTimer();

        if (pausePanel != null) pausePanel.SetActive(false);

        if (lockCursorInGameplay)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        ResetBattery();
        ShowOnly(mainMenuPanel);
    }

    public void ReturnHomeFromComplete()
    {
        Time.timeScale = 1f;
        ResetBattery();
        ShowOnly(mainMenuPanel);
    }

    public void RestartSelectedMission()
    {
        Time.timeScale = 1f;

        if (deliveryManager != null)
            deliveryManager.ResetDelivery();

        missionTimer = 0f;
        missionRunning = true;
        missionCompleted = false;
        ResetBattery();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        ShowOnly(gameHUDPanel);
        ApplyUpgradesToDrone();
    }

    public void NextMission()
    {
        int next = selectedMissionIndex + 1;

        if (next >= missions.Length)
        {
            OpenMainMenu();
            return;
        }

        if (next > highestUnlockedMission)
        {
            OpenMissionSelect();
            return;
        }

        selectedMissionIndex = next;
        PopulateLevelInfo();
        ShowOnly(levelInfoPanel);
    }

    // ============ SETTINGS METHODS ============

    private void RefreshSettings()
    {
        if (musicToggle != null)
            musicToggle.isOn = GameSaveSystem.GetMusicEnabled(true);

        if (soundToggle != null)
            soundToggle.isOn = GameSaveSystem.GetSoundEnabled(true);

        if (graphicsDropdown != null && graphicsDropdown.options.Count > 0)
        {
            int savedQuality = GameSaveSystem.GetQualityLevel(
                QualitySettings.GetQualityLevel()
            );

            savedQuality = Mathf.Clamp(
                savedQuality,
                0,
                graphicsDropdown.options.Count - 1
            );

            graphicsDropdown.value = savedQuality;
            QualitySettings.SetQualityLevel(savedQuality, true);
        }

        if (sensitivitySlider != null)
            sensitivitySlider.value = GameSaveSystem.GetSensitivity(1f);
    }

    private void SaveSettings()
    {
        if (musicToggle != null)
            GameSaveSystem.SetMusicEnabled(musicToggle.isOn);

        if (soundToggle != null)
            GameSaveSystem.SetSoundEnabled(soundToggle.isOn);

        if (graphicsDropdown != null)
        {
            QualitySettings.SetQualityLevel(
                graphicsDropdown.value,
                true
            );

            GameSaveSystem.SetQualityLevel(
                graphicsDropdown.value
            );
        }

        if (sensitivitySlider != null)
            GameSaveSystem.SetSensitivity(sensitivitySlider.value);

        GameSaveSystem.Save();
    }

    public void SetMusic(bool enabled)
    {
        GameSaveSystem.SetMusicEnabled(enabled);
        GameSaveSystem.Save();
    }

    public void SetSound(bool enabled)
    {
        GameSaveSystem.SetSoundEnabled(enabled);
        GameSaveSystem.Save();
    }

    public void SetGraphics(int qualityIndex)
    {
        if (graphicsDropdown != null &&
            graphicsDropdown.options.Count > 0)
        {
            qualityIndex = Mathf.Clamp(
                qualityIndex,
                0,
                graphicsDropdown.options.Count - 1
            );
        }

        QualitySettings.SetQualityLevel(qualityIndex, true);
        GameSaveSystem.SetQualityLevel(qualityIndex);
        GameSaveSystem.Save();
    }

    public void SetSensitivity(float value)
    {
        GameSaveSystem.SetSensitivity(value);
        GameSaveSystem.Save();
    }

    public void ResetAllProgress()
    {
        GameSaveSystem.ResetAllProgress();

        // Reload the current scene so all UI and upgrade objects return
        // to their true new-game state.
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();

        if (currentScene.buildIndex >= 0)
            SceneManager.LoadScene(currentScene.buildIndex);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveProgress();
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    // ============ UTILITY METHODS ============

    private void ShowOnly(GameObject target)
    {
        GameObject[] panels =
        {
            mainMenuPanel,
            missionSelectPanel,
            levelInfoPanel,
            upgradePanel,
            settingsPanel,
            dailyRewardPanel,
            gameHUDPanel,
            levelCompletePanel
        };

        foreach (GameObject panel in panels)
        {
            if (panel != null)
                panel.SetActive(panel == target);
        }

        if (target == dailyRewardPanel)
        {
            RefreshDailyRewards();
            UpdateDailyStreakDisplay();
        }

        if (target != gameHUDPanel && lockCursorInGameplay)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void StopMissionTimer()
    {
        missionRunning = false;
        missionCompleted = false;
        missionTimer = 0f;
        isBatteryDepleting = false;
    }

    public int GetSelectedMissionIndex() => selectedMissionIndex;

    public static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
}