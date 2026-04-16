using Sandbox;
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

        var data = Game.Random.FromList( _allProps );
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

        Log.Info( $"[PropSpawner] Spawned prop: {data.PropID} (Tier {data.Tier})" );
    }
}
