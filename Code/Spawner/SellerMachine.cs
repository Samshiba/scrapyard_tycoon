using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class SellerMachine : Component, Component.ITriggerListener
{
    private PlayerStats _linkedBank;

    [Property, Group( "Stats Tycoon" )] public float ProcessRateBase { get; set; } = 0.4f;
    [Property, Group( "Stats Tycoon" )] public float ValueMultiplierBase { get; set; } = 1.0f;
    [Property, Group( "Stats Tycoon" )] public int MaxQueueSizeBase { get; set; } = 10;

    public int MaxQueueSize
    {
        get
        {
            int total = MaxQueueSizeBase;

            if ( SaveManager.Instance?.Data?.Player?.GlobalUpgrades == null )
                return total;

            int upgradeLevel1 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "seller_queue_1", 0 );
            total += (upgradeLevel1 * 5);

            int upgradeLevel2 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "seller_queue_2", 0 );
            total += (upgradeLevel2 * 50);

            return total;
        }
    }

    public float ProcessRate
    {
        get
        {
            float total = ProcessRateBase;

            if ( SaveManager.Instance?.Data?.Player?.GlobalUpgrades == null )
                return total;

            int upgradeLevel1 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "seller_speed_1", 0 );
            total += (upgradeLevel1 * 0.1f);

            int upgradeLevel2 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "seller_speed_2", 0 );
            total += (upgradeLevel2 * 0.5f);

            return total;
        }
    }

    public float ValueMultiplier
    {
        get
        {
            float total = ValueMultiplierBase;

            if ( SaveManager.Instance?.Data?.Player?.GlobalUpgrades == null )
                return total;

            int upgradeLevel1 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "seller_value_1", 0 );
            total += (upgradeLevel1 * 0.05f);

            int upgradeLevel2 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "seller_value_2", 0 );
            total += (upgradeLevel2 * 0.25f);

            int upgradeLevel3 = SaveManager.Instance.Data.Player.GlobalUpgrades.GetValueOrDefault( "seller_value_3", 0 );
            total += (upgradeLevel3 * 1.0f);

            return total;
        }
    }

    public Queue<ItemData> ProcessingQueue { get; private set; } = new();
    private TimeSince _timeSinceLastProcess;

    public void OnTriggerEnter( Collider other )
    {
        // --- CAS 1 : Le Joueur rentre dans la zone pour vider son sac ---
        var backpack = other.GameObject.Components.GetInAncestorsOrSelf<PlayerBackpack>();
        if ( backpack != null )
        {
            int itemsTransferred = 0;

            // On transfère le contenu du sac vers la machine
            while ( backpack.CollectedItems.Count > 0 && ProcessingQueue.Count < MaxQueueSize )
            {
                // On prend le dernier objet du sac
                var item = backpack.CollectedItems[^1];
                backpack.CollectedItems.RemoveAt( backpack.CollectedItems.Count - 1 );

                // On le met dans la file d'attente de la machine
                ProcessingQueue.Enqueue( item );
                itemsTransferred++;
            }

            if ( itemsTransferred > 0 )
            {
                Log.Info( $"[SellerMachine] Backpack emptied: {itemsTransferred} items transferred to machine" );
            }
            if ( backpack.CollectedItems.Count > 0 )
            {
                Log.Warning( "[SellerMachine] WARNING: Processing queue is full. Some items remain in backpack." );
            }
            return; // On a géré le joueur, on s'arrête là.
        }

        // --- CAS 2 : Un déchet physique tombe dans la zone (pour plus tard avec les tapis roulants) ---
        var worldItem = other.GameObject.Components.GetInAncestorsOrSelf<IWorldItem>();
        if ( worldItem != null )
        {
            if ( ProcessingQueue.Count < MaxQueueSize )
            {
                ProcessingQueue.Enqueue( worldItem.GetItemData() );
                worldItem.Consume(); // Le déchet se détruit
            }
        }
    }

    public void OnTriggerExit( Collider other ) { }

    protected override void OnUpdate()
    {
        // RECHERCHE DYNAMIQUE DU JOUEUR (S'il n'est pas encore trouvé)
        if ( _linkedBank == null )
        {
            // On fouille la scène pour trouver le composant PlayerStats (parfait pour le runtime)
            _linkedBank = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();

            // Si le joueur n'est pas encore spawn, on attend la prochaine frame
            if ( _linkedBank == null ) return;
        }

        // La logique Tycoon
        if ( ProcessingQueue.Count == 0 ) return;

        if ( _timeSinceLastProcess >= (1f / ProcessRate) )
        {
            _timeSinceLastProcess = 0;

            ItemData item = ProcessingQueue.Dequeue();
            float finalValue = item.Value * ValueMultiplier;

            _linkedBank.AddScrap( finalValue );
            Log.Info( $"[SellerMachine] Processing complete: +{finalValue} scrap earned" );
            
            // FIXED: Only save when queue actually changed (item processed)
            // instead of checking every frame with SaveChanges()
            if ( ProcessingQueue.Count > 0 || ProcessingQueue.Count == 0 )
            {
                // Queue state changed - notify throttler for batched save
                SaveEventBus.NotifyChange( SaveEventBus.SaveReason.SellerQueueChanged, $"Processed item: Queue now {ProcessingQueue.Count} items" );
            }
        }
    }

    private void SaveChanges()
    {
        if ( !IsProxy && SaveManager.Instance != null )
        {
            SaveManager.Instance.Data.Factory.SellerQueue = new System.Collections.Generic.List<ItemData>( ProcessingQueue );
            
            // Notify throttler (queue changes typically throttled)
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.SellerQueueChanged, $"Queue: {ProcessingQueue.Count} items" );
        }
    }

    protected override void OnAwake()
    {
        if ( !IsProxy && SaveManager.Instance?.Data?.Factory != null )
        {
            ProcessingQueue = new Queue<ItemData>( SaveManager.Instance.Data.Factory.SellerQueue );
            Log.Info( $"[SellerMachine] Loaded {ProcessingQueue.Count} item(s) in processing queue" );
        }
    }
}