using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sandbox;

public enum UpgradeType
{
    SkillTree,
    Prestige,
}

public enum UpgradeValueType
{
    Additive,
    Multiplicative,
    Reductive,
    CompoundMultiplicative,
    Conversion,
}

[AssetType( Name = "Upgrade Definition", Extension = "upgrade", Category = "ScrapYard" )]
public class UpgradeDefinition : GameResource
{
    [Property, Group( "Identity" )] public string Id { get; set; }
    [Property, Group( "Identity" )] public string Icon { get; set; } = "⚡";
    [Property, Group( "Identity" )] public UpgradeType Type { get; set; }

    [Property, Group( "Economy" )] public int MaxLevel { get; set; } = 5;
    [Property, Group( "Economy" )] public double BaseCost { get; set; }
    [Property, Group( "Economy" )] public float CostMultiplier { get; set; }

    [Property, Group( "Effect" )] public string StatModified { get; set; }
    [Property, Group( "Effect" )] public float EffectValuePerLevel { get; set; }
    [Property, Group( "Effect" )] public UpgradeValueType ValueType { get; set; }
    [Property, Group( "Effect" )] public string StatSource { get; set; }

    [Property, Group( "Requirements" )] public List<UpgradeRequirement> RequiredUpgrades { get; set; }

    public double GetCostForLevel( int currentLevel ) =>
        Math.Round( BaseCost * Math.Pow( CostMultiplier, currentLevel ) );
}

public class UpgradeRequirement
{
    [Property] public UpgradeDefinition RequiredUpgrade { get; set; }
    [Property, Group( "Requirements" )] public int RequiredLevel { get; set; }
}