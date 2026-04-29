using Sandbox;
using static Sandbox.Services.Stats;

public static class GameStats
{
    // Vérification de sécurité (Anti-cheat custom + Natif)
    private static bool CanSendToSbox()
    {
        return SaveManager.Instance != null && !SaveManager.Instance.CurrentFactory.IsSandbox;
    }

    public static void OnScrapGained( string steamId, double totalAmount, double largestItemValue = 0 )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;

        save.CurrentFactory.Stats.ScrapGainedSession += totalAmount;
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.WorldStatUpdate, "Stats: Scrap" );

        // ARCHIVE: Update player local stats for archiving
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalScrapCollected += totalAmount;
            if ( largestItemValue > 0 && largestItemValue > pData.Stats.LargestScrapGain )
                pData.Stats.LargestScrapGain = largestItemValue;
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Scrap", steamId );
        }

        if ( CanSendToSbox() )
        {
            Increment( "lifetime_scrap", totalAmount );
            if ( largestItemValue > 0 )
                SetValue( "largest_scrap_gain", largestItemValue );
        }
    }

    public static void OnPropDestroyed( string steamId, string propId, string weaponId, double scrapYield, int gibsDropped )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;

        save.CurrentFactory.Stats.PropsDestroyed++;
        if ( gibsDropped > 0 )
            save.CurrentFactory.Stats.GibsDroppedSession += gibsDropped;

        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalPropsDestroyed++;
            pData.Stats.TotalTargetsDestroyed++;

            // ARCHIVE: Per-prop breakdown
            if ( !pData.Stats.PropVanity.ContainsKey( propId ) )
                pData.Stats.PropVanity[propId] = new PropVanityStat();
            pData.Stats.PropVanity[propId].TimesDestroyed++;
            pData.Stats.PropVanity[propId].TotalScrapDropped += scrapYield;
            pData.Stats.PropVanity[propId].NumberOfGibsDropped += gibsDropped;

            // ARCHIVE: Per-weapon breakdown
            if ( !string.IsNullOrEmpty( weaponId ) )
            {
                if ( !pData.Stats.WeaponVanity.ContainsKey( weaponId ) )
                    pData.Stats.WeaponVanity[weaponId] = new WeaponVanityStat();
                pData.Stats.WeaponVanity[weaponId].TargetsDestroyed++;
            }
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Prop", steamId );
        }

        if ( CanSendToSbox() )
        {
            // PLAYER: Total targets destroyed in lifetime
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
    }

    public static void OnDamageDealt( string steamId, string weaponId, double damage, bool isCrit = false )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;

        if ( damage > save.CurrentFactory.Stats.MaxDamageHit )
            save.CurrentFactory.Stats.MaxDamageHit = damage;

        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalAttacks++;
            pData.Stats.TotalDamageDealt += damage;

            // ARCHIVE: Track highest damage hit overall
            if ( damage > pData.Stats.HighestDamageHit )
                pData.Stats.HighestDamageHit = damage;

            // ARCHIVE: Track critical hits
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
                // ARCHIVE: Track max damage with this weapon
                if ( damage > pData.Stats.WeaponVanity[weaponId].MaxDamageHit )
                    pData.Stats.WeaponVanity[weaponId].MaxDamageHit = damage;
                // ARCHIVE: Track crits with this weapon
                if ( isCrit )
                    pData.Stats.WeaponVanity[weaponId].CriticalHits++;
            }
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Dmg", steamId );
        }

        if ( CanSendToSbox() )
        {
            // PLAYER: Damage stats (lifetime)
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
                // PLAYER: Highest damage hit with this specific weapon
                SetValue( $"highest_damage_hit_with_{weaponId}", damage );
                if ( isCrit )
                    Increment( $"crits_with_{weaponId}", 1 );
            }
        }
    }

    public static void OnPrestige( int newPrestigeLevel )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;

        // ARCHIVE: Local prestige tracking
        foreach ( var playerData in save.ActivePlayers.Values )
        {
            if ( playerData?.Stats == null ) continue;
            playerData.Stats.TimesPrestiged++;
            playerData.Stats.PrestigePoints += newPrestigeLevel;
            playerData.Stats.FactoryResets++;
        }
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Prestige" );

        if ( CanSendToSbox() )
        {
            Increment( "times_prestiged", 1 );
            Increment( "prestige_points", newPrestigeLevel );
        }
    }

    public static void OnPlayTimeAccumulated( float deltaSeconds, string steamId = null )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;

        // ARCHIVE: Update all players' lifetime playtime
        foreach ( var playerData in save.ActivePlayers.Values )
        {
            if ( playerData?.Stats == null ) continue;
            playerData.Stats.LifetimePlaytime += deltaSeconds;
        }

        if ( CanSendToSbox() )
        {
            Increment( "total_playtime_seconds", deltaSeconds );
        }
    }

    public static void OnMoneySpent( string steamId, double amount )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;

        // ARCHIVE: Local spending tracking
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalMoneySpent += amount;
            if ( amount > pData.Stats.LargestSinglePurchase )
                pData.Stats.LargestSinglePurchase = amount;
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Money", steamId );
        }

        if ( CanSendToSbox() )
        {
            // PLAYER: Lifetime money spent across all factories/prestiges
            Increment( "total_money_spent", amount );
            SetValue( "largest_single_purchase", amount );
        }
    }

    public static void OnUpgradeUnlocked( string steamId, string upgradeId )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;

        // ARCHIVE: Local upgrade unlock tracking
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.UpgradesUnlocked++;
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Upgrade", steamId );
        }

        if ( CanSendToSbox() )
        {
            // PLAYER: Total upgrades unlocked (cumule même après reset)
            Increment( "upgrades_unlocked", 1 );
            // PLAYER: Per-upgrade unlock count
            Increment( $"upgrade_{upgradeId}_unlocked", 1 );
        }
    }

    public static void OnWeaponUnlocked( string steamId, string weaponId )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;

        // ARCHIVE: Local weapon unlock tracking
        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.WeaponsUnlocked++;
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Weapon Unlock", steamId );
        }

        if ( CanSendToSbox() )
        {
            // PLAYER: Total weapons unlocked (cumule même après reset)
            Increment( "weapons_unlocked", 1 );
            // PLAYER: Per-weapon unlock count
            Increment( $"weapon_{weaponId}_unlocked", 1 );
        }
    }
}