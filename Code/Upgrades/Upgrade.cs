using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class UpgradeGraphData
{
    [JsonPropertyName( "nodes" )]
    public List<UpgradeNode> Nodes { get; set; } = new();
}

public class UpgradeNode
{
    [JsonPropertyName( "id" )] public string Id { get; set; }
    [JsonPropertyName( "name" )] public string Name { get; set; }
    [JsonPropertyName( "description" )] public string Description { get; set; }
    [JsonPropertyName( "icon" )] public string Icon { get; set; } = "⚡";
    [JsonPropertyName( "maxLevel" )] public int MaxLevel { get; set; }
    [JsonPropertyName( "baseCost" )] public double BaseCost { get; set; }
    [JsonPropertyName( "costMultiplier" )] public double CostMultiplier { get; set; }

    [JsonPropertyName( "requirements" )]
    public List<UpgradeRequirement> Requirements { get; set; } = new();

    public double GetCostForLevel( int currentLevel ) =>
        Math.Round( BaseCost * Math.Pow( CostMultiplier, currentLevel ) );
}

public class UpgradeRequirement
{
    [JsonPropertyName( "requiredId" )] public string RequiredId { get; set; }
    [JsonPropertyName( "requiredLevel" )] public int RequiredLevel { get; set; }
}