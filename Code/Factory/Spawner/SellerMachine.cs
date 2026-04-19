using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

public sealed class SellerMachine : Component, Component.ITriggerListener
{
    public static SellerMachine Instance { get; private set; }
    [Property, Group( "Stats Tycoon" )] public float ProcessRateBase { get; set; } = 0.4f;
    [Property, Group( "Stats Tycoon" )] public float ValueMultiplierBase { get; set; } = 1.0f;
    [Property, Group( "Stats Tycoon" )] public int MaxQueueSizeBase { get; set; } = 10;

    private bool _isLoaded = false;

    protected override void OnAwake()
    {
        Instance = this;
    }

    public int MaxQueueSize
    {
        get
        {
            return (int)GlobalUpgradesSystem.Instance.ApplyModifiers( "seller_queue", MaxQueueSizeBase );
        }
    }

    public float ProcessRate
    {
        get
        {
            return GlobalUpgradesSystem.Instance.ApplyModifiers( "seller_speed", ProcessRateBase );
        }
    }

    public float ValueMultiplier
    {
        get
        {
            return GlobalUpgradesSystem.Instance.ApplyModifiers( "seller_value", ValueMultiplierBase );
        }
    }

    public Queue<ItemData> ProcessingQueue { get; private set; } = new();
    private TimeSince _timeSinceLastProcess;

    public void OnTriggerEnter( Collider other )
    {
        if ( !Networking.IsHost ) return;

        // --- CAS 1 : Le Joueur rentre dans la zone pour vider son sac ---
        var backpack = other.GameObject.Components.GetInAncestorsOrSelf<PlayerBackpack>();
        if ( backpack != null )
        {
            int itemsTransferred = 0;
            string playerSteamId = backpack.Network.Owner?.SteamId.ToString() ?? "unknown";
            double scrapGained = 0;

            while ( backpack.CollectedItems.Count > 0 && ProcessingQueue.Count < MaxQueueSize )
            {
                var item = backpack.CollectedItems[^1];
                backpack.CollectedItems.RemoveAt( backpack.CollectedItems.Count - 1 );

                ProcessingQueue.Enqueue( item );
                scrapGained += item.Value;
                itemsTransferred++;
            }

            if ( itemsTransferred > 0 )
            {
                backpack.SaveChanges();
                GameStats.OnScrapGained( playerSteamId, scrapGained );
                Log.Info( $"[SellerMachine] Backpack emptied: {itemsTransferred} items transferred to machine" );
            }
            return;
        }

        // --- CAS 2 : Un déchet physique tombe dans la zone (pour plus tard avec les tapis roulants) ---
        var worldItem = other.GameObject.Components.GetInAncestorsOrSelf<IWorldItem>();
        if ( worldItem != null )
        {
            if ( ProcessingQueue.Count < MaxQueueSize )
            {
                ProcessingQueue.Enqueue( worldItem.GetItemData() );
                worldItem.Consume();
            }
        }
    }

    public void OnTriggerExit( Collider other ) { }

    protected override void OnUpdate()
    {
        if ( !Networking.IsHost ) return;

        if ( !_isLoaded && SaveManager.Instance?.IsFactoryReady == true )
        {
            ProcessingQueue = new Queue<ItemData>();

            foreach ( var stack in SaveManager.Instance.CurrentFactory.SellerQueue )
            {
                for ( int i = 0; i < stack.Count; i++ )
                {
                    ProcessingQueue.Enqueue( new ItemData { Type = stack.Type, Value = stack.Value } );
                }
            }
            _isLoaded = true;
            Log.Info( $"[SellerMachine] Loaded {ProcessingQueue.Count} item(s) in processing queue" );
        }

        if ( !_isLoaded || ProcessingQueue.Count == 0 ) return;

        if ( _timeSinceLastProcess >= (1f / ProcessRate) )
        {
            _timeSinceLastProcess = 0;

            ItemData item = ProcessingQueue.Dequeue();
            float finalValue = item.Value * ValueMultiplier;

            FactoryStats.Instance.AddScrap( finalValue );

            if ( SaveManager.Instance != null )
            {
                SaveManager.Instance.CurrentFactory.SellerQueue = ProcessingQueue
                .GroupBy( item => new { item.Type, item.Value } )
                .Select( group => new ItemStack
                {
                    Type = group.Key.Type,
                    Value = group.Key.Value,
                    Count = group.Count()
                } ).ToList();
                SaveEventBus.NotifyChange( SaveEventBus.SaveReason.SellerQueueChanged );
            }
        }
    }
}