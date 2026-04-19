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
        if ( !damage.Tags.Has( "player" ) && !damage.Tags.Has( "machine" ) && !damage.Tags.Has( "explosion" ) )
        {
            return;
        }
        Log.Info( $"[PropHealth] Damage received: {damage.Damage} from tags: {string.Join( ", ", damage.Tags )}" );
        CurrentHealth -= damage.Damage;
        FlashWhite();
        if ( CurrentHealth <= 0 ) OnBreak();
    }

    public void OnDamageDealt( string steamId, string weaponId, double damage, bool isCrit )
    {
        _lastAttackerSteamId = steamId;
        _lastWeaponId = weaponId;

        // Call stats immediately for damage dealt event
        GameStats.OnDamageDealt( steamId, weaponId, damage );
    }

    public async void FlashWhite()
    {
        var renderer = GameObject.Components.Get<ModelRenderer>( FindMode.EverythingInSelfAndDescendants );
        if ( renderer == null ) return;

        var originalMat = renderer.MaterialOverride;
        renderer.MaterialOverride = Material.Load( "materials/dev/primary_white.vmat" );
        await Task.DelayRealtime( 50 );
        renderer.MaterialOverride = originalMat;
    }

    private void OnBreak()
    {
        // Call stats for prop destroyed
        GameStats.OnPropDestroyed( _lastAttackerSteamId, Data.PropID, _lastWeaponId, TotalValue );

        // 1. Check if there are SubProps to spawn instead of gibs
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
        // 2. Else, break into gibs
        else
        {
            BreakIntoGibs();
        }

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

        for ( int i = 0; i < FinalGibCount; i++ )
        {
            var randomDir = new Vector3(
                Game.Random.Float( -1f, 1f ),
                Game.Random.Float( -1f, 1f ),
                Game.Random.Float( 0f, 0.4f )
            ).Normal;

            var spawnOffset = randomDir * Game.Random.Float( 5f, 15f ) + Vector3.Up * Game.Random.Float( 5f, 15f );
            var gib = GibPrefab.Clone( WorldPosition + spawnOffset );

            var scrapItem = gib.Components.Get<ResourceGib>( FindMode.EverythingInSelfAndDescendants );
            if ( scrapItem == null ) continue;

            var randomResourceType = Data.Types.Count > 0 ? Data.Types[Game.Random.Int( 0, Data.Types.Count - 1 )] : ResourceType.Wood;

            scrapItem.Initialize( randomResourceType, ValuePerGib, randomDir );
        }

        GameObject.Destroy();
    }
}