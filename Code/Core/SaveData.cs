using System.Collections.Generic;
using System.Text.Json.Serialization;

public class GameSaveData
{
    public string Version { get; set; } = "1.0";
    public PlayerSaveData Player { get; set; } = new();
    public InventorySaveData Inventory { get; set; } = new();
    public FactorySaveData Factory { get; set; } = new();
    public PrestigeSaveData Prestige { get; set; } = new();
}

public class PrestigeSaveData
{
    public int PrestigeLevel { get; set; } = 0;
    public Dictionary<string, bool> UnlockedPrestigeUpgrades { get; set; } = new();
}

public class PlayerSaveData
{
    public float TotalScrap { get; set; } = 0;
    public Dictionary<string, int> GlobalUpgrades { get; set; } = new();
}

public class InventorySaveData
{
    public List<ItemData> CollectedItems { get; set; } = new();

    public List<string> UnlockedWeapons { get; set; } = new();
    public List<string> UnlockedUtilities { get; set; } = new();

    public string[] EquippedWeapons { get; set; } = new string[4];
    public int ActiveWeaponIndex { get; set; } = 0;
}

public class FactorySaveData
{
    public Dictionary<string, int> MachineUpgrades { get; set; } = new();

    public Queue<ItemData> SellerQueue { get; set; } = new();

    public int Tier { get; set; } = 1;
}