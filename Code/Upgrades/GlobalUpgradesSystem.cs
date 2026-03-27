using Sandbox;
using System.Collections.Generic;

/// <summary>
/// Gère les achats d'upgrades en liant la Sauvegarde (SaveManager), l'Argent (PlayerStats) et le Catalogue (UpgradeManager).
/// </summary>
public sealed class GlobalUpgradesSystem : Component
{
    public static GlobalUpgradesSystem Instance { get; private set; }

    protected override void OnAwake()
    {
        Instance = this;
    }

    private Dictionary<string, int> PlayerUpgrades => SaveManager.Instance?.Data?.Player?.GlobalUpgrades;

    public int GetUpgradeLevel( string upgradeId )
    {
        if ( PlayerUpgrades == null ) return 0;

        return PlayerUpgrades.GetValueOrDefault( upgradeId, 0 );
    }

    public bool TryPurchaseUpgrade( string upgradeId )
    {
        // 1. Validate presence of necessary systems and data
        if ( PlayerUpgrades == null || UpgradeManager.Instance == null || PlayerStats.Local == null )
            return false;

        // 2. Verify upgrade exists in the JSON database
        if ( !UpgradeManager.Instance.Database.TryGetValue( upgradeId, out var node ) )
        {
            Log.Warning( $"[GlobalUpgradesSystem] Attempted purchase of unknown upgrade: {upgradeId}" );
            return false;
        }

        int currentLevel = GetUpgradeLevel( upgradeId );

        // 3. Check if upgrade is already at max level
        if ( currentLevel >= node.MaxLevel )
        {
            Log.Info( $"[GlobalUpgradesSystem] WARNING: {node.Name} is already at maximum level {node.MaxLevel}" );
            return false;
        }

        // 4. Verify all parent upgrade requirements are met
        if ( !UpgradeManager.Instance.IsNodeUnlocked( upgradeId, SaveManager.Instance.Data ) )
        {
            Log.Info( $"[GlobalUpgradesSystem] WARNING: Cannot unlock {node.Name}. Parent upgrades required." );
            return false;
        }

        // 5. Calculate cost for the next level
        float cost = node.GetCostForLevel( currentLevel );

        // 6. Attempt payment
        if ( PlayerStats.Local.SpendScrap( cost ) )
        {
            PlayerUpgrades[upgradeId] = currentLevel + 1;
            SaveManager.Instance.Save();

            Log.Info( $"[GlobalUpgradesSystem] Upgrade purchased: {node.Name} (Level {currentLevel + 1}) for {cost} scrap" );
            return true;
        }

        Log.Warning( $"[GlobalUpgradesSystem] WARNING: Insufficient funds for {node.Name}. Cost: {cost} scrap, Available: {PlayerStats.Local.TotalScrap} scrap" );
        return false;
    }
}