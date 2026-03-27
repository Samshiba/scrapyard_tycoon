using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class PlayerBackpack : Component
{
    [Property, Group( "Stats" )] public int BaseMaxItems { get; set; } = 10;

    [Property, Group( "Items" )] public List<ItemData> CollectedItems { get; set; } = new();

    public int MaxItems
    {
        get
        {
            int total = BaseMaxItems;

            int upgradeLevel1 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "backpack_capacity_1", 0 );
            total += (upgradeLevel1 * 5);

            int upgradeLevel2 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "backpack_capacity_2", 0 );
            total += (upgradeLevel2 * 50);

            int upgradeLevel3 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "backpack_capacity_3", 0 );
            total += (upgradeLevel3 * 500);

            int upgradeLevel4 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "backpack_capacity_4", 0 );
            total += (upgradeLevel4 * 5000);

            return total;
        }
    }

    protected override void OnAwake()
    {
        if ( !IsProxy && SaveManager.Instance?.Data?.Inventory != null )
        {
            CollectedItems = SaveManager.Instance.Data.Inventory.CollectedItems ?? new();
            Log.Info( $"[PlayerBackpack] Backpack loaded with {CollectedItems.Count} items" );
        }
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

    public float EmptyBackpack()
    {
        float amount = TotalValue;
        CollectedItems.Clear();
        SaveChanges();
        return amount;
    }

    private void SaveChanges()
    {
        if ( !IsProxy && SaveManager.Instance != null )
        {
            SaveManager.Instance.Data.Inventory.CollectedItems = CollectedItems;
            SaveManager.Instance.Save();
        }
    }
}