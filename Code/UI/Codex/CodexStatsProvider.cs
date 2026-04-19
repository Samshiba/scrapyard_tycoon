using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using static Sandbox.Services.Stats;

/// <summary>
/// Provides clean access to player and factory statistics for the Codex UI.
/// Handles all data retrieval and formatting in one place.
/// </summary>
public static class CodexStatsProvider
{
    // --- LIFETIME STATS ---

    public static double GetLifetimePlaytime()
    {
        PlayerStat playtimeStat = LocalPlayer.Get( "lifetime_playtime" );
        return playtimeStat.Value;
    }

    public static double GetTotalScrapCollected()
    {
        PlayerStat playerStats = LocalPlayer.Get( "total_scrap_collected" );
        return playerStats.Value;
    }

    public static double GetTotalDamageDealt()
    {
        PlayerStat damageStat = LocalPlayer.Get( "total_damage_dealt" );
        return damageStat.Value;
    }

    public static double GetTotalPropsDestroyed()
    {
        PlayerStat propStat = LocalPlayer.Get( "total_props_destroyed" );
        return propStat.Value;
    }

    public static double GetHighestPeakScrap()
    {
        PlayerStat peakScrapStat = LocalPlayer.Get( "peak_scrap" );
        return peakScrapStat.Value;
    }

    public static double GetTotalAttacks()
    {
        PlayerStat attacksStat = LocalPlayer.Get( "total_attacks" );
        return attacksStat.Value;
    }

    public static double GetWeaponsUnlockedCount()
    {
        if ( SaveManager.Instance?.CurrentFactory?.UnlockedWeapons == null )
            return 0;
        return SaveManager.Instance.CurrentFactory.UnlockedWeapons.Count;
    }

    public static double GetUpgradesPurchased()
    {
        if ( SaveManager.Instance?.CurrentFactory?.GlobalUpgrades == null )
            return 0;
        return SaveManager.Instance.CurrentFactory.GlobalUpgrades.Values.Sum();
    }

    public static double GetPrestiges()
    {
        if ( SaveManager.Instance?.CurrentFactory == null )
            return 0;
        return SaveManager.Instance.CurrentFactory.PrestigeLevel;
    }

    // --- FACTORY STATS (SESSION) ---

    public static double GetFactoryTier()
    {
        if ( SaveManager.Instance?.CurrentFactory == null )
            return 1;
        return SaveManager.Instance.CurrentFactory.Tier;
    }

    public static double GetScrapPerSecond()
    {
        // TODO: Implement scrap per second calculation based on seller machine
        return 0;
    }

    public static double GetScrapGainedSession()
    {
        if ( SaveManager.Instance?.CurrentFactory?.Stats == null )
            return 0;
        return SaveManager.Instance.CurrentFactory.Stats.ScrapGainedSession;
    }

    public static float GetSessionPlaytime()
    {
        if ( SaveManager.Instance?.CurrentFactory?.Stats == null )
            return 0f;
        return SaveManager.Instance.CurrentFactory.Stats.TimePlayed;
    }

    public static double GetMaxDamageHit()
    {
        if ( SaveManager.Instance?.CurrentFactory?.Stats == null )
            return 0;
        return SaveManager.Instance.CurrentFactory.Stats.MaxDamageHit;
    }

    public static long GetPropsDestroyedSession()
    {
        if ( SaveManager.Instance?.CurrentFactory?.Stats == null )
            return 0;
        return SaveManager.Instance.CurrentFactory.Stats.PropsDestroyed;
    }

    public static double GetPropsUnlockedCount()
    {
        // Props are unlocked by tier - count how many tiers are accessible
        if ( SaveManager.Instance?.CurrentFactory == null )
            return 0;

        var tier = SaveManager.Instance.CurrentFactory.Tier;
        var allProps = ResourceLibrary.GetAll<PropDefinition>();
        return allProps.Count( p => p.Tier <= tier );
    }

    public static double GetWeaponsUnlocked()
    {
        return GetWeaponsUnlockedCount();
    }

    // --- WEAPON VANITY STATS ---

    public static int GetWeaponKills( string weaponId )
    {
        var playerData = GetCurrentPlayerData();
        if ( playerData?.Stats.WeaponVanity == null )
            return 0;

        if ( playerData.Stats.WeaponVanity.TryGetValue( weaponId, out var stat ) )
            return stat.Count;

        return 0;
    }

    public static double GetWeaponDamageDealt( string weaponId )
    {
        var playerData = GetCurrentPlayerData();
        if ( playerData?.Stats.WeaponVanity == null )
            return 0;

        if ( playerData.Stats.WeaponVanity.TryGetValue( weaponId, out var stat ) )
            return stat.Value;

        return 0;
    }

    // --- PROP VANITY STATS ---

    public static int GetPropDestroyedCount( string propId )
    {
        var playerData = GetCurrentPlayerData();
        if ( playerData?.Stats.PropVanity == null )
            return 0;

        if ( playerData.Stats.PropVanity.TryGetValue( propId, out var stat ) )
            return stat.Count;

        return 0;
    }

    public static double GetPropScrapGained( string propId )
    {
        var playerData = GetCurrentPlayerData();
        if ( playerData?.Stats.PropVanity == null )
            return 0;

        if ( playerData.Stats.PropVanity.TryGetValue( propId, out var stat ) )
            return stat.Value;

        return 0;
    }

    // --- HELPERS ---

    /// <summary>
    /// Gets the current player's session data from the save manager.
    /// Returns null if player data is not available.
    /// </summary>
    private static PlayerSessionData GetCurrentPlayerData()
    {
        if ( SaveManager.Instance == null )
            return null;

        // If only one player, return their data (for offline/testing)
        if ( SaveManager.Instance.ActivePlayers.Count == 1 )
            return SaveManager.Instance.ActivePlayers.Values.First();

        // Try to get from the local player's backpack
        var backpack = PlayerBackpack.Local;
        if ( backpack != null && backpack.Network.Owner != null )
        {
            var steamId = backpack.Network.Owner.SteamId.ToString();
            if ( SaveManager.Instance.ActivePlayers.TryGetValue( steamId, out var playerData ) )
                return playerData;
        }

        // Fallback: return first player if available
        return SaveManager.Instance.ActivePlayers.Values.FirstOrDefault();
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
