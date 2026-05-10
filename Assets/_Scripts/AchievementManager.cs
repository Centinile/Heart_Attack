using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;
    public static System.Action<AchievementDefinition> OnAchievementUnlocked;
    private int _buildingsPlacedThisRun = 0;

    // Definitions — editable in inspector
    [Header("Achievement Definitions")]
    [SerializeField] private List<AchievementDefinition> achievements = new List<AchievementDefinition>
    {
        new AchievementDefinition { ID = AchievementID.CompleteTutorial,       Name = "First Steps",          Description = "Complete the tutorial.",                                Classification = AchievementClassification.Bronze },
        new AchievementDefinition { ID = AchievementID.WinEasy,                Name = "Steady Pulse",         Description = "Complete a game on Easy difficulty.",                   Classification = AchievementClassification.Bronze },
        new AchievementDefinition { ID = AchievementID.WinMedium,              Name = "Irregular Rhythm",     Description = "Complete a game on Medium difficulty.",                 Classification = AchievementClassification.Silver },
        new AchievementDefinition { ID = AchievementID.WinHard,                Name = "Flatline Survivor",    Description = "Complete a game on Hard difficulty.",                   Classification = AchievementClassification.Gold },
        new AchievementDefinition { ID = AchievementID.WinWithArrhythmia,      Name = "Arrhythmia",           Description = "Win a game with Random Waves active.",                  Classification = AchievementClassification.Silver },
        new AchievementDefinition { ID = AchievementID.WinWithAtherosclerosis, Name = "Atherosclerosis",      Description = "Win a game with Stat Ramping active.",                  Classification = AchievementClassification.Silver },
        new AchievementDefinition { ID = AchievementID.WinWithCardiomyopathy,  Name = "Cardiomyopathy",       Description = "Win a game with No Breaks active.",                     Classification = AchievementClassification.Silver },
        new AchievementDefinition { ID = AchievementID.WinWithLungFailure,     Name = "Lung Failure",         Description = "Win a game with Fog of War active.",                    Classification = AchievementClassification.Silver },
        new AchievementDefinition { ID = AchievementID.WinWithGERD,            Name = "GERD",                 Description = "Win a game with Acid Rain active.",                     Classification = AchievementClassification.Silver },
        new AchievementDefinition { ID = AchievementID.ReachWave80Freeplay,    Name = "Into the Abyss",       Description = "Reach wave 80 in Freeplay mode.",                       Classification = AchievementClassification.Gold },
        new AchievementDefinition { ID = AchievementID.WinAllModifiersHard,    Name = "Total Organ Failure",  Description = "Win on Hard with all modifiers active.",                Classification = AchievementClassification.Gold },
        new AchievementDefinition { ID = AchievementID.SoldHeart,              Name = "Heartless",            Description = "Sell the Heart building.",                              Classification = AchievementClassification.Bronze },
        new AchievementDefinition { ID = AchievementID.Placed100Buildings,     Name = "Base Planner",         Description = "Place 100 buildings in a single run.",                  Classification = AchievementClassification.Silver },
    };

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
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _buildingsPlacedThisRun = 0; // Reset per run
    }

    private void OnEnable()
    {
        GameManager.OnVictory     += OnVictory;
        WaveManager.OnWaveCleared += OnWaveCleared;
        Building.OnBuildingPlaced += OnBuildingPlaced;
        Building.OnHeartSold      += OnHeartSold;
    }


    private void OnDisable()
    {
        GameManager.OnVictory  -= OnVictory;
        WaveManager.OnWaveCleared -= OnWaveCleared;
        Building.OnBuildingPlaced -= OnBuildingPlaced;
        Building.OnHeartSold      -= OnHeartSold;
    }

    private void OnBuildingPlaced()
    {
        if (IsUnlocked(AchievementID.Placed100Buildings)) return;

        _buildingsPlacedThisRun++;

        if (_buildingsPlacedThisRun >= 100)
        {
            Debug.Log("[Achievement] Triggering Placed100Buildings unlock");
            Unlock(AchievementID.Placed100Buildings);
        }
    }

    private void OnHeartSold()
    {
        Unlock(AchievementID.SoldHeart);
    }

    // ── Public unlock entry point ──────────────────────────────────────

    public void Unlock(AchievementID id)
    {
        string key = $"Achievement_{id}";
        if (PlayerPrefs.GetInt(key, 0) == 1) return; // already unlocked

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();

        if (TryGetDefinition(id, out AchievementDefinition def))
            OnAchievementUnlocked?.Invoke(def);
    }

    public bool IsUnlocked(AchievementID id)
    {
        return PlayerPrefs.GetInt($"Achievement_{id}", 0) == 1;
    }

    public IReadOnlyList<AchievementDefinition> GetDefinitions()
    {
        return achievements;
    }

    public bool TryGetDefinition(AchievementID id, out AchievementDefinition definition)
    {
        definition = achievements.Find(a => a.ID == id);
        return definition != null;
    }

    // ── Event handlers ─────────────────────────────────────────────────

    private void OnVictory()
    {
        // Needs GameManager to still be alive — it will be since Victory fires before destroy
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        int difficulty = PlayerPrefs.GetInt("Difficulty", 0);

        // Difficulty achievements
        if (difficulty == 0) Unlock(AchievementID.WinEasy);
        if (difficulty == 1) Unlock(AchievementID.WinMedium);
        if (difficulty == 2) Unlock(AchievementID.WinHard);

        // Modifier achievements — only on victory
        if (gm.enableRandomWaves)  Unlock(AchievementID.WinWithArrhythmia);
        if (gm.enableStatRamping)  Unlock(AchievementID.WinWithAtherosclerosis);
        if (gm.enableNoBreaks)     Unlock(AchievementID.WinWithCardiomyopathy);
        if (gm.enableFogOfWar)     Unlock(AchievementID.WinWithLungFailure);
        if (gm.enableAcidRain)     Unlock(AchievementID.WinWithGERD);

        // All modifiers on Hard
        if (difficulty == 2
            && gm.enableRandomWaves
            && gm.enableStatRamping
            && gm.enableNoBreaks
            && gm.enableFogOfWar
            && gm.enableAcidRain)
        {
            Unlock(AchievementID.WinAllModifiersHard);
        }
    }

    private void OnWaveCleared()
    {
        // Wave 80 freeplay — WaveManager.currentWaveIndex has already incremented by now
        WaveManager wm = Object.FindFirstObjectByType<WaveManager>();
        if (wm == null) return;

        if (wm.freeplayMode && wm.currentWaveIndex >= 80)
            Unlock(AchievementID.ReachWave80Freeplay);
    }
}

public enum AchievementID
{
    CompleteTutorial,
    WinEasy,
    WinMedium,
    WinHard,
    WinWithArrhythmia,
    WinWithAtherosclerosis,
    WinWithCardiomyopathy,
    WinWithLungFailure,
    WinWithGERD,
    ReachWave80Freeplay,
    WinAllModifiersHard,
    SoldHeart,          
    Placed100Buildings,
}

public enum AchievementClassification
{
    Bronze,
    Silver,
    Gold
}

[System.Serializable]
public class AchievementDefinition
{
    public AchievementID ID;
    public string Name;
    public string Description;
    public AchievementClassification Classification;
    public Sprite TrophySprite;
}