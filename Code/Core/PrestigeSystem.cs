using Sandbox;
using System.Collections.Generic;

public sealed class PrestigeSystem : Component
{
    public static PrestigeSystem Instance { get; private set; }

    [Sync] public int PrestigeLevel { get; private set; } = 0;
    [Sync] public NetDictionary<string, bool> UnlockedUpgrades { get; private set; } = new();

    protected override void OnAwake()
    {
        Instance = this;
    }

    protected override void OnStart()
    {
        if ( !Networking.IsHost ) return;

        if ( SaveManager.Instance?.CurrentFactory != null )
        {
            var factory = SaveManager.Instance.CurrentFactory;
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
        
        GameStats.OnPrestige( PrestigeLevel );

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
        if ( !Networking.IsHost || SaveManager.Instance?.CurrentFactory == null ) return;

        SaveManager.Instance.CurrentFactory.PrestigeLevel = PrestigeLevel;

        var dict = new Dictionary<string, bool>();
        foreach ( var kvp in UnlockedUpgrades )
        {
            dict[kvp.Key] = kvp.Value;
        }
        SaveManager.Instance.CurrentFactory.UnlockedPrestigeUpgrades = dict;

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PrestigeChanged, $"Prestige Level {PrestigeLevel}" );
    }
}
