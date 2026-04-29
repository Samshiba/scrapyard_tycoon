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
        if ( Networking.IsHost && !_isLoaded && SaveManager.Instance?.IsFactoryReady == true )
        {
            var factoryUpgrades = SaveManager.Instance.CurrentFactory.GlobalUpgrades;

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

        foreach ( var kvp in SyncedUpgrades )
        {
            string upgradeId = kvp.Key;
            int level = kvp.Value;

            if ( level <= 0 ) continue;

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

        Log.Info( $"[GlobalUpgradesSystem] Cache rebuilt for {_statCache.Count} stats." );
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
        // 1. Validate server-side dependencies
        if ( !Networking.IsHost ) return false;

        var factoryUpgrades = SaveManager.Instance?.CurrentFactory?.GlobalUpgrades;
        if ( factoryUpgrades == null || UpgradeManager.Instance == null || FactoryStats.Instance == null ) return false;

        if ( !UpgradeManager.Instance.Database.TryGetValue( upgradeId, out var node ) ) return false;

        int currentLevel = GetUpgradeLevel( upgradeId );
        if ( currentLevel >= node.MaxLevel ) return false;

        // 2. Verify all parent upgrade requirements are met
        if ( !UpgradeManager.Instance.IsNodeUnlocked( upgradeId, SaveManager.Instance.CurrentFactory ) ) return false;

        double cost = node.GetCostForLevel( currentLevel );

        // 3. Attempt payment
        if ( FactoryStats.Instance.SpendScrap( cost ) )
        {
            factoryUpgrades[upgradeId] = currentLevel + 1;
            SyncedUpgrades[upgradeId] = currentLevel + 1;

            // 4. Apply tier upgrade bonus
            if ( upgradeId == "root_node" )
            {
                SaveManager.Instance.CurrentFactory.Tier++;
            }

            GameStats.OnMoneySpent( steamId, cost );
            GameStats.OnUpgradeUnlocked( steamId, upgradeId );

            RebuildCache();

            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.GlobalUpgradeChanged, $"{node.Id} → Level {currentLevel + 1}" );
            Log.Info( $"[GlobalUpgradesSystem] Upgrade purchased: {node.Id} (Level {currentLevel + 1}) for {cost} scrap" );
            return true;
        }

        Log.Warning( $"[GlobalUpgradesSystem] Insufficient funds for {node.Id}." );
        return false;
    }

    public bool IsUpgraded( string upgradeId, int requiredLevel = 1 )
    {
        return GetUpgradeLevel( upgradeId ) >= requiredLevel;
    }
}