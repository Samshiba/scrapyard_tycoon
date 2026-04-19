using Sandbox;

public static class GameStats
{
    // Vérification de sécurité (Anti-cheat custom + Natif)
    private static bool CanSendToSbox()
    {
        return SaveManager.Instance != null && !SaveManager.Instance.CurrentFactory.IsSandbox;
    }

    public static void OnScrapGained( string steamId, double amount )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;

        save.CurrentFactory.Stats.ScrapGainedSession += amount;
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.WorldStatUpdate, "Stats: Scrap" );

        if ( CanSendToSbox() )
            Sandbox.Services.Stats.Increment( "lifetime_scrap", amount );
    }

    public static void OnPropDestroyed( string steamId, string propId, string weaponId, double scrapYield )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;

        save.CurrentFactory.Stats.PropsDestroyed++;

        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            // Vanity Prop (Combien de fois on a cassé CE prop)
            if ( !pData.Stats.PropVanity.ContainsKey( propId ) )
                pData.Stats.PropVanity[propId] = new VanityStat();

            pData.Stats.PropVanity[propId].Count++;
            pData.Stats.PropVanity[propId].Value += scrapYield;

            // Vanity Arme (Combien de props détruits avec CETTE arme)
            if ( !string.IsNullOrEmpty( weaponId ) )
            {
                if ( !pData.Stats.WeaponVanity.ContainsKey( weaponId ) )
                    pData.Stats.WeaponVanity[weaponId] = new VanityStat();
                pData.Stats.WeaponVanity[weaponId].Count++;
            }
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Prop", steamId );
        }

        if ( CanSendToSbox() )
        {
            Sandbox.Services.Stats.Increment( "total_props_destroyed", 1 );
            Sandbox.Services.Stats.Increment( $"props_destroyed_{propId}", 1 );
            Sandbox.Services.Stats.Increment( $"props_destroyed_with_{weaponId}", 1 );
            Sandbox.Services.Stats.Increment( $"scrap_from_{propId}", scrapYield );
        }
    }

    public static void OnDamageDealt( string steamId, string weaponId, double damage )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;


        if ( damage > save.CurrentFactory.Stats.MaxDamageHit )
            save.CurrentFactory.Stats.MaxDamageHit = damage;

        if ( save.ActivePlayers.TryGetValue( steamId, out var pData ) )
        {
            pData.Stats.TotalAttacks++;

            // Vanity Arme (Dégâts infligés)
            if ( !string.IsNullOrEmpty( weaponId ) )
            {
                if ( !pData.Stats.WeaponVanity.ContainsKey( weaponId ) )
                    pData.Stats.WeaponVanity[weaponId] = new VanityStat();
                pData.Stats.WeaponVanity[weaponId].Value += damage;
            }
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PlayerStatUpdate, "Stats: Dmg", steamId );
        }

        if ( CanSendToSbox() )
        {
            Sandbox.Services.Stats.Increment( "total_damage", damage );
            Sandbox.Services.Stats.SetValue( "max_damage_hit", damage );
            Sandbox.Services.Stats.Increment( "total_attacks", 1 );
            Sandbox.Services.Stats.Increment( $"damage_with_{weaponId}", damage );
            Sandbox.Services.Stats.Increment( $"attacks_with_{weaponId}", 1 );
        }
    }

    public static void OnPrestige( int newPrestigeLevel )
    {
        var save = SaveManager.Instance;
        if ( save == null || !save.IsFactoryReady ) return;


        if ( CanSendToSbox() )
        {
            Sandbox.Services.Stats.Increment( "total_prestiges", 1 );
            Sandbox.Services.Stats.SetValue( "highest_prestige", newPrestigeLevel );
        }
    }
}