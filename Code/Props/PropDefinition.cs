using Sandbox;
using System;
using System.Collections.Generic;

[AssetType( Name = "Prop Definition", Extension = "prop", Category = "ScrapYard" )]
public partial class PropDefinition : GameResource
{
    [Property, Group( "Identity" )] public string PropID { get; set; } = "Item";
    [Property, Group( "Identity" )] public int Tier { get; set; } = 1;
    [Property, Group( "Identity" )] public Model Model { get; set; }
    [Property, Group( "Identity" )] public List<ResourceType> Types { get; set; } = new();

    [Property, Group( "Stats" ), Range( 1, 10 )] public int RarityMod { get; set; } = 1;
    [Property, Group( "Stats" )] public bool IsJackpot { get; set; } = false;

    [Property, Group( "Sub-Destruction" )] public List<PropDrop> SubProps { get; set; } = new();

    [Property, Group( "Stats" )]
    public int CalculatedSpawnWeight
    {
        get
        {
            return (int)(512 / Math.Pow( 2, RarityMod - 1 ));
        }
    }
}