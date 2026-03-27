using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class UpgradeManager : Component
{
    public static UpgradeManager Instance { get; private set; }

    [Property] public Dictionary<string, UpgradeNode> Database { get; private set; } = new();

    protected override void OnAwake()
    {
        Instance = this;
        LoadDatabase();
    }

    private void LoadDatabase()
    {
        if ( FileSystem.Mounted.FileExists( "data/upgrades.json" ) )
        {
            var graph = FileSystem.Mounted.ReadJson<UpgradeGraphData>( "data/upgrades.json" );

            Database = graph.Nodes.ToDictionary( node => node.Id );

            Log.Info( $"[UpgradeManager] Upgrade database loaded: {Database.Count} upgrades available" );
        }
        else
        {
            Log.Error( "[UpgradeManager] CRITICAL ERROR: Upgrade database file 'data/upgrades.json' not found. Upgrades system will not function." );
        }
    }

    public string GetUpgradeName( string upgradeId )
    {
        if ( !Database.TryGetValue( upgradeId, out var node ) ) return "[UNKNOWN]";

        string nameKey = $"upgrade.{upgradeId}.name";
        string localizedName = LocalizationManager.GetText( nameKey, null );

        if ( localizedName == null || localizedName.StartsWith( "[MISSING" ) )
        {
            return node.Name;
        }
        return localizedName;
    }

    public string GetUpgradeDescription( string upgradeId )
    {
        if ( !Database.TryGetValue( upgradeId, out var node ) ) return "[UNKNOWN]";

        string descKey = $"upgrade.{upgradeId}.description";
        string localizedDesc = LocalizationManager.GetText( descKey, null );

        if ( localizedDesc == null || localizedDesc.StartsWith( "[MISSING" ) )
        {
            return node.Description;
        }
        return localizedDesc;
    }

    public bool IsNodeUnlocked( string upgradeId, GameSaveData playerSave )
    {
        if ( !Database.TryGetValue( upgradeId, out var node ) ) return false;

        foreach ( var req in node.Requirements )
        {
            int playerLevel = playerSave.Player.GlobalUpgrades.GetValueOrDefault( req.RequiredId, 0 );
            if ( playerLevel < req.RequiredLevel )
            {
                return false;
            }
        }
        return true;
    }
}