using Microsoft.VisualBasic;
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

    public void Initialize(PropDefinition data)
    {
        config = BalanceConfig.Instance;
        if (config == null)
        {
            Log.Error("Aucun fichier BalanceConfig trouvé dans les assets !");
            return;
        }
        Data = data;
        GibPrefab = config.GibPrefab;

        // 1. HP Calcul : (base_hp * (hp_mult^(tier-1))) * rarity_mod
        float rawHP = config.BaseHP * MathF.Pow(config.HPMult, data.Tier - 1) * data.RarityMod;
        CurrentHealth = MathF.Round(rawHP);

        // 2. Value Calcul : (base_value * (value_mult^(tier-1))) * rarity_mod * jackpot_bonus
        float jackpot = data.RarityMod >= 3 ? config.JackpotBonus : 1.0f;
        float rawValue = config.BaseValue * MathF.Pow(config.ValueMult, data.Tier - 1) * (data.RarityMod * jackpot);
        TotalValue = MathF.Round(rawValue);

        // 3. Gib Count Calcul : base_gibs + ((tier-1) * gibs_per_tier), clamped at max_gibs
        int desiredGibs = config.BaseGibs + ((data.Tier - 1) * config.GibsPerTier);
        FinalGibCount = Math.Clamp(desiredGibs, 0, config.MaxGibs);

        // 4. Value per Gib : TotalValue / FinalGibCount (with safety check)
        ValuePerGib = FinalGibCount > 0 ? (float)Math.Round(TotalValue / FinalGibCount, 2) : TotalValue;
    }

    public void OnDamage(in DamageInfo damage)
    {
        if (!damage.Tags.Has("player") && !damage.Tags.Has("machine"))
        {
            return;
        }
        Log.Info($"PropHealth: Received {damage.Damage} damage. Tags = {string.Join(", ", damage.Tags)}");
        CurrentHealth -= damage.Damage;
        FlashWhite();
        if (CurrentHealth <= 0) OnBreak();
    }

    public async void FlashWhite()
    {
        var renderer = GameObject.Components.Get<ModelRenderer>(FindMode.EverythingInSelfAndDescendants);
        if (renderer == null) return;

        var originalMat = renderer.MaterialOverride;
        renderer.MaterialOverride = Material.Load("materials/dev/primary_white.vmat");
        await Task.DelayRealtime(50);
        renderer.MaterialOverride = originalMat;
    }

    private void OnBreak()
    {
        // 1. Check if there are SubProps to spawn instead of gibs
        if (Data.SubProps != null && Data.SubProps.Count > 0)
        {
            foreach (var drop in Data.SubProps)
            {
                if (drop.Prop == null) continue;

                for (int i = 0; i < drop.Count; i++)
                {
                    SpawnChild(drop.Prop);
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

    private void SpawnChild(PropDefinition childData)
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
        rb.Velocity = Vector3.Random * Game.Random.Float(50f, 150f) + Vector3.Up * Game.Random.Float(50f, 100f);

        // PropHealth
        var health = childGo.AddComponent<PropHealth>();
        health.Initialize(childData);
        health.GibPrefab = config.GibPrefab;
    }

    private void BreakIntoGibs()
    {
        if (GibPrefab == null)
        {
            Log.Warning($"PropHealth: GibPrefab is not set on {GameObject.Name}, skipping gib spawn.");
            return;
        }

        for (int i = 0; i < FinalGibCount; i++)
        {
            var randomDir = new Vector3(
                Game.Random.Float(-1f, 1f),
                Game.Random.Float(-1f, 1f),
                Game.Random.Float(0f, 0.4f)
            ).Normal;

            var spawnOffset = randomDir * Game.Random.Float(5f, 15f) + Vector3.Up * Game.Random.Float(5f, 15f);
            var gib = GibPrefab.Clone(WorldPosition + spawnOffset);

            var scrapItem = gib.Components.Get<ResourceGib>(FindMode.EverythingInSelfAndDescendants);
            if (scrapItem == null) continue;

            var randomResourceType = Data.Types.Count > 0 ? Data.Types[Game.Random.Int(0, Data.Types.Count - 1)] : ResourceType.Wood;

            scrapItem.Initialize(randomResourceType, ValuePerGib, randomDir);
        }

        GameObject.Destroy();
    }
}