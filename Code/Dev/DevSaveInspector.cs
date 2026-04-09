using Sandbox;
using System;

/// <summary>
/// Dev-only tool for inspecting, testing, and debugging save data.
/// Add this component to a GameObject in Dev scene for hotkey access.
/// Usage: F12 = Print Save, F11 = Add Scrap, F10 = Toggle Debug Logging
/// </summary>
public class DevSaveInspector : Component
{
    protected override void OnUpdate()
    {
        if ( !Networking.IsHost )
            return; // Server only

        // F12: Print entire save to console
        if ( Input.Pressed( "F12" ) )
        {
            PrintSave();
        }

        // F11: Add 10000 scrap (testing)
        if ( Input.Pressed( "F11" ) )
        {
            AddTestScrap( 10000 );
        }

        // F10: Print throttler status
        if ( Input.Pressed( "F10" ) )
        {
            PrintThrottlerStatus();
        }
    }

    /// <summary>
    /// Print entire save structure to console in readable format.
    /// </summary>
    public void PrintSave()
    {
        if ( SaveManager.Instance == null || SaveManager.Instance.Data == null )
        {
            Log.Warning( "[DevSaveInspector] SaveManager.Instance or SaveManager.Instance.Data is null - SaveManager not initialized yet" );
            return;
        }

        var data = SaveManager.Instance.Data;

        Log.Info( "================ SAVE DATA DUMP ================" );
        Log.Info( $"Version: {data.Version}" );
        Log.Info( $"LastModified: {data.LastModified}" );
        Log.Info( $"Language: {data.CurrentLanguage}" );
        Log.Info( "" );

        // Player Data
        Log.Info( "--- PLAYER DATA ---" );
        Log.Info( $"  TotalScrap: {data.Player?.TotalScrap ?? 0}" );
        if ( data.Player?.GlobalUpgrades != null )
        {
            Log.Info( $"  Global Upgrades ({data.Player.GlobalUpgrades.Count}):" );
            foreach ( var kvp in data.Player.GlobalUpgrades )
            {
                Log.Info( $"    {kvp.Key}: Level {kvp.Value}" );
            }
        }
        Log.Info( "" );

        // Inventory Data
        Log.Info( "--- INVENTORY DATA ---" );
        Log.Info( $"  Collected Items: {data.Inventory?.CollectedItems?.Count ?? 0}" );
        Log.Info( $"  Unlocked Weapons: {data.Inventory?.UnlockedWeapons?.Count ?? 0}" );
        if ( data.Inventory?.UnlockedWeapons != null )
        {
            foreach ( var weapon in data.Inventory.UnlockedWeapons )
                Log.Info( $"    - {weapon}" );
        }
        Log.Info( $"  Unlocked Utilities: {data.Inventory?.UnlockedUtilities?.Count ?? 0}" );
        if ( data.Inventory?.EquippedWeapons != null )
        {
            Log.Info( $"  Equipped Weapons:" );
            for ( int i = 0; i < data.Inventory.EquippedWeapons.Length; i++ )
                Log.Info( $"    Slot {i}: {data.Inventory.EquippedWeapons[i] ?? "empty"}" );
            Log.Info( $"  Active Slot: {data.Inventory.ActiveWeaponIndex}" );
        }
        Log.Info( "" );

        // Factory Data
        Log.Info( "--- FACTORY DATA ---" );
        Log.Info( $"  Tier: {data.Factory?.Tier ?? 1}" );
        if ( data.Factory?.MachineUpgrades != null )
        {
            Log.Info( $"  Machine Upgrades ({data.Factory.MachineUpgrades.Count}):" );
            foreach ( var kvp in data.Factory.MachineUpgrades )
            {
                Log.Info( $"    {kvp.Key}: Level {kvp.Value}" );
            }
        }
        Log.Info( $"  Seller Queue: {data.Factory?.SellerQueue?.Count ?? 0} items" );
        Log.Info( "" );

        // Prestige Data
        Log.Info( "--- PRESTIGE DATA ---" );
        Log.Info( $"  Prestige Level: {data.Prestige?.PrestigeLevel ?? 0}" );
        Log.Info( $"  Unlocked Prestige Upgrades: {data.Prestige?.UnlockedPrestigeUpgrades?.Count ?? 0}" );
        if ( data.Prestige?.UnlockedPrestigeUpgrades != null )
        {
            foreach ( var upgrade in data.Prestige.UnlockedPrestigeUpgrades )
            {
                Log.Info( $"    {upgrade.Key}: {upgrade.Value}" );
            }
        }
        Log.Info( "" );

        // Audit Log
        if ( SaveManager.Instance != null )
        {
            var auditLog = SaveManager.Instance.GetAuditLog();
            if ( auditLog != null )
            {
                var entries = auditLog.GetEntries();
                Log.Info( $"--- AUDIT LOG ({entries.Count} entries) ---" );
                int shown = Math.Min( entries.Count, 5 );
                for ( int i = entries.Count - shown; i < entries.Count; i++ )
                {
                    var entry = entries[i];
                    Log.Info( $"  [{entry.Timestamp}] {entry.ChangeType}: {entry.OldValue} → {entry.NewValue} ({entry.Reason})" );
                }
            }
        }

        Log.Info( "===============================================" );
    }

    /// <summary>
    /// Add test scrap for quick testing (dev only).
    /// </summary>
    private void AddTestScrap( float amount )
    {
        if ( PlayerStats.Local != null )
        {
            PlayerStats.Local.AddScrap( amount );
            Log.Info( $"[DevSaveInspector] Added {amount} test scrap. Total: {PlayerStats.Local.TotalScrap}" );
        }
        else
        {
            Log.Warning( "[DevSaveInspector] PlayerStats.Local is null" );
        }
    }



    /// <summary>
    /// Export save as JSON string to copy/paste.
    /// </summary>
    public void ExportSaveAsJson()
    {
        if ( SaveManager.Instance == null || SaveManager.Instance.Data == null )
        {
            Log.Warning( "[DevSaveInspector] SaveManager not initialized" );
            return;
        }

        var json = System.Text.Json.JsonSerializer.Serialize( SaveManager.Instance.Data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true } );
        Log.Info( json );
    }

    /// <summary>
    /// Manual trigger to save immediately (bypass throttle).
    /// </summary>
    public void ManualSave()
    {
        if ( SaveManager.Instance != null )
        {
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ManualSave, "DEV: Manual save triggered" );
            SaveManager.Instance.Save();
            Log.Info( "[DevSaveInspector] Manual save triggered" );
        }
    }

    /// <summary>
    /// Manual trigger to force flush throttler immediately.
    /// </summary>
    public void ForceFlushThrottler()
    {
        if ( SaveThrottler.Instance != null )
        {
            SaveThrottler.Instance.ForceFlush();
            Log.Info( "[DevSaveInspector] Throttler flush forced" );
        }
    }

    /// <summary>
    /// Print info about current throttler state.
    /// </summary>
    public void PrintThrottlerStatus()
    {
        if ( SaveThrottler.Instance == null )
        {
            Log.Warning( "[DevSaveInspector] SaveThrottler.Instance is null" );
            return;
        }

        Log.Info( $"[SaveThrottler Status]" );
        Log.Info( $"  Pending changes: {SaveThrottler.Instance.GetPendingChangeCount()}" );
        Log.Info( $"  Time to next flush: {SaveThrottler.Instance.GetTimeToNextFlush():F2}s" );
    }

    /// <summary>
    /// Add specific scrap amount by draining then adding back (resets to amount).
    /// </summary>
    public void SetScrapDebug( float amount )
    {
        if ( PlayerStats.Local == null )
        {
            Log.Warning( "[DevSaveInspector] PlayerStats.Local is null" );
            return;
        }

        amount = SaveValidator.ClampScrap( amount );

        // Drain current scrap, then add desired amount
        float current = PlayerStats.Local.TotalScrap;
        if ( current > 0 )
        {
            PlayerStats.Local.SpendScrap( current );
        }

        if ( amount > 0 )
        {
            PlayerStats.Local.AddScrap( amount );
        }

        Log.Warning( $"[DevSaveInspector] Scrap set to {amount} (DEV ONLY)" );
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ManualSave, $"DEV: Scrap set to {amount}" );
    }

    /// <summary>
    /// Add an upgrade to player (for testing).
    /// </summary>
    public void AddUpgradeDebug( string upgradeId, int level = 1 )
    {
        if ( SaveManager.Instance?.Data?.Player == null )
        {
            Log.Warning( "[DevSaveInspector] SaveManager data unavailable" );
            return;
        }

        level = SaveValidator.ClampUpgradeLevel( level );
        SaveManager.Instance.Data.Player.GlobalUpgrades[upgradeId] = level;

        Log.Warning( $"[DevSaveInspector] Upgrade '{upgradeId}' set to level {level} (DEV ONLY)" );
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ManualSave, $"DEV: Upgrade {upgradeId} = {level}" );
    }

    /// <summary>
    /// Unlock a weapon for player (for testing).
    /// </summary>
    public void UnlockWeaponDebug( string weaponId )
    {
        if ( SaveManager.Instance?.Data?.Inventory == null )
        {
            Log.Warning( "[DevSaveInspector] SaveManager data unavailable" );
            return;
        }

        if ( !SaveManager.Instance.Data.Inventory.UnlockedWeapons.Contains( weaponId ) )
        {
            SaveManager.Instance.Data.Inventory.UnlockedWeapons.Add( weaponId );
            Log.Warning( $"[DevSaveInspector] Weapon '{weaponId}' unlocked (DEV ONLY)" );
        }
        else
        {
            Log.Info( $"[DevSaveInspector] Weapon '{weaponId}' already unlocked" );
        }

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ManualSave, $"DEV: Weapon {weaponId} unlocked" );
    }
}
