using System;
using UnityEngine;

/// <summary>
/// Simple local save system for the Drone Delivery game.
/// Uses PlayerPrefs, so it works on Windows and Android without a server.
///
/// Saves:
/// - Coins / Diamonds
/// - Highest unlocked mission
/// - Best stars per mission
/// - Upgrade levels
/// - Daily reward progress
/// - Basic settings
///
/// This is intentionally simple for version 1.0.
/// </summary>
public static class GameSaveSystem
{
    private const string Prefix = "DroneDelivery_";

    private const string CoinsKey = Prefix + "Coins";
    private const string DiamondsKey = Prefix + "Diamonds";
    private const string HighestMissionKey = Prefix + "HighestUnlockedMission";

    private const string UpgradeSpeedKey = Prefix + "Upgrade_Speed";
    private const string UpgradeBatteryKey = Prefix + "Upgrade_Battery";
    private const string UpgradePowerKey = Prefix + "Upgrade_Power";
    private const string UpgradeDurabilityKey = Prefix + "Upgrade_Durability";

    private const string DailyCurrentDayKey = Prefix + "Daily_CurrentDay";
    private const string DailyStreakKey = Prefix + "Daily_Streak";
    private const string DailyClaimedKey = Prefix + "Daily_Claimed";

    private const string MusicKey = Prefix + "Setting_Music";
    private const string SoundKey = Prefix + "Setting_Sound";
    private const string QualityKey = Prefix + "Setting_Quality";
    private const string SensitivityKey = Prefix + "Setting_Sensitivity";

    private const string MissionStarsPrefix = Prefix + "MissionStars_";

    public static bool HasSaveData()
    {
        return PlayerPrefs.HasKey(CoinsKey) ||
               PlayerPrefs.HasKey(HighestMissionKey) ||
               PlayerPrefs.HasKey(UpgradeSpeedKey);
    }

    // ---------------- CURRENCY ----------------

    public static int GetCoins(int defaultValue)
    {
        return PlayerPrefs.GetInt(CoinsKey, defaultValue);
    }

    public static void SetCoins(int value)
    {
        PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, value));
    }

    public static int GetDiamonds(int defaultValue)
    {
        return PlayerPrefs.GetInt(DiamondsKey, defaultValue);
    }

    public static void SetDiamonds(int value)
    {
        PlayerPrefs.SetInt(DiamondsKey, Mathf.Max(0, value));
    }

    // ---------------- MISSIONS ----------------

    public static int GetHighestUnlockedMission(int defaultValue)
    {
        return PlayerPrefs.GetInt(HighestMissionKey, Mathf.Max(0, defaultValue));
    }

    public static void SetHighestUnlockedMission(int value)
    {
        PlayerPrefs.SetInt(HighestMissionKey, Mathf.Max(0, value));
    }

    public static int GetMissionStars(int missionIndex, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(
            MissionStarsPrefix + missionIndex,
            Mathf.Clamp(defaultValue, 0, 3)
        );
    }

    public static void SetMissionStars(int missionIndex, int stars)
    {
        PlayerPrefs.SetInt(
            MissionStarsPrefix + missionIndex,
            Mathf.Clamp(stars, 0, 3)
        );
    }

    // ---------------- UPGRADES ----------------

    public static int GetUpgradeLevel(
        UpgradeItemUI.UpgradeType type,
        int defaultLevel
    )
    {
        string key = GetUpgradeKey(type);
        return PlayerPrefs.GetInt(key, defaultLevel);
    }

    public static void SetUpgradeLevel(
        UpgradeItemUI.UpgradeType type,
        int level
    )
    {
        PlayerPrefs.SetInt(GetUpgradeKey(type), Mathf.Max(1, level));
    }

    private static string GetUpgradeKey(UpgradeItemUI.UpgradeType type)
    {
        switch (type)
        {
            case UpgradeItemUI.UpgradeType.Speed:
                return UpgradeSpeedKey;

            case UpgradeItemUI.UpgradeType.Battery:
                return UpgradeBatteryKey;

            case UpgradeItemUI.UpgradeType.Power:
                return UpgradePowerKey;

            case UpgradeItemUI.UpgradeType.Durability:
                return UpgradeDurabilityKey;

            default:
                return Prefix + "Upgrade_Unknown";
        }
    }

    // ---------------- DAILY REWARD ----------------

    public static int GetDailyCurrentDay(int defaultValue = 0)
    {
        return Mathf.Max(
            0,
            PlayerPrefs.GetInt(DailyCurrentDayKey, defaultValue)
        );
    }

    public static void SetDailyCurrentDay(int value)
    {
        PlayerPrefs.SetInt(DailyCurrentDayKey, Mathf.Max(0, value));
    }

    public static int GetDailyStreak(int defaultValue = 0)
    {
        return Mathf.Max(
            0,
            PlayerPrefs.GetInt(DailyStreakKey, defaultValue)
        );
    }

    public static void SetDailyStreak(int value)
    {
        PlayerPrefs.SetInt(DailyStreakKey, Mathf.Max(0, value));
    }

    public static bool[] GetDailyClaimed(int count)
    {
        bool[] result = new bool[Mathf.Max(0, count)];
        string saved = PlayerPrefs.GetString(DailyClaimedKey, "");

        if (string.IsNullOrEmpty(saved))
            return result;

        string[] values = saved.Split(',');

        for (int i = 0; i < result.Length && i < values.Length; i++)
            result[i] = values[i] == "1";

        return result;
    }

    public static void SetDailyClaimed(bool[] claimed)
    {
        if (claimed == null)
        {
            PlayerPrefs.SetString(DailyClaimedKey, "");
            return;
        }

        string[] values = new string[claimed.Length];

        for (int i = 0; i < claimed.Length; i++)
            values[i] = claimed[i] ? "1" : "0";

        PlayerPrefs.SetString(
            DailyClaimedKey,
            string.Join(",", values)
        );
    }

    // ---------------- SETTINGS ----------------

    public static bool GetMusicEnabled(bool defaultValue = true)
    {
        return PlayerPrefs.GetInt(MusicKey, defaultValue ? 1 : 0) == 1;
    }

    public static void SetMusicEnabled(bool value)
    {
        PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
    }

    public static bool GetSoundEnabled(bool defaultValue = true)
    {
        return PlayerPrefs.GetInt(SoundKey, defaultValue ? 1 : 0) == 1;
    }

    public static void SetSoundEnabled(bool value)
    {
        PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
    }

    public static int GetQualityLevel(int defaultValue)
    {
        return PlayerPrefs.GetInt(QualityKey, defaultValue);
    }

    public static void SetQualityLevel(int value)
    {
        PlayerPrefs.SetInt(QualityKey, Mathf.Max(0, value));
    }

    public static float GetSensitivity(float defaultValue = 1f)
    {
        return PlayerPrefs.GetFloat(SensitivityKey, defaultValue);
    }

    public static void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(
            SensitivityKey,
            Mathf.Clamp(value, 0.1f, 3f)
        );
    }

    // ---------------- SAVE / RESET ----------------

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void ResetAllProgress()
    {
        PlayerPrefs.DeleteKey(CoinsKey);
        PlayerPrefs.DeleteKey(DiamondsKey);
        PlayerPrefs.DeleteKey(HighestMissionKey);

        PlayerPrefs.DeleteKey(UpgradeSpeedKey);
        PlayerPrefs.DeleteKey(UpgradeBatteryKey);
        PlayerPrefs.DeleteKey(UpgradePowerKey);
        PlayerPrefs.DeleteKey(UpgradeDurabilityKey);

        PlayerPrefs.DeleteKey(DailyCurrentDayKey);
        PlayerPrefs.DeleteKey(DailyStreakKey);
        PlayerPrefs.DeleteKey(DailyClaimedKey);

        PlayerPrefs.DeleteKey(MusicKey);
        PlayerPrefs.DeleteKey(SoundKey);
        PlayerPrefs.DeleteKey(QualityKey);
        PlayerPrefs.DeleteKey(SensitivityKey);

        // Mission stars: remove a reasonable range.
        // If you have more than 1000 missions, increase this range.
        for (int i = 0; i < 1000; i++)
            PlayerPrefs.DeleteKey(MissionStarsPrefix + i);

        PlayerPrefs.Save();

        Debug.Log("Drone Delivery save data reset.");
    }
}
