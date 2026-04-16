using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class FactoryWorldData
{
    // Metadata
    [JsonPropertyName( "id" )] public string FactoryId { get; set; }
    [JsonPropertyName( "host_steam_id" )] public string HostSteamId { get; set; }
    [JsonPropertyName( "save_name" )] public string SaveName { get; set; } = "New Factory";
    [JsonPropertyName( "version" )] public string Version { get; set; } = "2.0";
    [JsonPropertyName( "is_sandbox" )] public bool IsSandbox { get; set; } = false;

    // Economy and progression
    [JsonPropertyName( "current_scrap" )] public double TotalScrap { get; set; } = 0;
    [JsonPropertyName( "prestige_level" )] public int PrestigeLevel { get; set; } = 0;

    [JsonPropertyName( "world_state" )] public WorldStateData WorldState { get; set; } = new();

    [JsonIgnore] public int Tier { get => WorldState.Tier; set => WorldState.Tier = value; }
    [JsonIgnore] public Dictionary<string, int> GlobalUpgrades { get => WorldState.GlobalUpgrades; set => WorldState.GlobalUpgrades = value; }
    [JsonIgnore] public Dictionary<string, bool> UnlockedPrestigeUpgrades { get => WorldState.UnlockedPrestigeUpgrades; set => WorldState.UnlockedPrestigeUpgrades = value; }
    [JsonIgnore] public List<string> UnlockedWeapons { get => WorldState.UnlockedWeapons; set => WorldState.UnlockedWeapons = value; }
    [JsonIgnore] public List<string> UnlockedUtilities { get => WorldState.UnlockedUtilities; set => WorldState.UnlockedUtilities = value; }
    [JsonIgnore] public Dictionary<string, int> MachineUpgrades { get => WorldState.MachineUpgrades; set => WorldState.MachineUpgrades = value; }
    [JsonIgnore] public List<ItemStack> SellerQueue { get => WorldState.SellerQueue; set => WorldState.SellerQueue = value; }
}

public class WorldStateData
{
    [JsonPropertyName( "tier" )] public int Tier { get; set; } = 1;
    [JsonPropertyName( "global_upgrades" )] public Dictionary<string, int> GlobalUpgrades { get; set; } = new();
    [JsonPropertyName( "unlocked_prestige_upgrades" )] public Dictionary<string, bool> UnlockedPrestigeUpgrades { get; set; } = new();
    [JsonPropertyName( "unlocked_weapons" )] public List<string> UnlockedWeapons { get; set; } = ["bat"];
    [JsonPropertyName( "unlocked_utilities" )] public List<string> UnlockedUtilities { get; set; } = new();
    [JsonPropertyName( "machine_upgrades" )] public Dictionary<string, int> MachineUpgrades { get; set; } = new();
    [JsonPropertyName( "seller_queue" )] public List<ItemStack> SellerQueue { get; set; } = new();
}

public class PlayerSessionData
{
    // Metadata
    [JsonPropertyName( "steam_id" )] public string PlayerSteamId { get; set; }
    [JsonPropertyName( "factory_id" )] public string FactoryId { get; set; }

    [JsonPropertyName( "inventory_state" )] public InventoryStateData Inventory { get; set; } = new();

    // Inventory and equipment
    [JsonIgnore] public string[] EquippedWeapons { get => Inventory.EquippedWeapons; set => Inventory.EquippedWeapons = value; }
    [JsonIgnore] public int ActiveWeaponIndex { get => Inventory.ActiveWeaponIndex; set => Inventory.ActiveWeaponIndex = value; }
    [JsonIgnore] public List<ItemStack> CollectedItems { get => Inventory.CollectedItems; set => Inventory.CollectedItems = value; }
}

public class InventoryStateData
{
    [JsonPropertyName( "equipped_weapons" )] public string[] EquippedWeapons { get; set; } = ["bat", null, null, null];
    [JsonPropertyName( "active_weapon_index" )] public int ActiveWeaponIndex { get; set; } = 0;
    [JsonPropertyName( "collected_items" )] public List<ItemStack> CollectedItems { get; set; } = new();
}
