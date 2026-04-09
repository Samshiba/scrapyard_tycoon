using Sandbox;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Validates SaveData integrity before writing to disk.
/// Prevents corrupted/exploited saves from persisting.
/// </summary>
public static class SaveValidator
{
    /// <summary>
    /// Validation result wrapper.
    /// </summary>
    public struct ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }

        public static ValidationResult Success() => new() { IsValid = true, ErrorMessage = "" };
        public static ValidationResult Failure( string error ) => new() { IsValid = false, ErrorMessage = error };
    }

    /// <summary>
    /// Validate the entire GameSaveData object before save.
    /// </summary>
    public static ValidationResult ValidateFull( GameSaveData data )
    {
        if ( data == null )
            return ValidationResult.Failure( "SaveData is null" );

        var playerResult = ValidatePlayerSaveData( data.Player );
        if ( !playerResult.IsValid ) return playerResult;

        var inventoryResult = ValidateInventorySaveData( data.Inventory );
        if ( !inventoryResult.IsValid ) return inventoryResult;

        var factoryResult = ValidateFactorySaveData( data.Factory );
        if ( !factoryResult.IsValid ) return factoryResult;

        var prestigeResult = ValidatePrestigeSaveData( data.Prestige );
        if ( !prestigeResult.IsValid ) return prestigeResult;

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validate player scrap and global upgrades.
    /// </summary>
    private static ValidationResult ValidatePlayerSaveData( PlayerSaveData player )
    {
        if ( player == null )
            return ValidationResult.Failure( "PlayerSaveData is null" );

        if ( player.TotalScrap < SaveConfig.MIN_SCRAP || player.TotalScrap > SaveConfig.MAX_SCRAP )
            return ValidationResult.Failure(
                $"TotalScrap out of bounds: {player.TotalScrap} (valid: {SaveConfig.MIN_SCRAP}-{SaveConfig.MAX_SCRAP})"
            );

        if ( player.GlobalUpgrades == null )
            return ValidationResult.Failure( "GlobalUpgrades dictionary is null" );

        // Validate each upgrade level
        foreach ( var upgrade in player.GlobalUpgrades )
        {
            if ( upgrade.Value < 0 || upgrade.Value > SaveConfig.MAX_UPGRADE_LEVEL )
                return ValidationResult.Failure(
                    $"GlobalUpgrade '{upgrade.Key}' level {upgrade.Value} exceeds max {SaveConfig.MAX_UPGRADE_LEVEL}"
                );
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validate inventory items, weapons, and slots.
    /// </summary>
    private static ValidationResult ValidateInventorySaveData( InventorySaveData inventory )
    {
        if ( inventory == null )
            return ValidationResult.Failure( "InventorySaveData is null" );

        if ( inventory.CollectedItems == null )
            return ValidationResult.Failure( "CollectedItems list is null" );

        if ( inventory.CollectedItems.Count > SaveConfig.MAX_BACKPACK_ITEMS )
            return ValidationResult.Failure(
                $"CollectedItems count {inventory.CollectedItems.Count} exceeds max {SaveConfig.MAX_BACKPACK_ITEMS}"
            );

        if ( inventory.UnlockedWeapons == null )
            return ValidationResult.Failure( "UnlockedWeapons list is null" );

        if ( inventory.UnlockedUtilities == null )
            return ValidationResult.Failure( "UnlockedUtilities list is null" );

        if ( inventory.EquippedWeapons == null )
            return ValidationResult.Failure( "EquippedWeapons array is null" );

        if ( inventory.EquippedWeapons.Length != SaveConfig.WEAPON_SLOTS )
            return ValidationResult.Failure(
                $"EquippedWeapons array size {inventory.EquippedWeapons.Length} != {SaveConfig.WEAPON_SLOTS}"
            );

        if ( inventory.ActiveWeaponIndex < 0 || inventory.ActiveWeaponIndex >= SaveConfig.WEAPON_SLOTS )
            return ValidationResult.Failure(
                $"ActiveWeaponIndex {inventory.ActiveWeaponIndex} out of bounds [0-{SaveConfig.WEAPON_SLOTS - 1}]"
            );

        // Get all available weapons for validation (search by Id property, not ResourceLibrary key)
        var allWeapons = ResourceLibrary.GetAll<WeaponDefinition>().ToList();
        var availableIds = string.Join( ", ", allWeapons.Select( w => w.Id ) );

        // Validate all equipped weapon IDs can be resolved
        for ( int i = 0; i < inventory.EquippedWeapons.Length; i++ )
        {
            if ( !string.IsNullOrEmpty( inventory.EquippedWeapons[i] ) )
            {
                // If ResourceLibrary is empty (e.g., at shutdown), skip ResourceLibrary validation
                // The data was just set seconds ago, so we trust it
                if ( allWeapons.Count > 0 )
                {
                    var weaponDef = allWeapons.FirstOrDefault( w => w.Id == inventory.EquippedWeapons[i] );
                    if ( weaponDef == null )
                    {
                        return ValidationResult.Failure(
                            $"EquippedWeapons[{i}] '{inventory.EquippedWeapons[i]}' not found. Available: [{availableIds}]"
                        );
                    }
                }
                else
                {
                    Log.Warning( $"[SaveValidator] ResourceLibrary is empty at validation time - skipping equipped weapon ResourceLibrary check (will trust data)" );
                }
            }
        }

        // Validate unlocked weapon IDs are resolvable
        foreach ( var weaponId in inventory.UnlockedWeapons )
        {
            // Skip ResourceLibrary check if it's empty (shutdown scenario)
            if ( allWeapons.Count > 0 )
            {
                var weaponDef = allWeapons.FirstOrDefault( w => w.Id == weaponId );
                if ( weaponDef == null )
                {
                    return ValidationResult.Failure(
                        $"UnlockedWeapons '{weaponId}' not found. Available: [{availableIds}]"
                    );
                }
            }
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validate factory upgrades and seller queue.
    /// </summary>
    private static ValidationResult ValidateFactorySaveData( FactorySaveData factory )
    {
        if ( factory == null )
            return ValidationResult.Failure( "FactorySaveData is null" );

        if ( factory.MachineUpgrades == null )
            return ValidationResult.Failure( "MachineUpgrades dictionary is null" );

        foreach ( var upgrade in factory.MachineUpgrades )
        {
            if ( upgrade.Value < 0 || upgrade.Value > SaveConfig.MAX_UPGRADE_LEVEL )
                return ValidationResult.Failure(
                    $"MachineUpgrade '{upgrade.Key}' level {upgrade.Value} exceeds max {SaveConfig.MAX_UPGRADE_LEVEL}"
                );
        }

        if ( factory.SellerQueue == null )
            return ValidationResult.Failure( "SellerQueue collection is null" );

        if ( factory.Tier < 1 )
            return ValidationResult.Failure( $"Factory Tier {factory.Tier} must be >= 1" );

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validate prestige system data.
    /// </summary>
    private static ValidationResult ValidatePrestigeSaveData( PrestigeSaveData prestige )
    {
        if ( prestige == null )
            return ValidationResult.Failure( "PrestigeSaveData is null" );

        if ( prestige.PrestigeLevel < 0 || prestige.PrestigeLevel > SaveConfig.MAX_PRESTIGE_LEVEL )
            return ValidationResult.Failure(
                $"PrestigeLevel {prestige.PrestigeLevel} out of bounds [0-{SaveConfig.MAX_PRESTIGE_LEVEL}]"
            );

        if ( prestige.UnlockedPrestigeUpgrades == null )
            return ValidationResult.Failure( "UnlockedPrestigeUpgrades dictionary is null" );

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validate and clamp a scrap value (emergency recovery).
    /// </summary>
    public static float ClampScrap( float value )
    {
        if ( value < SaveConfig.MIN_SCRAP )
            return SaveConfig.MIN_SCRAP;
        if ( value > SaveConfig.MAX_SCRAP )
            return SaveConfig.MAX_SCRAP;
        return value;
    }

    /// <summary>
    /// Validate and clamp an upgrade level (emergency recovery).
    /// </summary>
    public static int ClampUpgradeLevel( int level )
    {
        return level < 0 ? 0 : (level > SaveConfig.MAX_UPGRADE_LEVEL ? SaveConfig.MAX_UPGRADE_LEVEL : level);
    }

    /// <summary>
    /// Check if a weapon ID can be resolved from resources.
    /// </summary>
    public static bool IsWeaponIdValid( string weaponId )
    {
        if ( string.IsNullOrEmpty( weaponId ) )
            return true; // Empty slots are OK

        var weaponDef = ResourceLibrary.Get<WeaponDefinition>( weaponId );
        return weaponDef != null;
    }
}
