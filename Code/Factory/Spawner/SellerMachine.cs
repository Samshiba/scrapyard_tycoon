using Sandbox;
using Sandbox.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class SellerMachine : Component, Component.IPressable
{
    public static SellerMachine Instance { get; private set; }
    [Property, Group( "Stats Tycoon" )] public float ProcessRateBase { get; set; } = 0.4f;
    [Property, Group( "Stats Tycoon" )] public float ValueMultiplierBase { get; set; } = 1.0f;
    [Property, Group( "Stats Tycoon" )] public int MaxQueueSizeBase { get; set; } = 10;
    [Property, Group( "Visual" )] public Light SellerLight { get; set; }

    private bool _isLoaded = false;
    private Color _baseLightColor = (Color)Color.Parse( "#FEBA1B" );
    private Vector3 _baseLightPosition;

    protected override void OnAwake()
    {
        Instance = this;

        if ( SellerLight != null )
        {
            _baseLightColor = SellerLight.LightColor;
            _baseLightPosition = SellerLight.WorldPosition;
        }
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
    private TimeSince _timeSincePulseStart;
    private TimeSince _timeSinceLastFlicker;
    private bool _isPulsing = false;
    private const float PULSE_DURATION = 0.3f;
    private const float FLICKER_UPDATE_RATE = 0.1f; // Update flicker every 100ms

    public bool Press( IPressable.Event e )
    {
        if ( !Networking.IsHost ) return false;
        if ( e.Source == null ) return false;

        // Get the player's backpack
        var backpack = e.Source.GameObject.Components.GetInAncestorsOrSelf<PlayerBackpack>();
        if ( backpack == null ) return false;

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
            Log.Info( $"[SellerMachine] Items deposited: {itemsTransferred} items transferred to machine" );
            return true;
        }

        return false;
    }

    public bool CanPress( IPressable.Event e ) => true;
    public void Release( IPressable.Event e ) { }

    public System.Nullable<IPressable.Tooltip> GetTooltip( IPressable.Event e )
    {
        return new IPressable.Tooltip
        {
            Description = "#tooltip.seller.description",
        };
    }

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

        // Update light flickering
        UpdateLightFlicker();

        if ( !_isLoaded || ProcessingQueue.Count == 0 ) return;

        if ( _timeSinceLastProcess >= (1f / ProcessRate) )
        {
            _timeSinceLastProcess = 0;

            ItemData item = ProcessingQueue.Dequeue();
            float finalValue = item.Value * ValueMultiplier;

            FactoryStats.Instance.AddScrap( finalValue );

            // Trigger light pulse on sell
            _isPulsing = true;
            _timeSincePulseStart = 0;

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

    private void UpdateLightFlicker()
    {
        if ( SellerLight == null ) return;

        // Turn off light if queue is empty
        if ( ProcessingQueue.Count == 0 )
        {
            SellerLight.LightColor = Color.Black;
            SellerLight.WorldPosition = _baseLightPosition;
            return;
        }

        // Only update flicker at intervals to slow it down
        if ( _timeSinceLastFlicker < FLICKER_UPDATE_RATE ) return;
        _timeSinceLastFlicker = 0;

        // Compute flicker with random chaotic values
        float flickerStrength = Sandbox.Game.Random.Next( 60, 140 ) / 100f; // Range: 0.6 to 1.4

        // If pulsing, amplify the effect
        if ( _isPulsing )
        {
            float pulseProgress = _timeSincePulseStart / PULSE_DURATION;
            if ( pulseProgress >= 1f )
            {
                _isPulsing = false;
            }
            else
            {
                float pulseIntensity = MathX.Lerp( 2.5f, 1f, pulseProgress );
                flickerStrength *= pulseIntensity;
            }
        }

        // Random chaotic movement for flickering shadows
        float offsetX = (Sandbox.Game.Random.Next( -15, 15 )) * 0.05f;
        float offsetY = (Sandbox.Game.Random.Next( -15, 15 )) * 0.05f;
        float offsetZ = (Sandbox.Game.Random.Next( -10, 10 )) * 0.05f;

        Vector3 offset = new Vector3( offsetX, offsetY, offsetZ );
        SellerLight.WorldPosition = _baseLightPosition + offset;

        // Vary color chaotically with strong HDR
        float colorFlicker = (Sandbox.Game.Random.Next( 0, 200 ) - 100) / 100f; // Range: -1 to 1
        Color finalColor = _baseLightColor * (flickerStrength * (1f + colorFlicker * 0.5f));
        SellerLight.LightColor = finalColor;
    }
}