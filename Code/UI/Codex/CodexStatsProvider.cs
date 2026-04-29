using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using static Sandbox.Services.Stats;

/// <summary>
/// Provides clean access to player and factory statistics for the Codex UI.
/// All player stats come from Sbox (lifetime accumulated).
/// Factory stats come from local session data.
/// </summary>
public static class CodexStatsProvider
{
    // --- STAT TYPE ENUM ---
    private enum StatType { Sum, Value, Max, Min, Avg }

    // --- GENERIC SBOX STAT GETTER ---
    /// <summary>
    /// Generic getter for any Sbox player stat.
    /// </summary>
    private static double GetSboxStat( string statName, StatType type = StatType.Sum )
    {
        try
        {
            var stat = LocalPlayer.Get( statName );
            return type switch
            {
                StatType.Sum => stat.Sum,
                StatType.Value => stat.Value,
                StatType.Max => stat.Max,
                StatType.Min => stat.Min,
                StatType.Avg => stat.Avg,
                _ => stat.Sum
            };
        }
        catch
        {
            return 0;
        }
    }

    // --- FACTORY STAT HELPER ---
    /// <summary>
    /// Generic getter for factory session stats.
    /// </summary>
    private static T GetFactoryStat<T>( Func<FactoryStatsData, T> getter, T defaultValue = default )
    {
        if ( SaveManager.Instance?.CurrentFactory?.Stats == null )
            return defaultValue;
        return getter( SaveManager.Instance.CurrentFactory.Stats );
    }

    // --- PLAYER LIFETIME STATS (FROM SBOX) ---

    public static double GetLifetimePlaytime() => GetSboxStat( "total_playtime_seconds" );
    public static double GetTotalScrapCollected() => GetSboxStat( "lifetime_scrap" );
    public static double GetTotalDamageDealt() => GetSboxStat( "total_damage" );
    public static double GetHighestDamageHit() => GetSboxStat( "highest_damage_hit", StatType.Max );
    public static double GetTargetsDestroyed() => GetSboxStat( "targets_destroyed" );
    public static double GetTotalAttacks() => GetSboxStat( "total_attacks" );
    public static double GetTotalCriticalHits() => GetSboxStat( "total_critical_hits" );
    public static double GetCriticalDamage() => GetSboxStat( "critical_damage" );
    public static double GetHighestPeakScrap() => GetSboxStat( "largest_scrap_gain", StatType.Max );
    public static double GetTimesPrestiged() => GetSboxStat( "times_prestiged" );
    public static double GetPrestigePoints() => GetSboxStat( "prestige_points" );
    public static double GetFactoryResets() => GetSboxStat( "factory_resets" );
    public static double GetWeaponsUnlockedCount() => GetSboxStat( "weapons_unlocked" );
    public static double GetUpgradesUnlockedCount() => GetSboxStat( "upgrades_unlocked" );
    public static double GetTotalMoneySpent() => GetSboxStat( "total_money_spent" );
    public static double GetLargestSinglePurchase() => GetSboxStat( "largest_single_purchase", StatType.Max );
    public static double GetTotalGibsDropped() => GetSboxStat( "total_gibs_dropped" );

    // --- FACTORY STATS (SESSION ONLY) ---

    public static int GetFactoryTier()
    {
        if ( SaveManager.Instance?.CurrentFactory == null )
            return 1;
        return SaveManager.Instance.CurrentFactory.Tier;
    }

    public static double GetScrapGainedSession() => GetFactoryStat( s => s.ScrapGainedSession, 0 );
    public static float GetSessionPlaytime() => GetFactoryStat( s => s.TimePlayed, 0f );
    public static double GetMaxDamageHitSession() => GetFactoryStat( s => s.MaxDamageHit, 0 );
    public static long GetPropsDestroyedSession() => GetFactoryStat( s => s.PropsDestroyed, 0L );
    public static int GetGibsDroppedSession() => GetFactoryStat( s => s.GibsDroppedSession, 0 );

    public static double GetScrapPerSecond()
    {
        return FactoryStats.Instance?.CurrentSPS ?? 0;
    }

    public static double GetPropsUnlockedCount()
    {
        if ( SaveManager.Instance?.CurrentFactory == null )
            return 0;
        var tier = SaveManager.Instance.CurrentFactory.Tier;
        var allProps = ResourceLibrary.GetAll<PropDefinition>();
        return allProps.Count( p => p.Tier <= tier );
    }

    // --- WEAPON VANITY STATS (FROM SBOX ONLY) ---

    public static int GetWeaponKills( string weaponId )
    {
        return (int)GetSboxStat( $"targets_destroyed_with_{weaponId}" );
    }

    public static double GetWeaponDamageDealt( string weaponId )
    {
        return GetSboxStat( $"damage_with_{weaponId}" );
    }

    public static double GetWeaponHighestDamageHit( string weaponId )
    {
        return GetSboxStat( $"highest_damage_hit_with_{weaponId}", StatType.Value );
    }

    public static int GetWeaponCriticalHits( string weaponId )
    {
        return (int)GetSboxStat( $"crits_with_{weaponId}" );
    }

    public static int GetWeaponTargetsDestroyed( string weaponId )
    {
        return (int)GetSboxStat( $"targets_destroyed_with_{weaponId}" );
    }

    // --- PROP VANITY STATS (FROM SBOX ONLY) ---

    public static int GetPropDestroyedCount( string propId )
    {
        return (int)GetSboxStat( $"targets_destroyed_{propId}" );
    }

    public static double GetPropScrapDropped( string propId )
    {
        return GetSboxStat( $"scrap_dropped_{propId}" );
    }

    public static int GetPropGibsDropped( string propId )
    {
        return (int)GetSboxStat( $"gibs_dropped_{propId}" );
    }

    /// <summary>
    /// Formats playtime in hours and minutes.
    /// </summary>
    public static string FormatPlaytime( double seconds )
    {
        var hours = (int)(seconds / 3600);
        var minutes = (int)((seconds % 3600) / 60);
        return $"{hours}h {minutes}m";
    }
}
