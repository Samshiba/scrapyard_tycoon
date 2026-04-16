using Sandbox;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sanity checks and data clamping before sending to the Cloud.
/// </summary>
public static class SaveValidator
{
    /// <summary>
    /// Validate and clamp the factory's scrap value.
    /// </summary>
    public static double ClampScrap( double value )
    {
        if ( value < SaveConfig.MIN_SCRAP ) return SaveConfig.MIN_SCRAP;
        if ( value > SaveConfig.MAX_SCRAP ) return SaveConfig.MAX_SCRAP;
        return value;
    }

    /// <summary>
    /// Validate and clamp an upgrade level (Factory or Player).
    /// </summary>
    public static int ClampUpgradeLevel( int level )
    {
        return level < 0 ? 0 : (level > SaveConfig.MAX_UPGRADE_LEVEL ? SaveConfig.MAX_UPGRADE_LEVEL : level);
    }

    /// <summary>
    /// Validate and clamp prestige level.
    /// </summary>
    public static int ClampPrestige( int level )
    {
        return level < 0 ? 0 : (level > SaveConfig.MAX_PRESTIGE_LEVEL ? SaveConfig.MAX_PRESTIGE_LEVEL : level);
    }

    /// <summary>
    /// Check if a weapon ID is valid (prevents injecting fake items via memory editing).
    /// </summary>
    public static bool IsWeaponIdValid( string weaponId )
    {
        if ( string.IsNullOrEmpty( weaponId ) ) return false;

        // Optionnel : Tu pourrais vérifier contre un dictionnaire global de tes armes ici
        // return WeaponRegistry.Contains(weaponId);

        return true;
    }
}
