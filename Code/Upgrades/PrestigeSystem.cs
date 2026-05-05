using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class PrestigeSystem : Component
{
    public static PrestigeSystem Instance { get; private set; }

    protected override void OnAwake()
    {
        Instance = this;
    }

    // --- SERVER ACTION ---

    public void ExecutePrestigeReset()
    {
        if ( !Networking.IsHost ) return;
        var factory = SaveManager.Get( Scene )?.CurrentFactory;
        if ( factory == null ) return;

        int pointsEarned = CalculatePointsFromScrap( factory.WorldState.RunScrapGained );
        if ( pointsEarned <= 0 ) return;

        // Add prestige points via centralized FactoryStats
        FactoryStats.Get( Scene )?.AddPrestige( pointsEarned, trackStats: true );

        // Wipe scrap via centralized FactoryStats
        FactoryStats.Get( Scene )?.WipeScrapForPrestige();

        // Wipe global upgrades (keep prestige only) via centralized FactoryStats
        FactoryStats.Get( Scene )?.WipeGlobalUpgradesForPrestige();

        // Reset world state
        factory.WorldState = new WorldStateData();

        // Rebuild upgrade cache
        GlobalUpgradesSystem.Instance?.RebuildCache();

        Log.Info( $"[PrestigeSystem] Factory Prestiged! +{pointsEarned} points." );
    }

    public void Respec()
    {
        if ( !Networking.IsHost ) return;

        var factory = SaveManager.Get( Scene )?.CurrentFactory;
        if ( factory == null ) return;

        int pointsToRefund = 0;
        var keysToRemove = new List<string>();

        foreach ( var kvp in factory.GlobalUpgrades )
        {
            if ( UpgradeManager.Instance.Database.TryGetValue( kvp.Key, out var def ) && def.Type == UpgradeType.Prestige )
            {
                pointsToRefund += (int)def.BaseCost;
                keysToRemove.Add( kvp.Key );
            }
        }

        foreach ( var key in keysToRemove )
        {
            factory.GlobalUpgrades.Remove( key );
            GlobalUpgradesSystem.Instance?.SyncedUpgrades.Remove( key );
        }

        // Refund prestige points via centralized FactoryStats (without tracking stats)
        FactoryStats.Get( Scene )?.AddPrestige( pointsToRefund, trackStats: false );

        GlobalUpgradesSystem.Instance?.RebuildCache();

        Log.Info( $"[PrestigeSystem] Respec complete. Refunded {pointsToRefund} points." );
    }

    private int CalculatePointsFromScrap( double runScrap )
    {
        // À TOI DE DÉFINIR LA FORMULE ICI (ex: Racine carrée, ou log)
        if ( runScrap < 10000 ) return 0;
        return (int)(System.Math.Sqrt( runScrap ) / 100);
    }
}
