using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class PropSpawner : Component
{
    private List<PropDefinition> _allProps;

    public float TimeToNextSpawn
    {
        get
        {
            var upgrades = SaveManager.Instance?.CurrentFactory?.GlobalUpgrades;
            if ( upgrades == null ) return -1.0f;
            if ( upgrades.GetValueOrDefault( "auto_spawn_unlocked", 0 ) == 0 ) return -1.0f;

            float timeBase = 30.0f;

            float totalMultiplier = 1.0f;

            totalMultiplier += upgrades.GetValueOrDefault( "auto_spawn_1", 0 ) * 0.05f;
            totalMultiplier += upgrades.GetValueOrDefault( "auto_spawn_2", 0 ) * 0.25f;
            totalMultiplier += upgrades.GetValueOrDefault( "auto_spawn_3", 0 ) * 1.0f;
            totalMultiplier += upgrades.GetValueOrDefault( "auto_spawn_4", 0 ) * 5.0f;

            return timeBase / totalMultiplier;
        }
    }

    private float _spawnTimer = 0f;

    protected override void OnStart()
    {
        if ( !Networking.IsHost ) return;
        _allProps = ResourceLibrary.GetAll<PropDefinition>().ToList();
        Log.Info( $"[PropSpawner] Loaded {_allProps.Count} prop definition(s)" );

        SpawnProp();
    }

    protected override void OnUpdate()
    {
        if ( !Networking.IsHost ) return;

        float nextSpawn = TimeToNextSpawn;
        if ( nextSpawn <= 0f ) return;

        _spawnTimer += Time.Delta;

        if ( _spawnTimer >= nextSpawn )
        {
            SpawnProp();
            _spawnTimer = 0f;
        }
    }

    public void SpawnProp()
    {
        if ( _allProps == null || _allProps.Count == 0 ) return;

        int maxTier = SaveManager.Instance?.CurrentFactory?.Tier ?? 1;

        int chosenTier = GetRandomTier( maxTier, 1, 2.0f );

        var tierProps = _allProps.Where( p => p.Tier == chosenTier ).ToList();
        var prop = GetRandomPropForTier( tierProps );

        if ( prop != null )
        {
            InstantiateProp( prop );
            Log.Info( $"[PropSpawner] Spawned prop: {prop.PropID} (Tier {prop.Tier}, Rarity {prop.RarityMod})" );
        }
        else
        {
            Log.Warning( $"[PropSpawner] No prop found for Tier {chosenTier}" );
        }

    }

    private void InstantiateProp( PropDefinition data )
    {

        if ( data.Model == null ) return;

        var go = new GameObject();
        var randomOffset = Vector3.Random * 20f;
        var randomRot = Rotation.FromAxis( Vector3.Random.Normal, Game.Random.Float( 0f, 360f ) );
        go.Transform.World = new Transform( WorldPosition + randomOffset, randomRot );

        // ModelRenderer
        var renderer = go.AddComponent<ModelRenderer>();
        renderer.Model = data.Model;

        // ModelCollider
        var collider = go.AddComponent<ModelCollider>();
        collider.Model = data.Model;

        // Rigidbody
        var rb = go.AddComponent<Rigidbody>();
        rb.Velocity = Vector3.Random * Game.Random.Float( 50f, 150f ) + Vector3.Up * Game.Random.Float( 50f, 100f );

        // PropHealth
        var health = go.AddComponent<PropHealth>();
        health.Initialize( data );
    }

    /// <summary>
    /// Tire un Tier au hasard en utilisant une courbe en cloche (Loi Normale).
    /// </summary>
    /// <param name="currentFactoryTier">Le Tier maximum débloqué par le joueur</param>
    /// <param name="centerOffset">Le décalage du centre (ex: 1 pour cibler N-1)</param>
    /// <param name="spread">La largeur de la cloche</param>
    public int GetRandomTier( int currentFactoryTier, int centerOffset = 1, float spread = 2.0f )
    {
        int targetTier = Math.Max( 1, currentFactoryTier - centerOffset );

        var tierWeights = new Dictionary<int, float>();
        float totalWeight = 0f;

        for ( int t = 1; t <= currentFactoryTier; t++ )
        {
            float distance = Math.Abs( t - targetTier );

            float weight = MathF.Exp( -(distance * distance) / spread );

            weight = Math.Max( 0.01f, weight );

            tierWeights[t] = weight;
            totalWeight += weight;
        }

        float randomValue = Game.Random.Float( 0f, totalWeight );
        foreach ( var kvp in tierWeights )
        {
            randomValue -= kvp.Value;
            if ( randomValue <= 0 )
            {
                return kvp.Key;
            }
        }

        return targetTier;
    }

    private PropDefinition GetRandomPropForTier( List<PropDefinition> tierProps )
    {
        if ( tierProps == null || tierProps.Count == 0 ) return null;

        int totalWeight = tierProps.Sum( p => p.CalculatedSpawnWeight );

        int randomValue = Game.Random.Int( 0, totalWeight );

        foreach ( var prop in tierProps )
        {
            randomValue -= prop.CalculatedSpawnWeight;
            if ( randomValue <= 0 )
            {
                return prop;
            }
        }

        return tierProps.Last();
    }
}
