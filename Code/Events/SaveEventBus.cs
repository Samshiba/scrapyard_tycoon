using Sandbox;
using System;

/// <summary>
/// Pub/Sub event system for save notifications.
/// Systems emit SaveNeeded events instead of calling SaveManager.Save() directly.
/// SaveThrottler consumes these events and batches them for efficiency.
/// </summary>
public static class SaveEventBus
{
    /// <summary>
    /// Reasons why a save was triggered (for logging and categorization).
    /// </summary>
    public enum SaveReason
    {
        /// <summary>Player scrap balance changed</summary>
        ScrapChanged,
        /// <summary>Item collected or removed</summary>
        InventoryChanged,
        /// <summary>Weapon equipped or unequipped</summary>
        WeaponEquipped,
        /// <summary>Weapon or utility unlocked</summary>
        ItemUnlocked,
        /// <summary>Global upgrade purchased</summary>
        GlobalUpgradeChanged,
        /// <summary>Machine upgrade applied</summary>
        MachineUpgradeChanged,
        /// <summary>Prestige level or upgrades changed</summary>
        PrestigeChanged,
        /// <summary>Seller queue status changed</summary>
        SellerQueueChanged,
        /// <summary>Manual save requested by player (escape menu, etc)</summary>
        ManualSave,
        /// <summary>Player disconnecting - immediate save required</summary>
        PlayerDisconnect,
        /// <summary>Emergency save on crash recovery</summary>
        CrashRecovery,
        /// <summary>Data migrated from old format (ID corrections, etc)</summary>
        DataMigration,
        /// <summary>Player statistics updated</summary>
        PlayerStatUpdate,
        /// <summary>World statistics updated</summary>
        WorldStatUpdate,
    }

    /// <summary>
    /// Fired when a system detects a change that needs saving.
    /// SaveThrottler listens to this and batches saves.
    /// </summary>
    public static event Action<SaveReason, string, string> OnSaveNeeded;

    /// <summary>
    /// Fired after SaveManager successfully saved all pending changes.
    /// </summary>
    public static event Action<float> OnSaveComplete; // elapsed time for save

    /// <summary>
    /// Fired if SaveManager encounters an error during save.
    /// </summary>
    public static event Action<string> OnSaveFailed; // error message

    /// <summary>
    /// Notify the save system that a change needs persisting.
    /// </summary>
    /// <param name="reason">Why the save is needed (for filtering/logging)</param>
    /// <param name="details">Optional details about what changed (e.g., "scrap +500")</param>
    /// <param name="targetSteamId">The Steam ID of the player for whom the change is intended</param>
    public static void NotifyChange( SaveReason reason, string details = "", string targetSteamId = null )
    {
        if ( SaveConfig.DEBUG_SAVE_LOGGING )
        {
            Log.Info( $"[SaveEventBus] Change queued: {reason} | Details: {details} | Target: {targetSteamId ?? "FACTORY"}" );
        }

        OnSaveNeeded?.Invoke( reason, details, targetSteamId );
    }

    /// <summary>Internal: Called by SaveManager after successful save</summary>
    internal static void FireSaveComplete( float elapsedMs )
    {
        OnSaveComplete?.Invoke( elapsedMs );
    }

    /// <summary>Internal: Called by SaveManager if save fails</summary>
    internal static void FireSaveFailed( string errorMessage )
    {
        OnSaveFailed?.Invoke( errorMessage );
    }

    /// <summary>
    /// Clear all listeners (use in tests or scene reload).
    /// </summary>
    public static void ClearAllListeners()
    {
        OnSaveNeeded = null;
        OnSaveComplete = null;
        OnSaveFailed = null;
    }
}
