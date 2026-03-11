using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class PlayerBackpack : Component
{
    [Property, Group("Stats")] public int MaxItems { get; set; } = 10;

    [Property, Group("Items")] public List<ItemData> CollectedItems { get; set; } = new();

    protected override void OnAwake()
    {
        if (!IsProxy && SaveManager.Instance?.Data?.Inventory != null)
        {
            CollectedItems = SaveManager.Instance.Data.Inventory.CollectedItems ?? new();
            Log.Info($"🎒 Backpack chargé : {CollectedItems.Count} items");
        }
    }

    public int CurrentItemCount => CollectedItems.Count;
    public float TotalValue => CollectedItems.Sum(x => x.Value);
    public bool IsFull => CurrentItemCount >= MaxItems;

    public bool TryAddItem(ItemData item)
    {
        if (CollectedItems.Count >= MaxItems) return false;

        CollectedItems.Add(item);
        Log.Info($"Ramassé : {item.Type} (+{item.Value}). Place : {CollectedItems.Count}/{MaxItems}");
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
        if (!IsProxy && SaveManager.Instance != null)
        {
            SaveManager.Instance.Data.Inventory.CollectedItems = CollectedItems;
            SaveManager.Instance.Save();
        }
    }
}