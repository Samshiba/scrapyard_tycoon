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
        if (AllBays.Count == 0)
        {
            AllBays = Scene.GetAllComponents<BayComponent>().ToList();
            Log.Info($"GameState: {AllBays.Count} baie(s) trouvée(s) automatiquement.");
        }

        if (!Networking.IsActive)
        {
            Log.Info("GameState: Mode offline détecté, spawn local.");
            SpawnPlayerForConnection(Connection.Local);
        }
    }

    // Called when a new player connects to the server
    public void OnActive(Connection channel)
    {
        if (!Networking.IsHost) return;

        Log.Info($"GameState.OnActive: {channel.DisplayName}");

        // Load player save data
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Load();
            Log.Info($"💾 Save chargée pour {channel.DisplayName}");
        }

        SpawnPlayerForConnection(channel);
    }

    private void SpawnPlayerForConnection(Connection channel)
    {
        if (PlayerPrefab == null)
        {
            Log.Error("GameState: PlayerPrefab non assigné !");
            return;
        }

        var freeBay = AllBays.FirstOrDefault(b => !b.IsOccupied);
        if (freeBay == null)
        {
            Log.Warning($"Plus de baies libres pour {channel.DisplayName}");
            return;
        }

        freeBay.AssignOwner(channel);

        if (freeBay.PlayerStart == null)
        {
            Log.Error($"Baie {freeBay.BayId}: PlayerStart non assigné !");
            return;
        }

        var sceneCam = Scene.GetAllComponents<CameraComponent>().FirstOrDefault();
        if (sceneCam != null)
        {
            sceneCam.Enabled = false;
            Log.Info("GameState: Caméra de scène désactivée.");
        }

        var spawnPos = freeBay.PlayerStart.WorldPosition;
        var spawnRot = freeBay.PlayerStart.WorldRotation;

        var player = PlayerPrefab.Clone(spawnPos, spawnRot);
        player.NetworkSpawn(channel);

        Log.Info($"Player spawné pour {channel.DisplayName} à {spawnPos}");
    }

    public void OnDisconnected(Connection channel)
    {
        // Save player data before disconnect
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Save();
            Log.Info($"💾 Save envoyée avant déconnexion de {channel.DisplayName}");
        }

        var playerBay = AllBays.FirstOrDefault(b => b.Owner == channel);
        playerBay?.ClearOwner();
    }
}