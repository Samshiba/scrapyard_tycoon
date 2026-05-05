using Sandbox;
using static Sandbox.Services.Stats;

public static class GameStats
{
    // ANTI CHEAT CHECK
    public static bool CanSendToSbox( Scene scene )
    {
        return SaveManager.Get( scene ) != null && !SaveManager.Get( scene ).CurrentFactory.IsSandbox;
    }

    // ==========================================
    // 1. ECONOMY STATS
    // ==========================================

    public static void OnScrapGained( Scene scene, string steamId, double totalAmount, double largestItemValue )
    {
        var save = SaveManager.Get( scene );
        if ( save == null || !save.IsFactoryReady ) return;

        // --- 1. SAUVEGARDE & 2. RÉSEAU (FACTORY) ---
        var fStats = save.CurrentFactory.Stats;
        fStats.ScrapGained += totalAmount;
        if ( largestItemValue > fStats.LargestScrapGain )
            fStats.LargestScrapGain = largestItemValue;

        if ( FactoryStats.Get( scene ) != null )
        {
            FactoryStats.Get( scene ).ScrapGained = fStats.ScrapGained;
            FactoryStats.Get( scene ).LargestScrapGain = fStats.LargestScrapGain;
        }
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.WorldStatUpdate, "Stats: Scrap" );

        // --- 3. API S&BOX (JOUEUR) ---
        if ( CanSendToSbox( scene ) )
        {
            Increment( "lifetime_scrap", totalAmount );
            if ( largestItemValue > 0 )
                SetValue( "largest_scrap_gain", largestItemValue );
        }

        // --- 4. ARCHIVE LOCALE (JOUEUR) ---
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.LifetimeScrapGained += totalAmount;
            if ( largestItemValue > pData.Stats.LargestScrapGain )
                pData.Stats.LargestScrapGain = largestItemValue;

            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Scrap", steamId );
        }
    }

    public static void OnMoneySpent( Scene scene, string steamId, double amount )
    {
        var save = SaveManager.Get( scene );
        if ( save == null || !save.IsFactoryReady ) return;

        // --- 1. SAUVEGARDE & 2. RÉSEAU (FACTORY) ---
        var fStats = save.CurrentFactory.Stats;
        fStats.TotalMoneySpent += amount;
        if ( amount > fStats.LargestSinglePurchase )
            fStats.LargestSinglePurchase = amount;

        if ( FactoryStats.Get( scene ) != null )
        {
            FactoryStats.Get( scene ).TotalMoneySpent = fStats.TotalMoneySpent;
            FactoryStats.Get( scene ).LargestSinglePurchase = fStats.LargestSinglePurchase;
        }

        // --- 3. API S&BOX (JOUEUR) ---
        if ( CanSendToSbox( scene ) )
        {
            Increment( "total_money_spent", amount );
            SetValue( "largest_single_purchase", amount );
        }

        // --- 4. ARCHIVE LOCALE (JOUEUR) ---
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalMoneySpent += amount;
            if ( amount > pData.Stats.LargestSinglePurchase )
                pData.Stats.LargestSinglePurchase = amount;

            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Money", steamId );
        }
    }

    // ==========================================
    // 2. COMBAT & DESTRUCTION
    // ==========================================

    public static void OnPropDestroyed( Scene scene, string steamId, string propId, string weaponId, double scrapYield, int gibsDropped )
    {
        var save = SaveManager.Get( scene );
        if ( save == null || !save.IsFactoryReady ) return;

        // --- 1. SAUVEGARDE & 2. RÉSEAU (FACTORY) ---
        var fStats = save.CurrentFactory.Stats;
        fStats.TargetsDestroyed++;
        if ( gibsDropped > 0 )
            fStats.GibsDropped += gibsDropped;

        if ( FactoryStats.Get( scene ) != null )
        {
            FactoryStats.Get( scene ).TargetsDestroyed = fStats.TargetsDestroyed;
            FactoryStats.Get( scene ).GibsDropped = fStats.GibsDropped;
        }

        // --- 3. API S&BOX (JOUEUR) ---
        if ( CanSendToSbox( scene ) )
        {
            Increment( "targets_destroyed", 1 );
            Increment( $"targets_destroyed_{propId}", 1 );
            Increment( $"targets_destroyed_with_{weaponId}", 1 );
            Increment( $"scrap_dropped_{propId}", scrapYield );
            if ( gibsDropped > 0 )
            {
                Increment( "total_gibs_dropped", gibsDropped );
                Increment( $"gibs_dropped_{propId}", gibsDropped );
            }
        }

        // --- 4. ARCHIVE LOCALE (JOUEUR) ---
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalTargetsDestroyed++;
            if ( gibsDropped > 0 )
                pData.Stats.TotalGibsDropped += gibsDropped;

            // Vanity Prop
            if ( !pData.Stats.PropVanity.ContainsKey( propId ) )
                pData.Stats.PropVanity[propId] = new PropVanityStat();

            pData.Stats.PropVanity[propId].TimesDestroyed++;
            pData.Stats.PropVanity[propId].TotalScrapDropped += scrapYield;
            pData.Stats.PropVanity[propId].NumberOfGibsDropped += gibsDropped;

            // Vanity Weapon
            if ( !string.IsNullOrEmpty( weaponId ) )
            {
                if ( !pData.Stats.WeaponVanity.ContainsKey( weaponId ) )
                    pData.Stats.WeaponVanity[weaponId] = new WeaponVanityStat();

                pData.Stats.WeaponVanity[weaponId].TargetsDestroyed++;
            }
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Prop", steamId );
        }
    }

    public static void OnDamageDealt( Scene scene, string steamId, string weaponId, double damage, bool isCrit = false )
    {
        var save = SaveManager.Get( scene );
        if ( save == null || !save.IsFactoryReady ) return;

        // --- 1. SAUVEGARDE & 2. RÉSEAU (FACTORY) ---
        var fStats = save.CurrentFactory.Stats;
        fStats.TotalDamage += damage;
        fStats.TotalAttacks++;

        if ( damage > fStats.HighestDamageHit )
            fStats.HighestDamageHit = damage;

        if ( isCrit )
        {
            fStats.TotalCriticalHits++;
            fStats.CriticalDamage += damage;
        }

        if ( FactoryStats.Get( scene ) != null )
        {
            FactoryStats.Get( scene ).TotalDamage = fStats.TotalDamage;
            FactoryStats.Get( scene ).TotalAttacks = fStats.TotalAttacks;
            FactoryStats.Get( scene ).HighestDamageHit = fStats.HighestDamageHit;
            FactoryStats.Get( scene ).TotalCriticalHits = fStats.TotalCriticalHits;
            FactoryStats.Get( scene ).CriticalDamage = fStats.CriticalDamage;
        }

        // --- 3. API S&BOX (JOUEUR) ---
        if ( CanSendToSbox( scene ) )
        {
            Increment( "total_damage", damage );
            SetValue( "highest_damage_hit", damage );
            Increment( "total_attacks", 1 );

            if ( isCrit )
            {
                Increment( "total_critical_hits", 1 );
                Increment( "critical_damage", damage );
            }

            if ( !string.IsNullOrEmpty( weaponId ) )
            {
                Increment( $"damage_with_{weaponId}", damage );
                Increment( $"attacks_with_{weaponId}", 1 );

                SetValue( $"highest_damage_hit_with_{weaponId}", damage );
                if ( isCrit )
                    Increment( $"crits_with_{weaponId}", 1 );
            }
        }

        // --- 4. ARCHIVE LOCALE (JOUEUR) ---
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalDamage += damage;
            pData.Stats.TotalAttacks++;

            if ( damage > pData.Stats.HighestDamageHit )
                pData.Stats.HighestDamageHit = damage;

            if ( isCrit )
            {
                pData.Stats.TotalCriticalHits++;
                pData.Stats.CriticalDamage += damage;
            }

            if ( !string.IsNullOrEmpty( weaponId ) )
            {
                if ( !pData.Stats.WeaponVanity.ContainsKey( weaponId ) )
                    pData.Stats.WeaponVanity[weaponId] = new WeaponVanityStat();

                pData.Stats.WeaponVanity[weaponId].TotalDamage += damage;
                pData.Stats.WeaponVanity[weaponId].TotalAttacks++;

                if ( damage > pData.Stats.WeaponVanity[weaponId].MaxDamageHit )
                    pData.Stats.WeaponVanity[weaponId].MaxDamageHit = damage;
                if ( isCrit )
                    pData.Stats.WeaponVanity[weaponId].CriticalHits++;
            }
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Dmg", steamId );
        }
    }

    // ==========================================
    // 3. BUYING STATS (WEAPONS & UPGRADES)
    // ==========================================

    public static void OnWeaponBought( Scene scene, string steamId )
    {
        var save = SaveManager.Get( scene );
        if ( save == null || !save.IsFactoryReady ) return;

        // --- 1. SAUVEGARDE & 2. RÉSEAU (FACTORY) ---
        save.CurrentFactory.Stats.WeaponsBought++;
        if ( FactoryStats.Get( scene ) != null )
            FactoryStats.Get( scene ).WeaponsBought = save.CurrentFactory.Stats.WeaponsBought;

        // --- 3. API S&BOX (JOUEUR) ---
        if ( CanSendToSbox( scene ) )
            Increment( "total_weapons_bought", 1 );

        // --- 4. ARCHIVE LOCALE (JOUEUR) ---
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalWeaponsBought++;
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Weapon Buy", steamId );
        }
    }

    public static void OnUpgradeBought( Scene scene, string steamId )
    {
        var save = SaveManager.Get( scene );
        if ( save == null || !save.IsFactoryReady ) return;

        // --- 1. SAUVEGARDE & 2. RÉSEAU (FACTORY) ---
        save.CurrentFactory.Stats.UpgradesBought++;
        if ( FactoryStats.Get( scene ) != null )
            FactoryStats.Get( scene ).UpgradesBought = save.CurrentFactory.Stats.UpgradesBought;

        // --- 3. API S&BOX (JOUEUR) ---
        if ( CanSendToSbox( scene ) )
            Increment( "total_upgrades_bought", 1 );

        // --- 4. ARCHIVE LOCALE (JOUEUR) ---
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalUpgradesBought++;
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Upgrade Buy", steamId );
        }
    }

    // ==========================================
    // 4. TIME & META STATS
    // ==========================================

    public static void OnPrestige( Scene scene, int newPrestigeLevel )
    {
        var save = SaveManager.Get( scene );
        if ( save == null || !save.IsFactoryReady ) return;

        // --- 1. SAUVEGARDE & 2. RÉSEAU (FACTORY) ---
        save.CurrentFactory.Stats.TimesPrestiged++;
        save.CurrentFactory.Stats.PrestigePoints += newPrestigeLevel;

        if ( FactoryStats.Get( scene ) != null )
        {
            FactoryStats.Get( scene ).TimesPrestiged = save.CurrentFactory.Stats.TimesPrestiged;
            FactoryStats.Get( scene ).TotalPrestigePoints = save.CurrentFactory.Stats.PrestigePoints;
        }

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.WorldStatUpdate, "Stats: Prestige" );

        // --- 3. API S&BOX (JOUEUR) ---
        if ( CanSendToSbox( scene ) )
        {
            Increment( "total_times_prestiged", 1 );
            Increment( "prestige_points", newPrestigeLevel );
        }

        // --- 4. ARCHIVE LOCALE (JOUEUR) ---
        foreach ( var playerData in save.ActivePlayers.Values )
        {
            if ( playerData?.Stats == null ) continue;
            playerData.Stats.TotalTimesPrestiged++;
            playerData.Stats.TotalPrestigePoints += newPrestigeLevel;
        }
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Prestige" );
    }

    public static void OnPlayTimeAccumulated( Scene scene, float deltaSeconds, string steamId = null )
    {
        var save = SaveManager.Get( scene );
        if ( save == null || !save.IsFactoryReady ) return;

        // --- 1. SAUVEGARDE & 2. RÉSEAU (FACTORY) ---
        save.CurrentFactory.Stats.TimePlayed += deltaSeconds;
        if ( FactoryStats.Get( scene ) != null )
            FactoryStats.Get( scene ).TimePlayed = save.CurrentFactory.Stats.TimePlayed;

        // --- 3. API S&BOX (JOUEUR) ---
        if ( CanSendToSbox( scene ) )
            Increment( "total_playtime_seconds", deltaSeconds );

        // --- 4. ARCHIVE LOCALE (JOUEUR) ---
        foreach ( var playerData in save.ActivePlayers.Values )
        {
            if ( playerData?.Stats == null ) continue;
            playerData.Stats.TotalTimePlayed += deltaSeconds;
        }
    }

    // ==========================================
    // 5. ENERGY STATS
    // ==========================================

    public static void OnEnergyConsumed( Scene scene, string steamId, float energyAmount )
    {
        var save = SaveManager.Get( scene );
        if ( save == null || !save.IsFactoryReady ) return;

        // --- 1. SAUVEGARDE & 2. RÉSEAU (FACTORY) ---
        var fStats = save.CurrentFactory.Stats;
        fStats.EnergyConsumed += energyAmount;
        if ( FactoryStats.Get( scene ) != null )
            FactoryStats.Get( scene ).EnergyConsumed = fStats.EnergyConsumed;

        // --- 3. API S&BOX (JOUEUR) ---
        if ( CanSendToSbox( scene ) )
            Increment( "total_energy_consumed", energyAmount );

        // --- 4. ARCHIVE LOCALE (JOUEUR) ---
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalEnergyConsumed += energyAmount;
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Energy", steamId );
        }
    }

    public static void OnExhaustionPenalty( Scene scene, string steamId )
    {
        var save = SaveManager.Get( scene );
        if ( save == null || !save.IsFactoryReady ) return;

        // --- 1. SAUVEGARDE & 2. RÉSEAU (FACTORY) ---
        var fStats = save.CurrentFactory.Stats;
        fStats.ExhaustionPenalties++;
        if ( FactoryStats.Get( scene ) != null )
            FactoryStats.Get( scene ).ExhaustionPenalties = fStats.ExhaustionPenalties;

        // --- 3. API S&BOX (JOUEUR) ---
        if ( CanSendToSbox( scene ) )
            Increment( "total_exhaustion_penalties", 1 );

        // --- 4. ARCHIVE LOCALE (JOUEUR) ---
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.ExhaustionPenalties++;
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Exhaustion", steamId );
        }
    }
}