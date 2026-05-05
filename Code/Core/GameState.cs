using Sandbox;
using System.Linq;
using System.Collections.Generic;
using Sandbox.Network;
using System.Threading.Tasks;

public sealed class GameState : Component, Component.INetworkListener
{
    [Property] public List<BayComponent> AllBays { get; set; } = new();
    [Property] public GameObject PlayerPrefab { get; set; }

    protected override void OnStart()
    {
        if ( AllBays.Count == 0 )
        {
            AllBays = Scene.GetAllComponents<BayComponent>().ToList();
            Log.Info( $"[GameState] Auto-discovered {AllBays.Count} bay(s) in scene" );
        }

        if ( !Networking.IsActive )
        {
            Log.Info( "[GameState] Offline mode detected. Host starting for dev." );
            Networking.CreateLobby( new LobbyConfig()
            {
                MaxPlayers = 4,
                Privacy = LobbyPrivacy.Private
            } );
        }
    }

    // Called when a new player connects to the server
    public void OnActive( Connection channel )
    {
        if ( !Networking.IsHost ) return;

        Log.Info( $"[GameState] Player connected: {channel.DisplayName}" );

        _ = SpawnPlayerForConnectionAsync( channel );
    }

    private async Task SpawnPlayerForConnectionAsync( Connection channel )
    {
        if ( PlayerPrefab == null )
        {
            Log.Error( "[GameState] PlayerPrefab is not assigned in inspector. Player cannot be spawned." );
            return;
        }

        var bayForPlayer = AllBays.FirstOrDefault();
        if ( bayForPlayer == null )
        {
            Log.Error( $"[GameState] No bays configured in scene for player {channel.DisplayName}." );
            return;
        }

        bayForPlayer.AssignOwner( channel );

        if ( bayForPlayer.PlayerStart == null )
        {
            Log.Error( $"[GameState] Bay {bayForPlayer.BayId} has no PlayerStart GameObject assigned. Check inspector configuration." );
            return;
        }

        // Wait for SaveManager to load all player data before spawning visually
        // string steamId = channel.SteamId.ToString();
        string steamId = channel.GetUniqueId();
        var loadTimeout = System.Diagnostics.Stopwatch.StartNew();
        while ( !SaveManager.Get( Scene )?.ActivePlayers.ContainsKey( steamId ) ?? true )
        {
            if ( loadTimeout.ElapsedMilliseconds > 10000 )
            {
                Log.Warning( $"[GameState] Timeout waiting for {channel.DisplayName} data. Spawning anyway." );
                break;
            }
            await Task.Delay( 100 );
        }

        var sceneCam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
        if ( sceneCam != null )
        {
            sceneCam.Enabled = false;
            Log.Info( "[GameState] Scene camera disabled. Using player camera." );
        }

        var spawnPos = bayForPlayer.PlayerStart.WorldPosition;
        var spawnRot = bayForPlayer.PlayerStart.WorldRotation;

        var player = PlayerPrefab.Clone( spawnPos, spawnRot );
        player.NetworkSpawn( channel );

        Log.Info( $"[GameState] Player {channel.DisplayName} spawned at position {spawnPos} in Bay {bayForPlayer.BayId} (all data loaded)" );
    }

    public void OnDisconnected( Connection channel )
    {
        if ( !Networking.IsHost ) return;

        // string steamId = channel.SteamId.ToString();
        string steamId = channel.GetUniqueId();

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerDisconnect, $"Player {channel.DisplayName} disconnected", steamId );

        var playerBay = AllBays.FirstOrDefault( b => b.Owners.Contains( channel ) );
        playerBay?.RemoveOwner( channel );

        if ( SaveManager.Get( Scene ) != null && SaveManager.Get( Scene ).ActivePlayers.ContainsKey( steamId ) )
        {
            SaveManager.Get( Scene ).ActivePlayers.Remove( steamId );
        }

        Log.Info( $"[GameState] Player {channel.DisplayName} disconnected. Save triggered and removed from bay." );
    }
}