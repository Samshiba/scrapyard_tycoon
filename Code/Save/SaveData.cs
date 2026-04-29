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
    [JsonPropertyName( "current_scrap" )] public double TotalScrap { get; set; } = 7929999798.9796;
    [JsonPropertyName( "prestige_level" )] public int PrestigeLevel { get; set; } = 0;

    // World state
    [JsonPropertyName( "world_state" )] public WorldStateData WorldState { get; set; } = new();
    [JsonPropertyName( "statistics" )] public FactoryStatsData Stats { get; set; } = new();

    // Accessors for easier data manipulation
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

    [JsonPropertyName( "statistics" )] public PlayerStatsData Stats { get; set; } = new();

    // Accessors for easier data manipulation
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

public class FactoryStatsData
{
    [JsonPropertyName( "time_played" )] public float TimePlayed { get; set; }
    [JsonPropertyName( "max_damage_hit" )] public double MaxDamageHit { get; set; }
    [JsonPropertyName( "props_destroyed" )] public long PropsDestroyed { get; set; }
    [JsonPropertyName( "scrap_gained_session" )] public double ScrapGainedSession { get; set; }
    [JsonPropertyName( "gibs_dropped_session" )] public int GibsDroppedSession { get; set; }
}

public class PlayerStatsData
{
    // --- LIFETIME CORE STATS ---
    [JsonPropertyName( "lifetime_playtime" )] public float LifetimePlaytime { get; set; }
    [JsonPropertyName( "total_scrap_collected" )] public double TotalScrapCollected { get; set; }
    [JsonPropertyName( "largest_scrap_gain" )] public double LargestScrapGain { get; set; }

    // --- COMBAT STATS ---
    [JsonPropertyName( "total_damage_dealt" )] public double TotalDamageDealt { get; set; }
    [JsonPropertyName( "highest_damage_hit" )] public double HighestDamageHit { get; set; }
    [JsonPropertyName( "total_attacks" )] public int TotalAttacks { get; set; }
    [JsonPropertyName( "total_critical_hits" )] public int TotalCriticalHits { get; set; }
    [JsonPropertyName( "critical_damage" )] public double CriticalDamage { get; set; }

    // --- TARGETS/PROPS ---
    [JsonPropertyName( "total_targets_destroyed" )] public long TotalTargetsDestroyed { get; set; }
    [JsonPropertyName( "total_props_destroyed" )] public long TotalPropsDestroyed { get; set; }

    // --- PROGRESSION ---
    [JsonPropertyName( "times_prestiged" )] public int TimesPrestiged { get; set; }
    [JsonPropertyName( "prestige_points" )] public int PrestigePoints { get; set; }
    [JsonPropertyName( "factory_resets" )] public int FactoryResets { get; set; }

    // --- UNLOCKS & PURCHASES ---
    [JsonPropertyName( "upgrades_unlocked" )] public int UpgradesUnlocked { get; set; }
    [JsonPropertyName( "weapons_unlocked" )] public int WeaponsUnlocked { get; set; }
    [JsonPropertyName( "total_money_spent" )] public double TotalMoneySpent { get; set; }
    [JsonPropertyName( "largest_single_purchase" )] public double LargestSinglePurchase { get; set; }

    // --- VANITY STATS (Per-weapon and Per-prop breakdown) ---
    [JsonPropertyName( "weapon_vanity" )] public Dictionary<string, WeaponVanityStat> WeaponVanity { get; set; } = new();
    [JsonPropertyName( "prop_vanity" )] public Dictionary<string, PropVanityStat> PropVanity { get; set; } = new();
}

/// <summary>
/// Per-weapon breakdown stats (targets destroyed, damage dealt, etc).
/// </summary>
public class WeaponVanityStat
{
    [JsonPropertyName( "targets_destroyed" )] public int TargetsDestroyed { get; set; }
    [JsonPropertyName( "total_damage" )] public double TotalDamage { get; set; }
    [JsonPropertyName( "max_damage_hit" )] public double MaxDamageHit { get; set; }
    [JsonPropertyName( "critical_hits" )] public int CriticalHits { get; set; }
}

/// <summary>
/// Per-prop breakdown stats (times destroyed, scrap gained, etc).
/// </summary>
public class PropVanityStat
{
    [JsonPropertyName( "times_destroyed" )] public int TimesDestroyed { get; set; }
    [JsonPropertyName( "total_scrap_dropped" )] public double TotalScrapDropped { get; set; }
    [JsonPropertyName( "number_of_gibs_dropped" )] public int NumberOfGibsDropped { get; set; }
}
