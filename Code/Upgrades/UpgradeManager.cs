using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class UpgradeManager : Component
{
    public static UpgradeManager Instance { get; private set; }

    public Dictionary<string, UpgradeDefinition> Database { get; private set; } = new();

    private bool _isLoaded = false;

    protected override void OnAwake()
    {
        Instance = this;
        LoadDefinitions();
    }

    private void LoadDefinitions()
    {
        var allUpgrades = ResourceLibrary.GetAll<UpgradeDefinition>();
        foreach ( var upgrade in allUpgrades )
        {
            if ( upgrade.Id == null )
            {
                Log.Error( $"[UpgradeManager] Found upgrade with null Id: {upgrade}" );
            }
        }
        Database = allUpgrades.Where( u => u.Id != null ).ToDictionary( u => u.Id );
        Log.Info( $"[UpgradeManager] Loaded {Database.Count} upgrades from assets." );
    }

    public string GetUpgradeName( string upgradeId ) => Database.ContainsKey( upgradeId ) ? $"#upgrade.{upgradeId}.name" : "[UNKNOWN]";
    public string GetUpgradeDescription( string upgradeId ) => Database.ContainsKey( upgradeId ) ? $"#upgrade.{upgradeId}.description" : "[UNKNOWN]";

    public bool IsNodeUnlocked( string upgradeId, FactoryWorldData factorySave )
    {
        if ( !Database.TryGetValue( upgradeId, out var node ) || factorySave == null ) return false;

        foreach ( var req in node.RequiredUpgrades )
        {
            int currentLevel = factorySave.GlobalUpgrades.GetValueOrDefault( req.RequiredUpgrade.Id, 0 );
            if ( currentLevel < req.RequiredLevel ) return false;
        }
        return true;
    }
}