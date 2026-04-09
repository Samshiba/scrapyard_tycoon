using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Root save data structure. Contains all player progression.
/// Server-authoritative: only the server loads/saves this.
/// </summary>
public class GameSaveData
{
    /// <summary>Schema version for migration support in future updates.</summary>
    public string Version { get; set; } = "1.0";

    /// <summary>CRC32 checksum to detect file corruption or tampering (computed on save).</summary>
    public string Checksum { get; set; } = "";

    /// <summary>ISO 8601 timestamp when this save was last modified (UTC).</summary>
    public string LastModified { get; set; } = "";

    /// <summary>Player's selected language preference.</summary>
    public string CurrentLanguage { get; set; } = "en";

    /// <summary>Player progression data (scrap, upgrades).</summary>
    public PlayerSaveData Player { get; set; } = new();

    /// <summary>Inventory and equipment data.</summary>
    public InventorySaveData Inventory { get; set; } = new();

    /// <summary>Factory and production data.</summary>
    public FactorySaveData Factory { get; set; } = new();

    /// <summary>Prestige system data (meta-progression).</summary>
    public PrestigeSaveData Prestige { get; set; } = new();
}

/// <summary>
/// Prestige system data (meta-progression unlocked through resets).
/// </summary>
public class PrestigeSaveData
{
    /// <summary>Current prestige level (0-100).</summary>
    public int PrestigeLevel { get; set; } = 0;

    /// <summary>
    /// Prestige upgrades that have been permanently unlocked.
    /// Key: upgrade ID, Value: always true (once unlocked, stays unlocked).
    /// </summary>
    public Dictionary<string, bool> UnlockedPrestigeUpgrades { get; set; } = new();
}

/// <summary>
/// Player economy and global progression data.
/// </summary>
public class PlayerSaveData
{
    /// <summary>Total scrap currency collected (0 to MAX_SCRAP).</summary>
    public float TotalScrap { get; set; } = 100;

    /// <summary>
    /// Global upgrades purchased by the player.
    /// Key: upgrade ID, Value: upgrade level (0+).
    /// </summary>
    public Dictionary<string, int> GlobalUpgrades { get; set; } = new();
}

/// <summary>
/// Inventory, weapons, and equipment data.
/// </summary>
public class InventorySaveData
{
    /// <summary>Items currently in the player's backpack.</summary>
    public List<ItemData> CollectedItems { get; set; } = new();

    /// <summary>IDs of all weapons the player has unlocked.</summary>
    public List<string> UnlockedWeapons { get; set; } = new();

    /// <summary>IDs of all utilities the player has unlocked.</summary>
    public List<string> UnlockedUtilities { get; set; } = new();

    /// <summary>
    /// Weapon IDs currently equipped in each slot (4 slots total).
    /// Can contain null/empty strings for empty slots.
    /// </summary>
    public string[] EquippedWeapons { get; set; } = new string[4];

    /// <summary>Currently active weapon slot index (0-3).</summary>
    public int ActiveWeaponIndex { get; set; } = 0;
}

/// <summary>
/// Factory production and machine data.
/// </summary>
public class FactorySaveData
{
    /// <summary>
    /// Machine upgrades applied to the factory.
    /// Key: machine upgrade ID, Value: upgrade level (0+).
    /// </summary>
    public Dictionary<string, int> MachineUpgrades { get; set; } = new();

    /// <summary>
    /// Items currently queued for processing by the seller machine.
    /// Changed from Queue to List for better JSON serialization.
    /// Items are processed in order (index 0 is next to process).
    /// </summary>
    public List<ItemData> SellerQueue { get; set; } = new();

    /// <summary>Current factory tier (1+). Determines production capabilities.</summary>
    public int Tier { get; set; } = 1;
}