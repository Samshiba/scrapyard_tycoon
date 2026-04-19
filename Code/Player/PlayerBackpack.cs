using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class PlayerBackpack : Component, Component.INetworkListener
{
    public static PlayerBackpack Local { get; private set; }
    [Property, Group( "Stats" )] public int BaseMaxItems { get; set; } = 10;

    [Sync]
    [Property, Group( "Items" )]
    public NetList<ItemData> CollectedItems { get; set; } = new();

    private string MySteamId => Network.Owner.SteamId.ToString();

    public int MaxItems
    {
        get
        {
            return (int)GlobalUpgradesSystem.Instance.ApplyModifiers( "backpack_capacity", BaseMaxItems );
        }
    }

    protected override void OnAwake()
    {
        if ( !IsProxy )
        {
            Local = this;
        }
    }

    protected override void OnStart()
    {
        if ( !Networking.IsHost ) return;

        if ( SaveManager.Instance?.ActivePlayers.TryGetValue( MySteamId, out var playerData ) == true )
        {
            CollectedItems.Clear();

            foreach ( var stack in playerData.CollectedItems )
            {
                for ( int i = 0; i < stack.Count; i++ )
                {
                    CollectedItems.Add( new ItemData { Type = stack.Type, Value = stack.Value } );
                }
            }
            Log.Info( $"[PlayerBackpack] Backpack loaded with {CollectedItems.Count} items" );
        }
    }

    [Rpc.Broadcast]
    public void RpcTryAddItem( ResourceType type, float value )
    {
        if ( !Networking.IsHost ) return;

        if ( CollectedItems.Count >= MaxItems ) return;

        CollectedItems.Add( new ItemData { Type = type, Value = value } );
        Log.Info( $"[PlayerBackpack] Item collected: {type} (+{value}). Inventory: {CollectedItems.Count}/{MaxItems}" );
        SaveChanges();
    }

    public int CurrentItemCount => CollectedItems.Count;
    public float TotalValue => CollectedItems.Sum( x => x.Value );
    public bool IsFull => CurrentItemCount >= MaxItems;

    public bool TryAddItem( ItemData item )
    {
        if ( CollectedItems.Count >= MaxItems ) return false;

        CollectedItems.Add( item );
        Log.Info( $"[PlayerBackpack] Item collected: {item.Type} (+{item.Value}). Inventory: {CollectedItems.Count}/{MaxItems}" );
        SaveChanges();
        return true;
    }

    public void SaveChanges()
    {
        if ( !Networking.IsHost || SaveManager.Instance == null ) return;

        if ( SaveManager.Instance.ActivePlayers.TryGetValue( MySteamId, out var playerData ) )
        {
            playerData.CollectedItems = CollectedItems
            .GroupBy( item => new { item.Type, item.Value } )
            .Select( group => new ItemStack
            {
                Type = group.Key.Type,
                Value = group.Key.Value,
                Count = group.Count()
            } ).ToList();

            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.InventoryChanged, "Inventory updated", MySteamId );
        }
    }
}