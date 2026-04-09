using Sandbox;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Dev menu for quick save debugging/testing.
/// Server-only component. Add to your dev scene.
/// Console commands: dev_print_save, dev_add_scrap, dev_set_scrap, dev_unlock_weapons, dev_manual_save
/// Press backtick (`) to show command list.
/// </summary>
public partial class DevMenu : Component
{
    private static DevMenu _instance;

    protected override void OnAwake()
    {
        if ( !Networking.IsHost )
        {
            Destroy();
            return;
        }

        _instance = this;
        Log.Info( "[DevMenu] Dev menu initialized. Press ` for commands or call DevMenu methods directly." );
    }

    protected override void OnUpdate()
    {
        if ( !Networking.IsHost )
            return;

        // Backtick to show dev menu help
        if ( Input.Pressed( "`" ) )
        {
            PrintHelp();
        }
    }

    /// <summary>
    /// Publicly callable: Print entire save data.
    /// </summary>
    public static void PrintSaveDataPublic()
    {
        if ( _instance != null )
            _instance.PrintSaveData();
    }

    /// <summary>
    /// Publicly callable: Add scrap.
    /// </summary>
    public static void AddScrapPublic( float amount = 10000 )
    {
        if ( _instance != null )
            _instance.AddScrap( amount );
    }

    /// <summary>
    /// Publicly callable: Set scrap to exact amount.
    /// </summary>
    public static void SetScrapPublic( float amount )
    {
        if ( _instance != null )
            _instance.SetScrap( amount );
    }

    /// <summary>
    /// Publicly callable: Unlock weapons.
    /// </summary>
    public static void UnlockWeaponsPublic()
    {
        if ( _instance != null )
            _instance.UnlockAllWeapons();
    }

    /// <summary>
    /// Publicly callable: Manual save.
    /// </summary>
    public static void ManualSavePublic()
    {
        if ( _instance != null )
            _instance.ManualSave();
    }

    /// <summary>
    /// Publicly callable: Force flush throttler.
    /// </summary>
    public static void FlushThrottlerPublic()
    {
        if ( _instance != null )
            _instance.ForceFlush();
    }

    /// <summary>
    /// Publicly callable: Show throttler status.
    /// </summary>
    public static void ThrottlerStatusPublic()
    {
        if ( _instance != null )
            _instance.PrintThrottlerStatus();
    }

    /// <summary>
    /// Publicly callable: Reset all saves.
    /// </summary>
    public static void ResetSavePublic()
    {
        if ( _instance != null )
            _instance.ResetSaveData();
    }

    private void PrintHelp()
    {
        Log.Info( "========== DEV MENU COMMANDS ==========" );
        Log.Info( "dev_print_save          - Dump entire save data to console" );
        Log.Info( "dev_add_scrap [amount]  - Add scrap (default 10000)" );
        Log.Info( "dev_set_scrap [amount]  - Set scrap to exact amount" );
        Log.Info( "dev_unlock_weapons      - Unlock all weapons" );
        Log.Info( "dev_manual_save         - Trigger save immediately" );
        Log.Info( "dev_flush_throttler     - Force throttler to flush now" );
        Log.Info( "dev_throttler_status    - Show current throttler state" );
        Log.Info( "dev_reset_save          - Clear and reset save data" );
        Log.Info( "======================================" );
    }

    private void PrintSaveData()
    {
        if ( SaveManager.Instance == null )
        {
            Log.Warning( "[DevMenu] SaveManager.Instance is null" );
            return;
        }

        var inspector = GetComponent<DevSaveInspector>();
        if ( inspector == null )
        {
            // Find or create DevSaveInspector in scene
            inspector = Scene.GetAllComponents<DevSaveInspector>().FirstOrDefault();
        }

        if ( inspector != null )
        {
            inspector.PrintSave();
        }
        else
        {
            Log.Warning( "[DevMenu] No DevSaveInspector found in scene" );
        }
    }

    private void AddScrap( float amount )
    {
        if ( PlayerStats.Local == null )
        {
            Log.Warning( "[DevMenu] PlayerStats.Local is null" );
            return;
        }

        PlayerStats.Local.AddScrap( amount );
        Log.Info( $"[DevMenu] Added {amount} scrap. Total: {PlayerStats.Local.TotalScrap}" );
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ManualSave, $"DEV: Added {amount} scrap" );
    }

    private void SetScrap( float amount )
    {
        if ( PlayerStats.Local == null )
        {
            Log.Warning( "[DevMenu] PlayerStats.Local is null" );
            return;
        }

        // Drain to zero, then add target amount
        float current = PlayerStats.Local.TotalScrap;
        if ( current > 0 )
        {
            PlayerStats.Local.SpendScrap( current );
        }

        if ( amount > 0 )
        {
            PlayerStats.Local.AddScrap( amount );
        }

        Log.Warning( $"[DevMenu] Scrap set to {amount}" );
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ManualSave, $"DEV: Scrap set to {amount}" );
    }

    private void UnlockAllWeapons()
    {
        if ( SaveManager.Instance?.Data?.Inventory == null )
        {
            Log.Warning( "[DevMenu] SaveManager inventory unavailable" );
            return;
        }

        // Add example weapons
        var inventory = SaveManager.Instance.Data.Inventory;
        inventory.UnlockedWeapons.Add( "rifle_a" );
        inventory.UnlockedWeapons.Add( "shotgun_b" );
        inventory.UnlockedWeapons.Add( "pistol_c" );

        Log.Warning( "[DevMenu] Unlocked example weapons (DEV ONLY)" );
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ManualSave, "DEV: Unlocked weapons" );
    }

    private void ManualSave()
    {
        if ( SaveManager.Instance == null )
        {
            Log.Warning( "[DevMenu] SaveManager.Instance is null" );
            return;
        }

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ManualSave, "DEV: Manual save" );
        SaveManager.Instance.Save();
        Log.Info( "[DevMenu] Save triggered" );
    }

    private void ForceFlush()
    {
        if ( SaveThrottler.Instance == null )
        {
            Log.Warning( "[DevMenu] SaveThrottler.Instance is null" );
            return;
        }

        SaveThrottler.Instance.ForceFlush();
        Log.Info( "[DevMenu] Throttler flushed" );
    }

    private void PrintThrottlerStatus()
    {
        if ( SaveThrottler.Instance == null )
        {
            Log.Warning( "[DevMenu] SaveThrottler.Instance is null" );
            return;
        }

        Log.Info( $"[SaveThrottler Status]" );
        Log.Info( $"  Pending changes: {SaveThrottler.Instance.GetPendingChangeCount()}" );
        Log.Info( $"  Time to next flush: {SaveThrottler.Instance.GetTimeToNextFlush():F2}s" );
    }

    private void ResetSaveData()
    {
        if ( SaveManager.Instance == null )
        {
            Log.Warning( "[DevMenu] SaveManager.Instance is null" );
            return;
        }

        Log.Warning( "[DevMenu] Save files cleared. Fresh save will be created on next action." );
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ManualSave, "DEV: Reset save" );
    }
}
