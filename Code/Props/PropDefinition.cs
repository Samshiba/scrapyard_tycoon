using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;

[AssetType( Name = "Prop Definition", Extension = "prop", Category = "ScrapYard" )]
public partial class PropDefinition : GameResource
{
    [Property, Group( "Identity" )]
    public string PropID
    {
        get
        {
            return Path.GetFileNameWithoutExtension( ResourcePath ?? "Item" );
        }
    }

    [Property, Group( "Identity" )]
    public string DisplayName
    {
        get
        {
            return FormatName( PropID );
        }
    }

    private string FormatName( string propID )
    {
        // Ex: "prop_metal_bar_01" → "Metal Bar"
        var parts = propID.Split( '_' );
        if ( parts.Length < 2 ) return propID;

        var nameParts = new List<string>();
        for ( int i = 0; i < parts.Length; i++ )
        {
            if ( int.TryParse( parts[i], out _ ) ) continue; // Ignore les parties numériques à la fin
            nameParts.Add( parts[i] );
        }

        for ( int i = 0; i < nameParts.Count; i++ )
        {
            nameParts[i] = char.ToUpper( nameParts[i][0] ) + nameParts[i].Substring( 1 ); // Capitalize
        }

        return string.Join( " ", nameParts );
    }


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

    [Group( "Identity" )]
    public string IconPath
    {
        get => $"Props/Tier{Tier}/Textures/{PropID}.vmdl.png";
    }
}