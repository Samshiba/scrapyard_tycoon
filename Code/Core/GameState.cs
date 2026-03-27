using Sandbox;
using System.Linq;
using System.Collections.Generic;

public sealed class GameState : Component, Component.INetworkListener
{
    public static GameState Instance { get; private set; }

    // Gardé pour assignation manuelle optionnelle, mais auto-rempli au démarrage
    [Property] public List<BayComponent> AllBays { get; set; } = new();
    [Property] public GameObject PlayerPrefab { get; set; }

    protected override void OnAwake()
    {
        Instance = this;
    }

    protected override void OnStart()
    {
        if ( AllBays.Count == 0 )
        {
            AllBays = Scene.GetAllComponents<BayComponent>().ToList();
            Log.Info( $"[GameState] Auto-discovered {AllBays.Count} bay(s) in scene" );
        }

        if ( !Networking.IsActive )
        {
            Log.Info( "[GameState] Offline mode detected. Spawning local player." );
            SpawnPlayerForConnection( Connection.Local );
        }
    }

    // Called when a new player connects to the server
    public void OnActive( Connection channel )
    {
        if ( !Networking.IsHost ) return;

        Log.Info( $"[GameState] Player connected: {channel.DisplayName}" );

        // Load player save data
        if ( SaveManager.Instance != null )
        {
            SaveManager.Instance.Load();
            Log.Info( $"[GameState] Save data loaded for player {channel.DisplayName}" );
        }

        SpawnPlayerForConnection( channel );
    }

    private void SpawnPlayerForConnection( Connection channel )
    {
        if ( PlayerPrefab == null )
        {
            Log.Error( "[GameState] ERROR: PlayerPrefab is not assigned in inspector. Player cannot be spawned." );
            return;
        }

        var freeBay = AllBays.FirstOrDefault( b => !b.IsOccupied );
        if ( freeBay == null )
        {
            Log.Warning( $"[GameState] WARNING: No available bays for player {channel.DisplayName}. Server at capacity." );
            return;
        }

        freeBay.AssignOwner( channel );

        if ( freeBay.PlayerStart == null )
        {
            Log.Error( $"[GameState] ERROR: Bay {freeBay.BayId} has no PlayerStart GameObject assigned. Check inspector configuration." );
            return;
        }

        var sceneCam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
        if ( sceneCam != null )
        {
            sceneCam.Enabled = false;
            Log.Info( "[GameState] Scene camera disabled. Using player camera." );
        }

        var spawnPos = freeBay.PlayerStart.WorldPosition;
        var spawnRot = freeBay.PlayerStart.WorldRotation;

        var player = PlayerPrefab.Clone( spawnPos, spawnRot );
        player.NetworkSpawn( channel );

        Log.Info( $"[GameState] Player {channel.DisplayName} spawned at position {spawnPos}" );
    }

    public void OnDisconnected( Connection channel )
    {
        // Save player data before disconnect
        if ( SaveManager.Instance != null )
        {
            SaveManager.Instance.Save();
            Log.Info( $"[GameState] Player data saved for {channel.DisplayName} before disconnect" );
        }

        var playerBay = AllBays.FirstOrDefault( b => b.Owner == channel );
        playerBay?.ClearOwner();
    }
}