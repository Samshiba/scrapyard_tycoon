using Sandbox;
using System.Collections.Generic;

/// <summary>
/// Gère le système de prestige : niveaux et upgrades déblocables
/// </summary>
public sealed class PrestigeSystem : Component
{
    public static PrestigeSystem Instance { get; private set; }

    private int _prestigeLevel = 0;
    private Dictionary<string, bool> _unlockedUpgrades = new();

    public int PrestigeLevel
    {
        get => _prestigeLevel;
        set
        {
            _prestigeLevel = value;
            SaveChanges();
            Log.Info($"[PrestigeSystem] Prestige increased to level {_prestigeLevel}");
        }
    }

    protected override void OnAwake()
    {
        if (Instance != null)
        {
            GameObject.Destroy();
            return;
        }
        Instance = this;
        Load();
    }

    private void Load()
    {
        if (SaveManager.Instance?.Data?.Prestige != null)
        {
            _prestigeLevel = SaveManager.Instance.Data.Prestige.PrestigeLevel;
            _unlockedUpgrades = SaveManager.Instance.Data.Prestige.UnlockedPrestigeUpgrades ?? new();
            Log.Info($"[PrestigeSystem] Prestige loaded: level {_prestigeLevel}");
        }
    }

    public bool IsUpgradeUnlocked(string upgradeName)
    {
        return _unlockedUpgrades.ContainsKey(upgradeName) && _unlockedUpgrades[upgradeName];
    }

    public void UnlockUpgrade(string upgradeName)
    {
        if (!_unlockedUpgrades.ContainsKey(upgradeName))
        {
            _unlockedUpgrades[upgradeName] = true;
            SaveChanges();
            Log.Info($"[PrestigeSystem] Prestige upgrade unlocked: {upgradeName}");
        }
    }

    private void SaveChanges()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.Prestige.PrestigeLevel = _prestigeLevel;
            SaveManager.Instance.Data.Prestige.UnlockedPrestigeUpgrades = _unlockedUpgrades;
            SaveManager.Instance.Save();
        }
    }
}
