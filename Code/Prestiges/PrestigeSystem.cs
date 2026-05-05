using Sandbox;
using System.Collections.Generic;

public sealed class PrestigeSystem : Component
{
    [Sync] public int PrestigeLevel { get; private set; } = 0;
    [Sync] public NetDictionary<string, bool> UnlockedUpgrades { get; private set; } = new();

    protected override void OnStart()
    {
        if ( !Networking.IsHost ) return;

        if ( SaveManager.Get( Scene )?.CurrentFactory != null )
        {
            var factory = SaveManager.Get( Scene ).CurrentFactory;
            PrestigeLevel = factory.PrestigeLevel;

            if ( factory.UnlockedPrestigeUpgrades != null )
            {
                foreach ( var kvp in factory.UnlockedPrestigeUpgrades )
                {
                    UnlockedUpgrades[kvp.Key] = kvp.Value;
                }
            }
            Log.Info( $"[PrestigeSystem] Prestige loaded: level {PrestigeLevel}" );
        }
    }

    public bool IsUpgradeUnlocked( string upgradeName )
    {
        return UnlockedUpgrades.ContainsKey( upgradeName ) && UnlockedUpgrades[upgradeName];
    }

    // --- SERVER ACTION ---

    public void IncreasePrestige()
    {
        if ( !Networking.IsHost ) return;

        PrestigeLevel++;
        SaveChanges();
        Log.Info( $"[PrestigeSystem] L'Usine est passée au Prestige {PrestigeLevel} !" );

        GameStats.OnPrestige( Scene, PrestigeLevel );

        // TODO : RESET FACTORY
    }

    public void UnlockUpgrade( string upgradeName )
    {
        if ( !Networking.IsHost ) return;

        if ( !UnlockedUpgrades.ContainsKey( upgradeName ) || !UnlockedUpgrades[upgradeName] )
        {
            UnlockedUpgrades[upgradeName] = true;
            SaveChanges();
            Log.Info( $"[PrestigeSystem] Prestige upgrade unlocked: {upgradeName}" );
        }
    }

    private void SaveChanges()
    {
        if ( !Networking.IsHost || SaveManager.Get( Scene )?.CurrentFactory == null ) return;

        SaveManager.Get( Scene ).CurrentFactory.PrestigeLevel = PrestigeLevel;

        var dict = new Dictionary<string, bool>();
        foreach ( var kvp in UnlockedUpgrades )
        {
            dict[kvp.Key] = kvp.Value;
        }
        SaveManager.Get( Scene ).CurrentFactory.UnlockedPrestigeUpgrades = dict;

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PrestigeChanged, $"Prestige Level {PrestigeLevel}" );
    }
}
