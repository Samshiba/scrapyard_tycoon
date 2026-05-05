using Sandbox;
using System;
using System.Collections.Generic;

public sealed class GlobalUpgradesSystem : Component
{
    public static GlobalUpgradesSystem Instance { get; private set; }

    [Sync] public NetDictionary<string, int> SyncedUpgrades { get; set; } = new();

    private Dictionary<string, StatModifiers> _statCache = new();

    private bool _isLoaded = false;

    protected override void OnAwake()
    {
        Instance = this;
    }

    protected override void OnUpdate()
    {
        if ( Networking.IsHost && !_isLoaded && SaveManager.Get( Scene )?.IsFactoryReady == true )
        {
            var factoryUpgrades = SaveManager.Get( Scene ).CurrentFactory.GlobalUpgrades;

            SyncedUpgrades.Clear();
            foreach ( var kvp in factoryUpgrades )
            {
                SyncedUpgrades[kvp.Key] = kvp.Value;
            }

            RebuildCache();

            _isLoaded = true;
            Log.Info( $"[GlobalUpgradesSystem] Loaded {SyncedUpgrades.Count} upgrades." );
        }
    }

    public void RebuildCache()
    {
        _statCache.Clear();

        // 1. Load Upgrades
        foreach ( var kvp in SyncedUpgrades )
        {
            ApplyDefToCache( kvp.Key, kvp.Value );
        }

        // 2. Aplly Prestige Points bonuses
        var factory = SaveManager.Get( Scene )?.CurrentFactory;
        Log.Info( factory );
        if ( factory != null && factory.PrestigePoints > 0 )
        {
            if ( !_statCache.ContainsKey( "all_stats" ) )
                _statCache["all_stats"] = new StatModifiers();

            _statCache["all_stats"].Multiplicative += (factory.PrestigePoints * 0.01f);
        }

        Log.Info( $"[GlobalUpgradesSystem] Cache rebuilt for {_statCache.Count} stats." );
    }

    private void ApplyDefToCache( string upgradeId, int level )
    {
        if ( level <= 0 ) return;

        if ( UpgradeManager.Instance.Database.TryGetValue( upgradeId, out var def ) )
        {
            string stat = def.StatModified;

            if ( !_statCache.ContainsKey( stat ) )
                _statCache[stat] = new StatModifiers();

            float effect = def.EffectValuePerLevel * level;

            switch ( def.ValueType )
            {
                case UpgradeValueType.Additive:
                    _statCache[stat].Additive += effect;
                    break;
                case UpgradeValueType.Multiplicative:
                    _statCache[stat].Multiplicative += effect;
                    break;
                case UpgradeValueType.Reductive:
                    _statCache[stat].Multiplicative -= effect;
                    break;
                case UpgradeValueType.CompoundMultiplicative:
                    _statCache[stat].Compound *= MathF.Pow( 1 + def.EffectValuePerLevel, level );
                    break;
                case UpgradeValueType.Conversion:
                    if ( !string.IsNullOrEmpty( def.StatSource ) )
                    {
                        if ( !_statCache[stat].Conversions.ContainsKey( def.StatSource ) )
                            _statCache[stat].Conversions[def.StatSource] = 0f;

                        _statCache[stat].Conversions[def.StatSource] += effect;
                    }
                    break;
            }
        }
    }

    public StatModifiers GetModifiersForStat( string statTarget )
    {
        return _statCache.TryGetValue( statTarget, out var mods ) ? mods : new StatModifiers();
    }

    public StatModifiers GetAggregatedModifiers( params string[] tags )
    {
        var result = new StatModifiers { Additive = 0f, Multiplicative = 1f, Compound = 1f };
        float totalMultiBonus = 0f;

        foreach ( string tag in tags )
        {
            if ( _statCache.TryGetValue( tag, out var mods ) )
            {
                result.Additive += mods.Additive;
                totalMultiBonus += (mods.Multiplicative - 1f);
                result.Compound *= mods.Compound;

                // (Optionnel) Fusionner les Conversions ici si besoin
            }
        }

        result.Multiplicative = 1f + totalMultiBonus;
        return result;
    }

    public float ApplyModifiers( string statTarget, float baseValue )
    {
        if ( _statCache.TryGetValue( statTarget, out var mods ) )
        {
            return (baseValue + mods.Additive) * mods.Multiplicative * mods.Compound;
        }

        return baseValue;
    }

    public int GetUpgradeLevel( string upgradeId )
    {
        return SyncedUpgrades.TryGetValue( upgradeId, out var level ) ? level : 0;
    }

    public bool TryPurchaseUpgrade( string upgradeId, string steamId )
    {
        if ( !Networking.IsHost ) return false;

        // Use centralized FactoryStats which handles:
        // 1. Validation of upgrade and player requirements
        // 2. Payment handling (SpendScrap or SpendPrestige)
        // 3. SaveManager updates
        // 4. [Sync] updates (SyncedUpgrades, Tier)
        // 5. Tier increment for root_node
        // 6. GameStats tracking (OnMoneySpent, OnUpgradeBought)
        // 7. SaveEventBus notifications
        // 8. Cache rebuild
        return FactoryStats.Get( Scene )?.PurchaseUpgrade( upgradeId, steamId ) ?? false;
    }

    public bool IsUpgraded( string upgradeId, int requiredLevel = 1 )
    {
        return GetUpgradeLevel( upgradeId ) >= requiredLevel;
    }
}