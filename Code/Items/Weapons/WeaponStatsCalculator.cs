using Sandbox;

public static class WeaponStatsCalculator
{
    /// <summary>
    /// ENDPOINT UNIQUE : Génère la formule complète pour n'importe quelle stat d'arme.
    /// Il construit automatiquement les tags (ex: weapon_damage, pistol_damage, etc.)
    /// </summary>
    public static WeaponStatFormula GetStatFormula( WeaponDefinition weapon, WeaponStatTarget stat )
    {
        if ( weapon == null )
            return new WeaponStatFormula();

        float baseValue = stat switch
        {
            WeaponStatTarget.Damage => weapon.DamageBase,
            WeaponStatTarget.AttackRate => weapon.AttackRateBase,
            WeaponStatTarget.CriticalChance => weapon.CriticalChanceBase,
            WeaponStatTarget.CriticalDamage => weapon.CriticalDamageBase,
            WeaponStatTarget.Range => weapon.RangeBase,
            WeaponStatTarget.EnergyCost => weapon.EnergyCost,
            _ => 0f
        };

        string statName = stat.ToString().ToLower();

        string allStatsTag = $"all_stats";
        string globalTag = $"weapon_{statName}";
        string categoryTag = $"{weapon.Category.ToString().ToLower()}_{statName}";
        string elementTag = $"{weapon.DamageType.ToString().ToLower()}_{statName}";

        return CalculateFormula( baseValue, allStatsTag, globalTag, categoryTag, elementTag );
    }

    /// <summary>
    /// ENDPOINT UNIQUE : Retourne juste la valeur finale (float) si on n'a pas besoin du Codex.
    /// Fini la duplication de code !
    /// </summary>
    public static float GetStat( WeaponDefinition weapon, WeaponStatTarget stat )
    {
        return GetStatFormula( weapon, stat ).FinalValue;
    }


    /// <summary>
    /// Fonction interne universelle pour construire l'objet Formule
    /// </summary>
    private static WeaponStatFormula CalculateFormula( float baseValue, params string[] statTags )
    {
        if ( GlobalUpgradesSystem.Instance == null )
            return new WeaponStatFormula { BaseValue = baseValue, FinalValue = baseValue };

        var mods = GlobalUpgradesSystem.Instance.GetAggregatedModifiers( statTags );

        float finalValue = (baseValue + mods.Additive) * mods.Multiplicative * mods.Compound;

        return new WeaponStatFormula
        {
            BaseValue = baseValue,
            AdditiveBonus = mods.Additive,
            MultiplicativeBonus = mods.Multiplicative - 1f,
            CompoundMultiplier = mods.Compound,
            FinalValue = finalValue
        };
    }

    public static float GetDPS( WeaponDefinition weapon )
    {
        if ( weapon == null ) return 0f;

        float damage = GetStat( weapon, WeaponStatTarget.Damage );
        float attackRate = GetStat( weapon, WeaponStatTarget.AttackRate );
        float critChance = GetStat( weapon, WeaponStatTarget.CriticalChance );
        float critDamage = GetStat( weapon, WeaponStatTarget.CriticalDamage );

        return damage * attackRate * (1 + (critChance / 100f) * (critDamage - 1));
    }
}