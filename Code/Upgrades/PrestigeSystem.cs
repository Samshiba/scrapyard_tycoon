using Sandbox;
using System;
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

        // Add prestige points via centralized FactoryDataSyncer
        FactoryDataSyncer.Get( Scene )?.AddPrestige( pointsEarned, trackStats: true );

        // Wipe scrap via centralized FactoryDataSyncer
        FactoryDataSyncer.Get( Scene )?.WipeScrapForPrestige();

        // Wipe global upgrades (keep prestige only) via centralized FactoryDataSyncer
        FactoryDataSyncer.Get( Scene )?.WipeGlobalUpgradesForPrestige();

        // Wipe Weapons/Utilities via centralized FactoryDataSyncer
        FactoryDataSyncer.Get( Scene )?.WipeWeaponsAndUtilitiesForPrestige();

        // Wipe player inventories (weapons, gibs, items) for all players
        FactoryDataSyncer.Get( Scene )?.WipePlayerInventoriesForPrestige();

        // Force all connected players to reload their inventories
        foreach ( var backpack in Scene.GetAllComponents<PlayerBackpack>() )
        {
            backpack.ResetInventoryForPrestige();
        }

        foreach ( var inventory in Scene.GetAllComponents<PlayerInventory>() )
        {
            inventory.ResetEquippedWeaponsForPrestige();
        }

        // Reset world state
        FactoryDataSyncer.Get( Scene )?.ResetWorldStateForPrestige();

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

        pointsToRefund -= Math.Max( 1, (int)Math.Ceiling( pointsToRefund * 0.1 ) );

        foreach ( var key in keysToRemove )
        {
            factory.GlobalUpgrades.Remove( key );
            GlobalUpgradesSystem.Instance?.SyncedUpgrades.Remove( key );
        }

        // Refund prestige points via centralized FactoryDataSyncer (without tracking stats)
        FactoryDataSyncer.Get( Scene )?.AddPrestige( pointsToRefund, trackStats: false );

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
