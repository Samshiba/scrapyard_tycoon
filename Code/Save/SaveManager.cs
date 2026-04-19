using Sandbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Server-authoritative Save Manager (Cloud-Based via unified Sync endpoint).
/// ONLY runs on the server (Networking.IsHost).
/// Uses Steam ticket authentication with server-side anti-cheat validation.
/// </summary>
public sealed class SaveManager : Component, Component.INetworkListener
{
    public static SaveManager Instance { get; private set; }

    public bool IsFactoryReady { get; private set; } = false;

    public FactoryWorldData CurrentFactory { get; private set; }
    public Dictionary<string, PlayerSessionData> ActivePlayers { get; private set; } = new();

    private string _currentFactoryId = "factory_demo_001";

    private SaveAuditLog _auditLog;
    private SaveThrottler _throttler;

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

        _throttler = GameObject.GetComponent<SaveThrottler>() ?? GameObject.AddComponent<SaveThrottler>();

        _auditLog = new SaveAuditLog( "host_server" );

        if ( SaveConfig.DEBUG_SAVE_LOGGING )
            Log.Info( $"[SaveManager] Initialized (server-only)" );
    }

    protected override void OnStart()
    {
        if ( !Networking.IsHost ) return;
        _ = LoadFactoryWorldAsync();
    }

    public async void OnActive( Connection channel )
    {
        if ( !Networking.IsHost ) return;

        while ( !IsFactoryReady )
        {
            await Task.Delay( 100 );
        }

        // Initialize new player with default data if they don't exist yet
        if ( channel != null && channel.IsActive )
        {
            string playerSteamId = channel.SteamId.ToString();
            
            if ( !ActivePlayers.ContainsKey( playerSteamId ) )
            {
                Log.Info( $"[SaveManager] New player joined: {playerSteamId}. Creating default player data." );

                var newPlayerData = new PlayerSessionData
                {
                    PlayerSteamId = playerSteamId,
                    FactoryId = CurrentFactory.FactoryId,
                    Inventory = new InventoryStateData
                    {
                        EquippedWeapons = new[] { "bat", null, null, null },
                        ActiveWeaponIndex = 0,
                        CollectedItems = new()
                    }
                };

                ActivePlayers[playerSteamId] = newPlayerData;
                
                // Mark player as dirty so their initial data gets synced to backend
                SaveEventBus.NotifyChange( SaveEventBus.SaveReason.DataMigration, "New player initialized with default bat weapon", playerSteamId );

                Log.Info( $"[SaveManager] Initialized player {playerSteamId} with default bat weapon" );
            }
        }
    }

    // ==========================================
    // 1. LOADING
    // ==========================================

    private async Task LoadFactoryWorldAsync()
    {
        Log.Info( "[SaveManager] Fetching complete world state..." );

        string mySteamId = Connection.Local.SteamId.ToString();
        string myToken = await GetSboxToken( Connection.Local );
        var (factory, players) = await SupabaseService.FetchWorldStateAsync( _currentFactoryId, myToken, mySteamId );

        if ( factory == null )
        {
            Log.Info( "[SaveManager] Factory not found, preparing first sync..." );
            CurrentFactory = new FactoryWorldData
            {
                FactoryId = _currentFactoryId,
                HostSteamId = Connection.Local.SteamId.ToString(),
                SaveName = "New Factory"
            };
        }
        else
        {
            CurrentFactory = factory;

            if ( players != null )
            {
                foreach ( var p in players )
                {
                    ActivePlayers[p.PlayerSteamId] = p;
                }
            }

            Log.Info( $"[SaveManager] World loaded: {factory.SaveName} with {players?.Count ?? 0} known players." );
        }

        IsFactoryReady = true;
    }


    // ==========================================
    // 2. SAVING
    // ==========================================

    public async void FlushDirtyData( bool saveFactory, HashSet<string> dirtyPlayers )
    {
        if ( !Networking.IsHost ) return;

        if ( !saveFactory && (dirtyPlayers == null || dirtyPlayers.Count == 0) )
        {
            if ( SaveConfig.DEBUG_SAVE_LOGGING )
                Log.Info( "[SaveManager] No dirty data to flush." );
            return;
        }

        try
        {
            var playersDataList = new List<PlayerSessionData>();

            // Collect dirty players
            if ( dirtyPlayers != null )
            {
                foreach ( var steamId in dirtyPlayers )
                {
                    if ( ActivePlayers.TryGetValue( steamId, out var pData ) )
                    {
                        playersDataList.Add( pData );
                    }
                }
            }

            string mySteamId = Connection.Local.SteamId.ToString();
            string myToken = await GetSboxToken( Connection.Local );

            // Single unified sync call with all changes
            bool syncSuccess = await SupabaseService.SyncAsync(
                _currentFactoryId,
                saveFactory ? CurrentFactory : null,
                playersDataList.Count > 0 ? playersDataList : null,
                myToken,
                mySteamId
            );

            if ( syncSuccess )
            {
                _auditLog?.Save();
                if ( SaveConfig.DEBUG_SAVE_LOGGING )
                    Log.Info( $"[SaveManager] Flushed factory={saveFactory}, players={dirtyPlayers?.Count ?? 0}" );
            }
            else
            {
                Log.Warning( "[SaveManager] Flush sync failed. Data not persisted." );
            }
        }
        catch ( System.Exception ex )
        {
            Log.Error( $"[SaveManager] Flush error: {ex.Message}" );
        }
    }

    // ==========================================
    // 3. UTILITIES
    // ==========================================

    private async Task<string> GetSboxToken( Connection connection )
    {
        if ( connection == null || !connection.IsActive )
        {
            Log.Warning( "[SaveManager] Invalid connection, cannot obtain Sbox token" );
            return null;
        }

        string sboxToken = await Sandbox.Services.Auth.GetToken( "supabase_sync" );

        return sboxToken;
    }

    /// <summary>
    /// Get current SaveAuditLog instance (for external systems to log events).
    /// </summary>
    public SaveAuditLog GetAuditLog() => _auditLog;

    // ==========================================
    // 4. SHUTDOWN LOGIC
    // ==========================================

    protected override void OnDisabled()
    {
        if ( !Networking.IsHost ) return;

        Log.Info( "[SaveManager] Shutting down server, performing final flush..." );
        if ( SaveThrottler.Instance != null )
        {
            SaveThrottler.Instance.ForceFlush();
        }
    }

    protected override void OnDestroy()
    {
        Log.Info( "[SaveManager] Shutdown complete" );
    }
}