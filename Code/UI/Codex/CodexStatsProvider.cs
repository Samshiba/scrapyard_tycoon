using Sandbox;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using static Sandbox.Services.Stats;

/// <summary>
/// Provides clean access to player and factory statistics for the Codex UI.
/// All player stats come from Sbox (lifetime accumulated).
/// Factory stats come from the synchronized FactoryStats component (Network).
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

    private static Scene Scene;

    public static void SetScene( Scene scene )
    {
        Scene = scene;
    }

    // ==========================================
    // 1. PLAYER LIFETIME STATS (FROM SBOX)
    // ==========================================

    // Economy
    public static double GetTotalScrapCollected() => GetSboxStat( "lifetime_scrap" );
    public static double GetHighestPeakScrap() => GetSboxStat( "largest_scrap_gain", StatType.Max );
    public static double GetTotalMoneySpent() => GetSboxStat( "total_money_spent" );
    public static double GetLargestSinglePurchase() => GetSboxStat( "largest_single_purchase", StatType.Max );
    public static double GetTotalGibsDropped() => GetSboxStat( "total_gibs_dropped" );

    // Combat
    public static double GetTotalDamageDealt() => GetSboxStat( "total_damage" );
    public static double GetHighestDamageHit() => GetSboxStat( "highest_damage_hit", StatType.Max );
    public static double GetTotalAttacks() => GetSboxStat( "total_attacks" );
    public static double GetTotalCriticalHits() => GetSboxStat( "total_critical_hits" );
    public static double GetCriticalDamage() => GetSboxStat( "critical_damage" );
    public static double GetTargetsDestroyed() => GetSboxStat( "targets_destroyed" );

    // Buying
    public static double GetTotalWeaponsBought() => GetSboxStat( "total_weapons_bought" );
    public static double GetTotalUpgradesBought() => GetSboxStat( "total_upgrades_bought" );

    // Time & Meta
    public static double GetLifetimePlaytime() => GetSboxStat( "total_playtime_seconds" );
    public static double GetTimesPrestiged() => GetSboxStat( "times_prestiged" );
    public static double GetPrestigePoints() => GetSboxStat( "prestige_points" );


    // ==========================================
    // 2. FACTORY STATS (NETWORK SYNC)
    // ==========================================

    // SPS
    public static double GetScrapPerSecond() => FactoryStats.Get( Scene )?.CurrentSPS ?? 0;

    // Economy
    public static double GetFactoryScrapGained() => FactoryStats.Get( Scene )?.ScrapGained ?? 0;
    public static double GetFactoryLargestScrapGain() => FactoryStats.Get( Scene )?.LargestScrapGain ?? 0;
    public static double GetFactoryMoneySpent() => FactoryStats.Get( Scene )?.TotalMoneySpent ?? 0;
    public static double GetFactoryLargestPurchase() => FactoryStats.Get( Scene )?.LargestSinglePurchase ?? 0;
    public static int GetFactoryGibsDropped() => FactoryStats.Get( Scene )?.GibsDropped ?? 0;

    // Combat
    public static double GetFactoryTotalDamage() => FactoryStats.Get( Scene )?.TotalDamage ?? 0;
    public static double GetFactoryHighestDamageHit() => FactoryStats.Get( Scene )?.HighestDamageHit ?? 0;
    public static int GetFactoryTotalAttacks() => FactoryStats.Get( Scene )?.TotalAttacks ?? 0;
    public static int GetFactoryTotalCriticalHits() => FactoryStats.Get( Scene )?.TotalCriticalHits ?? 0;
    public static double GetFactoryCriticalDamage() => FactoryStats.Get( Scene )?.CriticalDamage ?? 0;
    public static long GetFactoryTargetsDestroyed() => FactoryStats.Get( Scene )?.TargetsDestroyed ?? 0;

    // Buying
    public static int GetFactoryWeaponsBought() => FactoryStats.Get( Scene )?.WeaponsBought ?? 0;
    public static int GetFactoryUpgradesBought() => FactoryStats.Get( Scene )?.UpgradesBought ?? 0;

    // Time & Meta
    public static float GetFactoryPlaytime() => FactoryStats.Get( Scene )?.TimePlayed ?? 0f;
    public static int GetFactoryTimesPrestiged() => FactoryStats.Get( Scene )?.TimesPrestiged ?? 0;

    public static int GetFactoryTier()
    {
        return FactoryStats.Get( Scene )?.Tier ?? 1;
    }


    // ==========================================
    // 3. WEAPON VANITY STATS (FROM SBOX ONLY)
    // ==========================================

    public static int GetWeaponKills( string weaponId ) => (int)GetSboxStat( $"targets_destroyed_with_{weaponId}" );
    public static double GetWeaponDamageDealt( string weaponId ) => GetSboxStat( $"damage_with_{weaponId}" );
    public static double GetWeaponHighestDamageHit( string weaponId ) => GetSboxStat( $"highest_damage_hit_with_{weaponId}", StatType.Max );
    public static int GetWeaponCriticalHits( string weaponId ) => (int)GetSboxStat( $"crits_with_{weaponId}" );
    public static int GetWeaponTargetsDestroyed( string weaponId ) => (int)GetSboxStat( $"targets_destroyed_with_{weaponId}" );


    // ==========================================
    // 4. PROP VANITY STATS (FROM SBOX ONLY)
    // ==========================================

    public static int GetPropDestroyedCount( string propId ) => (int)GetSboxStat( $"targets_destroyed_{propId}" );
    public static double GetPropScrapDropped( string propId ) => GetSboxStat( $"scrap_dropped_{propId}" );
    public static int GetPropGibsDropped( string propId ) => (int)GetSboxStat( $"gibs_dropped_{propId}" );


    // ==========================================
    // 5. UI HELPERS & CALCULATORS
    // ==========================================

    /// <summary>
    /// Formats playtime in hours and minutes.
    /// </summary>
    public static string FormatPlaytime( double seconds )
    {
        var hours = (int)(seconds / 3600);
        var minutes = (int)((seconds % 3600) / 60);
        return $"{hours}h {minutes}m";
    }

    public static bool IsWeaponUnlocked( string weaponId )
    {
        if ( weaponId == "bat" ) return true;

        return GetWeaponDamageDealt( weaponId ) > 0;
    }

    public static bool IsPropSeen( string propId ) => GetPropDestroyedCount( propId ) > 0;

    public static float GetPropHP( PropDefinition data ) => PropStatsCalculator.GetHealth( data );
    public static float GetPropScrap( PropDefinition data ) => PropStatsCalculator.GetValue( data );

    public static int GetPropGibCount( PropDefinition data )
    {
        try { return PropStatsCalculator.GetGibCount( data ); }
        catch { return 0; }
    }

    public static float GetPropValuePerGib( PropDefinition data )
    {
        try { return PropStatsCalculator.GetValuePerGib( data ); }
        catch { return 0; }
    }

    public static float GetPropTotalValue( PropDefinition data )
    {
        try { return PropStatsCalculator.GetTotalValueWithSubProps( data ); }
        catch { return 0; }
    }

    public static float GetSellerMultiplier()
    {
        try { return PropStatsCalculator.GetSellerValueMultiplier( Scene ); }
        catch { return 1f; }
    }

    public static float GetPropFinalValue( PropDefinition data )
    {
        try
        {
            float totalValue = PropStatsCalculator.GetTotalValueWithSubProps( data );
            return PropStatsCalculator.ApplyMoneyMultiplier( totalValue, GetSellerMultiplier() );
        }
        catch { return 0; }
    }
}
