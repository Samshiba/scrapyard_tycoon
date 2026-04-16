using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class UpgradeManager : Component
{
    public static UpgradeManager Instance { get; private set; }

    [Property] public Dictionary<string, UpgradeNode> Database { get; private set; } = new();

    private bool _isLoaded = false;

    protected override void OnAwake()
    {
        Instance = this;
    }

    protected override void OnStart()
    {
        LoadDatabase();
    }

    private void LoadDatabase()
    {
        if ( _isLoaded ) return;

        if ( FileSystem.Mounted.FileExists( "data/upgrades.json" ) )
        {
            var graph = FileSystem.Mounted.ReadJson<UpgradeGraphData>( "data/upgrades.json" );

            Database = graph.Nodes.ToDictionary( node => node.Id );

            Log.Info( $"[UpgradeManager] Upgrade database loaded: {Database.Count} upgrades available" );
            _isLoaded = true;
        }
        else
        {
            Log.Error( "[UpgradeManager] Upgrade database file 'data/upgrades.json' not found. Upgrades system will not function." );
        }
    }

    public string GetUpgradeName( string upgradeId ) => Database.ContainsKey( upgradeId ) ? $"#upgrade.{upgradeId}.name" : "[UNKNOWN]";
    public string GetUpgradeDescription( string upgradeId ) => Database.ContainsKey( upgradeId ) ? $"#upgrade.{upgradeId}.description" : "[UNKNOWN]";

    public bool IsNodeUnlocked( string upgradeId, FactoryWorldData factorySave )
    {
        if ( !Database.TryGetValue( upgradeId, out var node ) || factorySave == null ) return false;

        foreach ( var req in node.Requirements )
        {
            int currentLevel = factorySave.GlobalUpgrades.GetValueOrDefault( req.RequiredId, 0 );
            if ( currentLevel < req.RequiredLevel ) return false;
        }
        return true;
    }
}