using Sandbox;
using System.Collections.Generic;

/// <summary>
/// Gère les achats d'upgrades en liant la Sauvegarde (SaveManager), l'Argent (PlayerStats) et le Catalogue (UpgradeManager).
/// </summary>
public sealed class GlobalUpgradesSystem : Component
{
    public static GlobalUpgradesSystem Instance { get; private set; }

    [Sync] public NetDictionary<string, int> SyncedUpgrades { get; set; } = new();

    private bool _isLoaded = false;

    protected override void OnAwake()
    {
        Instance = this;
    }

    protected override void OnUpdate()
    {
        if ( Networking.IsHost && !_isLoaded && SaveManager.Instance?.IsFactoryReady == true )
        {
            var factoryUpgrades = SaveManager.Instance.CurrentFactory.GlobalUpgrades;

            SyncedUpgrades.Clear();
            foreach ( var kvp in factoryUpgrades )
            {
                SyncedUpgrades[kvp.Key] = kvp.Value;
            }

            _isLoaded = true;
            Log.Info( $"[GlobalUpgradesSystem] Loaded {SyncedUpgrades.Count} upgrades." );
        }
    }


    public int GetUpgradeLevel( string upgradeId )
    {
        return SyncedUpgrades.TryGetValue( upgradeId, out var level ) ? level : 0;
    }

    public bool TryPurchaseUpgrade( string upgradeId )
    {
        // 1. Validate server-side dependencies
        if ( !Networking.IsHost ) return false;

        var factoryUpgrades = SaveManager.Instance?.CurrentFactory?.GlobalUpgrades;
        if ( factoryUpgrades == null || UpgradeManager.Instance == null || FactoryStats.Instance == null ) return false;

        if ( !UpgradeManager.Instance.Database.TryGetValue( upgradeId, out var node ) ) return false;

        int currentLevel = GetUpgradeLevel( upgradeId );
        if ( currentLevel >= node.MaxLevel ) return false;

        // 2. Verify all parent upgrade requirements are met
        if ( !UpgradeManager.Instance.IsNodeUnlocked( upgradeId, SaveManager.Instance.CurrentFactory ) ) return false;

        double cost = node.GetCostForLevel( currentLevel );

        // 3. Attempt payment
        if ( FactoryStats.Instance.SpendScrap( cost ) )
        {
            factoryUpgrades[upgradeId] = currentLevel + 1;

            SyncedUpgrades[upgradeId] = currentLevel + 1;

            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.GlobalUpgradeChanged, $"{node.Name} → Level {currentLevel + 1}" );
            Log.Info( $"[GlobalUpgradesSystem] Upgrade purchased: {node.Name} (Level {currentLevel + 1}) for {cost} scrap" );
            return true;
        }

        Log.Warning( $"[GlobalUpgradesSystem] Insufficient funds for {node.Name}." );
        return false;
    }
}