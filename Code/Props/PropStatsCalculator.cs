using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System;
using Sandbox.Utils;

public static class PropStatsCalculator
{
    /// <summary>
    /// Calculate HP for a prop based on tier and rarity
    /// Formula: (base_hp * (hp_mult^(tier-1))) * rarity_mod
    /// </summary>
    public static float GetHealth( PropDefinition prop )
    {
        if ( prop == null ) return 0;

        var config = BalanceConfig.Instance;
        if ( config == null ) return 0;

        float rarityHPMod = 1f + (prop.RarityMod - 1f) * 0.75f;

        float rawHP = config.BaseHP * MathF.Pow( config.HPMult, prop.Tier - 1 ) * rarityHPMod;
        return MathF.Round( rawHP, 2 );
    }

    /// <summary>
    /// Calculate raw value for a single prop (before SubProps consideration)
    /// Formula: (base_value * (value_mult^(tier-1))) * rarity_mod * jackpot_bonus
    /// </summary>
    public static float GetValue( PropDefinition prop )
    {
        if ( prop == null ) return 0;

        var config = BalanceConfig.Instance;
        if ( config == null ) return 0;

        float finalJackpotMultiplier = 1.0f;
        if ( prop.IsJackpot )
        {
            finalJackpotMultiplier = GlobalUpgradesSystem.Instance.ApplyModifiers( "jackpot_bonus", config.JackpotBonus );
        }

        float rawValue = config.BaseValue * MathF.Pow( config.ValueMult, prop.Tier - 1 ) * prop.RarityMod * finalJackpotMultiplier;
        return MathF.Round( rawValue, 2 );
    }

    /// <summary>
    /// Calculate gib count for a prop
    /// Formula: base_gibs + ((tier-1) * gibs_per_tier), clamped at max_gibs
    /// </summary>
    public static int GetGibCount( PropDefinition prop )
    {
        if ( prop == null ) return 0;

        var config = BalanceConfig.Instance;
        if ( config == null ) return 0;

        int desiredGibs = config.BaseGibs + ((prop.Tier - 1) * config.GibsPerTier);
        return Math.Clamp( desiredGibs, 0, config.MaxGibs );
    }

    /// <summary>
    /// Calculate value per gib
    /// Formula: TotalValue / FinalGibCount
    /// </summary>
    public static float GetValuePerGib( PropDefinition prop )
    {
        if ( prop == null ) return 0;

        float totalValue = GetValue( prop );
        int gibCount = GetGibCount( prop );

        return gibCount > 0 ? MathF.Round( totalValue / gibCount, 2 ) : totalValue;
    }

    /// <summary>
    /// Calculate total value including SubProps recursively
    /// This includes the value from breaking into gibs + value from SubProps
    /// </summary>
    public static float GetTotalValueWithSubProps( PropDefinition prop )
    {
        return GetTotalValueWithSubPropsInternal( prop, 0 );
    }

    private static float GetTotalValueWithSubPropsInternal( PropDefinition prop, int depth )
    {
        const int MaxDepth = 10; // Prevent infinite recursion

        if ( prop == null ) return 0;
        if ( depth > MaxDepth ) return GetValue( prop ); // Fall back to base value if too deep

        float totalValue = 0f;

        // If has SubProps, calculate their value recursively
        if ( prop.SubProps != null && prop.SubProps.Count > 0 )
        {
            foreach ( var drop in prop.SubProps )
            {
                if ( drop.Prop == null ) continue;
                totalValue += GetTotalValueWithSubPropsInternal( drop.Prop, depth + 1 ) * drop.Count;
            }
        }
        else
        {
            // Otherwise, this prop breaks into gibs
            totalValue = GetValue( prop );
        }

        return totalValue;
    }

    /// <summary>
    /// Apply money multiplier to a value
    /// Can be overridden per weapon type or seller upgrades
    /// </summary>
    public static float ApplyMoneyMultiplier( float baseValue, float multiplier = 1.0f )
    {
        return baseValue * multiplier;
    }

    /// <summary>
    /// Get the current seller value multiplier from upgrades
    /// </summary>
    public static float GetSellerValueMultiplier()
    {
        var sellerMachine = SellerMachine.Instance;
        if ( sellerMachine != null )
            return sellerMachine.ValueMultiplier;

        return 1.0f;
    }
}
