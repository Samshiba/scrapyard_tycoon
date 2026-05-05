using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class BayComponent : Component
{
    [Property] public int BayId { get; set; }
    public List<Connection> Owners { get; private set; } = new();
    public bool IsOccupied => Owners.Count > 0;

    [Property] public GameObject SpawnPoint { get; set; }
    [Property] public GameObject HopperArea { get; set; }
    [Property] public GameObject PlayerStart { get; set; }

    public void AssignOwner( Connection channel )
    {
        if ( !Owners.Contains( channel ) )
        {
            Owners.Add( channel );
            Log.Info( $"[BayComponent] Bay {BayId} assigned to player {channel.DisplayName} ({Owners.Count} player(s) total)" );
        }

        // TODO: Apply visual upgrades from SaveManager.Get( Scene ).CurrentFactory.UnlockedPrestigeUpgrades    
    }

    public void RemoveOwner( Connection channel )
    {
        if ( Owners.Contains( channel ) )
        {
            Owners.Remove( channel );
            Log.Info( $"[BayComponent] Player {channel.DisplayName} removed from Bay {BayId} ({Owners.Count} player(s) remaining)" );
        }

        if ( Owners.Count == 0 )
        {
            Log.Info( $"[BayComponent] Bay {BayId} cleared." );
        }
    }
}