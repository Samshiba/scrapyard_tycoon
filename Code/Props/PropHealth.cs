using Sandbox;
using System;

public sealed class PropHealth : Component, Component.IDamageable
{
    [Property] public float CurrentHealth { get; set; }
    [Property] public float TotalValue { get; set; }
    [Property] public int FinalGibCount { get; set; }
    [Property] public float ValuePerGib { get; set; }

    [Property] public PropDefinition Data { get; private set; }

    [Property] public GameObject GibPrefab { get; set; }

    [Property] public BalanceConfig config { get; set; }

    private bool _isFlashing = false;

    // Track last attacker for stats
    private string _lastAttackerSteamId = "";
    private string _lastWeaponId = "";

    public void Initialize( PropDefinition data )
    {
        config = BalanceConfig.Instance;
        if ( config == null )
        {
            Log.Error( "[PropHealth] ERROR: BalanceConfig not found. Ensure BalanceConfig.asset is in your project and loaded." );
            return;
        }
        Data = data;
        GibPrefab = config.GibPrefab;

        CurrentHealth = PropStatsCalculator.GetHealth( data );
        TotalValue = PropStatsCalculator.GetValue( data );
        FinalGibCount = PropStatsCalculator.GetGibCount( data );
        ValuePerGib = PropStatsCalculator.GetValuePerGib( data );
    }

    public void OnDamage( in DamageInfo damage )
    {
        if ( !damage.Tags.Has( "player" ) && !damage.Tags.Has( "machine" ) && !damage.Tags.Has( "explosion" ) && !damage.Tags.Has( "piercing" ) )
        {
            return;
        }
        Log.Info( $"[PropHealth] Damage received: {damage.Damage} from tags: {string.Join( ", ", damage.Tags )}" );
        if ( damage.Damage <= 0 ) return;
        if ( GameStats.CanSendToSbox( Scene ) && damage.Damage >= 10f * CurrentHealth )
        {
            Sandbox.Services.Achievements.Unlock( "scrt_overkill" );
        }
        CurrentHealth -= damage.Damage;
        FlashDamage();
        if ( CurrentHealth <= 0 ) OnBreak();
    }

    public void OnDamageDealt( string steamId, string weaponId, double damage, bool isCrit )
    {
        _lastAttackerSteamId = steamId;
        _lastWeaponId = weaponId;

        // Call stats immediately for damage dealt event, passing isCrit info
        GameStats.OnDamageDealt( Scene, steamId, weaponId, damage, isCrit );
    }

    private Color GetFlashColor()
    {
        if ( Data.IsJackpot || Data.RarityMod >= 10 ) return (Color)Color.Parse( "#ffde23" );

        if ( Data.RarityMod > 6 ) return (Color)Color.Parse( "#ff0000" );

        if ( Data.RarityMod > 3 ) return (Color)Color.Parse( "#002fff" );

        return (Color)Color.Parse( "#FFFFFF" );
    }

    public async void FlashDamage()
    {
        if ( _isFlashing ) return;

        var renderer = GameObject.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
        if ( renderer == null ) return;

        _isFlashing = true;

        var originalMat = renderer.MaterialOverride;
        var originalTint = renderer.Tint;

        renderer.MaterialOverride = Material.Load( "materials/dev/primary_white.vmat" );
        renderer.Tint = GetFlashColor();

        await Task.DelayRealtime( 50 );

        if ( !renderer.IsValid() || !GameObject.IsValid() ) return;

        renderer.MaterialOverride = originalMat;
        renderer.Tint = originalTint;
        _isFlashing = false;
    }

    private void OnBreak()
    {
        // Achievements
        if ( GameStats.CanSendToSbox( Scene ) )
        {
            Sandbox.Services.Achievements.Unlock( "first_blood" );
            if ( Data.IsJackpot )
            {
                Sandbox.Services.Achievements.Unlock( "scrt_jackpot" );
            }
        }

        // Check if there are SubProps to spawn instead of gibs
        if ( Data.SubProps != null && Data.SubProps.Count > 0 )
        {
            foreach ( var drop in Data.SubProps )
            {
                if ( drop.Prop == null ) continue;

                for ( int i = 0; i < drop.Count; i++ )
                {
                    SpawnChild( drop.Prop );
                }
            }
        }
        // Else, break into gibs
        else
        {
            BreakIntoGibs();
        }

        GameStats.OnPropDestroyed( Scene, _lastAttackerSteamId, Data.PropID, _lastWeaponId, TotalValue, FinalGibCount );

        GameObject.Destroy();
    }

    private void SpawnChild( PropDefinition childData )
    {
        var childGo = new GameObject();
        childGo.WorldPosition = WorldPosition + Vector3.Random * 15f;

        // ModelRenderer
        var renderer = childGo.AddComponent<ModelRenderer>();
        renderer.Model = childData.Model;

        // ModelCollider
        var collider = childGo.AddComponent<ModelCollider>();
        collider.Model = childData.Model;

        // Rigidbody
        var rb = childGo.AddComponent<Rigidbody>();
        rb.Velocity = Vector3.Random * Game.Random.Float( 50f, 150f ) + Vector3.Up * Game.Random.Float( 50f, 100f );

        // PropHealth
        var health = childGo.AddComponent<PropHealth>();
        health.Initialize( childData );
        health.GibPrefab = config.GibPrefab;
    }

    private void BreakIntoGibs()
    {
        if ( GibPrefab == null )
        {
            Log.Warning( $"[PropHealth] WARNING: GibPrefab not set on {GameObject.Name}. Gib spawning disabled." );
            return;
        }

        Color gibColor = GetFlashColor();

        for ( int i = 0; i < FinalGibCount; i++ )
        {
            var randomDir = new Vector3(
                Game.Random.Float( -1f, 1f ),
                Game.Random.Float( -1f, 1f ),
                Game.Random.Float( 0f, 0.4f )
            ).Normal;

            float explosionForce = Data.IsJackpot ? 3.0f : 1.0f;
            var spawnOffset = randomDir * Game.Random.Float( 5f, 15f ) * explosionForce + Vector3.Up * Game.Random.Float( 5f, 15f ) * explosionForce;

            var gib = GibPrefab.Clone( WorldPosition + spawnOffset );

            var renderer = gib.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
            if ( renderer != null )
            {
                renderer.Tint = gibColor;
            }

            var scrapItem = gib.Components.Get<ResourceGib>( FindMode.EverythingInSelfAndDescendants );
            if ( scrapItem == null ) continue;

            var randomResourceType = Data.Types.Count > 0 ? Data.Types[Game.Random.Int( 0, Data.Types.Count - 1 )] : ResourceType.Wood;

            scrapItem.Initialize( randomResourceType, ValuePerGib, randomDir );
        }

        if ( Data.IsJackpot )
        {
            TriggerJackpotEffects();
        }

        GameObject.Destroy();
    }

    private void TriggerJackpotEffects()
    {
        // 1. LE SON (KACHING !)
        // Joue un son très distinctif, fort, et satisfaisant.
        // Remplace "ui.coins" par le nom d'un son de ta bibliothèque S&box.
        Sound.Play( "ui.coins", WorldPosition );
        Sound.Play( "explosion.small", WorldPosition ); // Un petit boom pour le côté impact

        // 2. LES PARTICULES (Feu d'artifice)
        // Spawn un système de particules (des étincelles dorées ou des confettis)
        // Assure-toi d'avoir un petit prefab de particules prêt dans tes assets.
        /* var vfx = ParticlePrefab.Clone(WorldPosition);
        vfx.DestroyAsync(2f); // Se détruit tout seul après 2 secondes
        */

        // 3. LE TEXTE FLOTTANT (La cerise sur le gâteau)
        // Montre au joueur COMBIEN il vient de faire exploser d'un coup.
        float displayValue = PropStatsCalculator.GetValue( Data );
        Log.Info( $"[JACKPOT] {Data.PropID} destroyed for {displayValue} Scrap!" );

        // Si tu as un système de Floating Text :
        // FloatingText.Spawn(WorldPosition + Vector3.Up * 30f, $"JACKPOT! {displayValue}", Color.Parse("#FFD700"));
    }
}