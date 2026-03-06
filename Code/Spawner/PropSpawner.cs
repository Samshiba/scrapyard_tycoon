using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class PropSpawner : Component
{
    private List<PropDefinition> _allProps;

    protected override void OnStart()
    {
        _allProps = ResourceLibrary.GetAll<PropDefinition>().ToList();
        Log.Info($"PropSpawner: {_allProps.Count} prop(s) trouvé(s).");
        SpawnProp();
    }

    public void SpawnProp()
    {
        if (_allProps == null || _allProps.Count == 0) return;

        var data = Game.Random.FromList(_allProps);
        if (data.Model == null) return;

        var go = new GameObject();
        var randomOffset = Vector3.Random * 20f;
        var randomRot = Rotation.FromAxis(Vector3.Random.Normal, Game.Random.Float(0f, 360f));
        go.Transform.World = new Transform(WorldPosition + randomOffset, randomRot);

        // ModelRenderer
        var renderer = go.AddComponent<ModelRenderer>();
        renderer.Model = data.Model;

        // ModelCollider
        var collider = go.AddComponent<ModelCollider>();
        collider.Model = data.Model;

        // Rigidbody
        var rb = go.AddComponent<Rigidbody>();
        rb.Velocity = Vector3.Random * Game.Random.Float(50f, 150f) + Vector3.Up * Game.Random.Float(50f, 100f);

        // PropHealth
        var health = go.AddComponent<PropHealth>();
        health.Initialize(data);

        Log.Info($"Spawned: {data.PropID} (Tier {data.Tier})");
    }
}
