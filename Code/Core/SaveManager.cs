using Sandbox;
using System;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Server-authoritative save manager.
/// ONLY runs on the server (Networking.IsHost).
/// Handles per-player save files with validation, recovery, and audit logging.
/// Integrates with SaveThrottler for batched saves and SaveEventBus for notifications.
/// </summary>
public sealed class SaveManager : Component
{
    public static SaveManager Instance { get; private set; }

    /// <summary>
    /// Current player's save data. Only meaningful on server.
    /// Clients NEVER access this directly.
    /// </summary>
    public GameSaveData Data { get; private set; }

    /// <summary>
    /// Flag indicating SaveData has been fully loaded and is ready for use.
    /// </summary>
    public bool IsDataReady { get; private set; } = false;

    /// <summary>
    /// Current player connection (set on player join).
    /// </summary>
    private Connection _currentConnection;

    private SaveRecovery _recovery;
    private SaveAuditLog _auditLog;
    private SaveThrottler _throttler;
    private GameSaveData _cachedDataForRollback;
    private bool _hasShutdownSaved = false;  // Track if we already saved during shutdown

    protected override void OnAwake()
    {
        // Server-only validation
        if ( !Networking.IsHost )
        {
            Log.Warning( "[SaveManager] Attempted to create SaveManager on client. Destroying." );
            GameObject.Destroy();
            return;
        }

        Instance = this;

        // CRITICAL: Create SaveThrottler AFTER SaveManager is set as Instance
        // This ensures that when SaveThrottler subscribes to SaveEventBus,
        // SaveManager is fully initialized
        _throttler = GameObject.GetComponent<SaveThrottler>();
        if ( _throttler == null )
        {
            _throttler = GameObject.AddComponent<SaveThrottler>();
        }

        if ( SaveConfig.DEBUG_SAVE_LOGGING )
            Log.Info( "[SaveManager] Initialized (server-only)" );
    }

    /// <summary>
    /// Set the player connection for this SaveManager session.
    /// Called when a player joins the server.
    /// </summary>
    public void SetPlayerConnection( Connection connection )
    {
        _currentConnection = connection;

        var playerId = GetPlayerId( connection );
        _recovery = new SaveRecovery( playerId );
        _auditLog = new SaveAuditLog( playerId );

        Load();
        ApplyWeaponIDMigrations(); // Fix old weapon IDs that may exist in old saves
        IsDataReady = true; // Signal that SaveData is fully loaded

        Log.Info( $"[SaveManager] Player {playerId} loaded (save: {GetSaveFileName()})" );
    }

    /// <summary>
    /// Validate and save all pending changes to disk.
    /// Called by SaveThrottler when throttle interval expires or major event occurs.
    /// </summary>
    public void Save()
    {
        Log.Info( "[SaveManager] Save() called" );
        if ( !Networking.IsHost )
        {
            Log.Warning( "[SaveManager] Save() - Not IsHost, returning" );
            return;
        }

        if ( _currentConnection == null || Data == null )
        {
            Log.Warning( $"[SaveManager] Save() - No connection or data: _currentConnection={_currentConnection != null}, Data={Data != null}" );
            return;
        }

        var sw = Stopwatch.StartNew();

        try
        {
            Log.Info( $"[SaveManager] Save() - Starting save. Current EquippedWeapons: [{string.Join( ", ", Data.Inventory.EquippedWeapons.Select( w => w ?? "null" ) )}]" );

            // Cache current state for rollback
            _cachedDataForRollback = new GameSaveData
            {
                Version = Data.Version,
                CurrentLanguage = Data.CurrentLanguage,
                Checksum = Data.Checksum,
                LastModified = Data.LastModified,
                Player = Data.Player,
                Inventory = Data.Inventory,
                Factory = Data.Factory,
                Prestige = Data.Prestige
            };

            // Update metadata
            Data.LastModified = DateTime.UtcNow.ToString( "O" );

            // CRITICAL: Validate entire save before writing
            var validation = SaveValidator.ValidateFull( Data );
            if ( !validation.IsValid )
            {
                Log.Error( $"[SaveManager] Validation FAILED: {validation.ErrorMessage}. Save rejected." );
                SaveEventBus.FireSaveFailed( validation.ErrorMessage );
                return;
            }

            // Create backup before overwriting (skip at shutdown - file system unstable)
            if ( FileSystem.Data != null )
            {
                _recovery.CreateBackup();
            }

            // Write to disk
            var fileName = GetSaveFileName();
            Log.Info( $"[SaveManager] Writing to disk: {fileName}" );

            if ( FileSystem.Data == null )
            {
                Log.Error( "[SaveManager] FileSystem.Data is null - cannot write! (likely shutdown)" );
                return;
            }

            try
            {
                FileSystem.Data.WriteJson( fileName, Data );
                Log.Info( $"[SaveManager] JSON written. EquippedWeapons saved: [{string.Join( ", ", Data.Inventory.EquippedWeapons.Select( w => w ?? "null" ) )}]" );
            }
            catch ( Exception writeEx )
            {
                Log.Error( $"[SaveManager] WriteJson FAILED: {writeEx.Message}\n{writeEx.StackTrace}" );
                throw; // Re-throw to be caught by outer handler
            }

            // Write audit log
            if ( _auditLog != null )
            {
                _auditLog.Save();
            }

            sw.Stop();

            Log.Info( $"[SaveManager] Save SUCCESS: {fileName} ({sw.ElapsedMilliseconds}ms). Active slot: {Data.Inventory.ActiveWeaponIndex}" );
            SaveEventBus.FireSaveComplete( sw.ElapsedMilliseconds );
        }
        catch ( Exception ex )
        {
            sw.Stop();
            Log.Error( $"[SaveManager] Save FAILED: {ex.Message}\n{ex.StackTrace}" );
            SaveEventBus.FireSaveFailed( ex.Message );

            // Don't rollback at shutdown - data in memory is still valid
            if ( _currentConnection != null )
            {
                Log.Info( "[SaveManager] Active connection detected - rolling back to cached state" );
                if ( _cachedDataForRollback != null )
                {
                    Data = _cachedDataForRollback;
                    Log.Warning( "[SaveManager] Rolled back to cached state" );
                }
            }
            else
            {
                Log.Warning( "[SaveManager] No active connection - skipping rollback (shutdown scenario)" );
            }
        }
    }

    /// <summary>
    /// Load player save from disk. Called on player join.
    /// Attempts recovery if main save is corrupted.
    /// </summary>
    private void Load()
    {
        var fileName = GetSaveFileName();

        try
        {
            if ( FileSystem.Data.FileExists( fileName ) )
            {
                Data = FileSystem.Data.ReadJson<GameSaveData>( fileName );

                // Validate loaded data
                var validation = SaveValidator.ValidateFull( Data );
                if ( !validation.IsValid )
                {
                    Log.Error( $"[SaveManager] Loaded save is corrupted: {validation.ErrorMessage}" );
                    throw new Exception( validation.ErrorMessage );
                }

                if ( SaveConfig.DEBUG_SAVE_LOGGING )
                    Log.Info( "[SaveManager] Save file loaded successfully" );
            }
            else
            {
                Log.Info( "[SaveManager] No save file found. Starting new game." );
                Data = new GameSaveData();
            }
        }
        catch ( Exception ex )
        {
            Log.Error( $"[SaveManager] Failed to load save: {ex.Message}. Attempting recovery..." );

            // Try to recover from backup
            var recovered = _recovery.TryRecoverFromBackup();
            if ( recovered != null )
            {
                Data = recovered;
                Log.Warning( "[SaveManager] Recovered from backup" );
            }
            else
            {
                Log.Error( "[SaveManager] Recovery failed. Starting new game." );
                Data = new GameSaveData();
            }
        }
    }

    /// <summary>
    /// Get per-player save file name from Steam ID or local identifier.
    /// </summary>
    private string GetSaveFileName()
    {
        var playerId = GetPlayerId( _currentConnection );
        return $"save_{playerId}.json";
    }

    /// <summary>
    /// Extract unique player identifier from connection (Steam ID or fallback).
    /// </summary>
    private string GetPlayerId( Connection connection )
    {
        if ( connection == null )
            return "default";

        // Use Steam ID if available
        if ( connection.IsActive )
            return connection.SteamId.ToString();

        // Fallback to connection ID
        return connection.Id.ToString();
    }

    /// <summary>
    /// Get current SaveAuditLog instance (for external systems to log events).
    /// </summary>
    public SaveAuditLog GetAuditLog() => _auditLog;

    /// <summary>
    /// Apply weapon ID migrations to fix old/incorrect IDs from previous versions.
    /// Maps old IDs to new ones (e.g., "rocketLauncher" -> "rocket_launcher").
    /// </summary>
    private void ApplyWeaponIDMigrations()
    {
        if ( Data?.Inventory == null )
            return;

        // Define ID mappings: old ID -> new ID
        var idMappings = new System.Collections.Generic.Dictionary<string, string>()
        {
            { "rocketLauncher", "rocket_launcher" },  // Previous camelCase -> underscore format
            { "rocketlauncher", "rocket_launcher" },  // Previous lowercase variant
            { "submachineGun", "submachine_gun" }     // Previous variant
        };

        bool migrationNeeded = false;

        // Fix EquippedWeapons array
        if ( Data.Inventory.EquippedWeapons != null )
        {
            for ( int i = 0; i < Data.Inventory.EquippedWeapons.Length; i++ )
            {
                string weaponId = Data.Inventory.EquippedWeapons[i];
                if ( !string.IsNullOrEmpty( weaponId ) && idMappings.ContainsKey( weaponId ) )
                {
                    Data.Inventory.EquippedWeapons[i] = idMappings[weaponId];
                    migrationNeeded = true;
                    Log.Warning( $"[SaveManager] Migrated EquippedWeapons[{i}]: '{weaponId}' -> '{idMappings[weaponId]}'" );
                }
            }
        }

        // Fix UnlockedWeapons list
        if ( Data.Inventory.UnlockedWeapons != null )
        {
            for ( int i = 0; i < Data.Inventory.UnlockedWeapons.Count; i++ )
            {
                string weaponId = Data.Inventory.UnlockedWeapons[i];
                if ( idMappings.ContainsKey( weaponId ) )
                {
                    Data.Inventory.UnlockedWeapons[i] = idMappings[weaponId];
                    migrationNeeded = true;
                    Log.Warning( $"[SaveManager] Migrated UnlockedWeapons: '{weaponId}' -> '{idMappings[weaponId]}'" );
                }
            }
        }

        if ( migrationNeeded )
        {
            Log.Info( "[SaveManager] Weapon ID migration completed. Save will be updated on next flush." );
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.DataMigration, "Weapon IDs migrated" );
        }
    }

    /// <summary>
    /// <summary>
    /// Immediate flush of pending saves (for graceful shutdown).
    /// </summary>
    public void ForceFlush()
    {
        Log.Info( "[SaveManager] ForceFlush() called - requesting immediate throttler flush" );
        if ( _throttler != null )
        {
            _throttler.ForceFlush();
            Log.Info( "[SaveManager] ForceFlush() - throttler flush completed" );
        }
        else
        {
            Log.Warning( "[SaveManager] ForceFlush() - throttler is null!" );
        }
    }

    /// <summary>
    /// Save immediately and synchronously (for quit/shutdown).
    /// Bypasses throttler and writes directly to disk.
    /// </summary>
    public void SaveNow()
    {
        if ( Data == null || _currentConnection == null )
        {
            Log.Warning( "[SaveManager] SaveNow() skipped - no data or connection" );
            return;
        }

        if ( FileSystem.Data == null )
        {
            Log.Error( "[SaveManager] SaveNow() failed - FileSystem.Data is null" );
            return;
        }

        try
        {
            var fileName = GetSaveFileName();
            Log.Info( $"[SaveManager] SaveNow() - Writing directly to {fileName}" );

            FileSystem.Data.WriteJson( fileName, Data );

            Log.Info( $"[SaveManager] SaveNow() SUCCESS - Equipment: [{string.Join( ", ", Data.Inventory.EquippedWeapons.Select( w => w ?? "null" ) )}]" );
        }
        catch ( Exception ex )
        {
            Log.Error( $"[SaveManager] SaveNow() FAILED: {ex.Message}\n{ex.StackTrace}" );
        }
    }

    /// <summary>
    /// Called when component is disabled. This is BEFORE OnDestroy, still have FileSystem access.
    /// Perform a final guaranteed save before shutdown.
    /// </summary>
    protected override void OnDisabled()
    {
        Log.Info( "[SaveManager] OnDisabled() - Component disabling, attempting final save" );

        // First, flush any pending changes queued in throttler (while FileSystem still available)
        if ( SaveThrottler.Instance != null && SaveThrottler.Instance.GetPendingChangeCount() > 0 )
        {
            Log.Info( $"[SaveManager] OnDisabled() - Flushing {SaveThrottler.Instance.GetPendingChangeCount()} pending changes from throttler" );
            SaveThrottler.Instance.ForceFlush();
        }

        // Then attempt direct save as final backup
        SaveNow();

        Log.Info( "[SaveManager] OnDisabled() - Shutdown save complete" );
    }

    /// <summary>
    /// Game is shutting down - final cleanup.
    /// </summary>
    protected override void OnDestroy()
    {
        Log.Info( "[SaveManager] OnDestroy() - Shutdown complete" );
    }
}