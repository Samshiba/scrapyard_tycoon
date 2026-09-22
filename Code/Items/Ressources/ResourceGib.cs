using Sandbox;

public sealed class ResourceGib : Component, Component.IPressable, IWorldItem
{
    [Property] public ItemData ItemData { get; private set; }
    [Property] public Vector3 LaunchVelocity { get; set; }
    [Property] public Vector3 LaunchAngularVelocity { get; set; }

    private bool _launched = false;
    private TemporaryEffect _despawnEffect;

    protected override void OnAwake()
    {
        GibsManager.Instance?.RegisterGib( this );

        if ( GameSettings.Instance != null )
        {
            GameSettings.Instance.OnSettingsChanged += OnSettingsChanged;
        }
    }

    protected override void OnDestroy()
    {
        GibsManager.Instance?.UnregisterGib( this );

        if ( GameSettings.Instance != null )
        {
            GameSettings.Instance.OnSettingsChanged -= OnSettingsChanged;
        }
    }

    public ItemData GetItemData()
    {
        return ItemData;
    }

    public void Consume()
    {
        GameObject.Destroy();
    }

    public void Initialize( ResourceType type, float value, Vector3 randomDir )
    {
        RandomizeLaunch( randomDir );
        ItemData = new ItemData
        {
            Type = type,
            Value = value
        };

        ApplyGibSettings();
    }

    private void ApplyGibSettings()
    {
        // Apply auto-despawn if enabled in settings
        if ( GameSettings.Instance?.Performance.EnableGibsAutoDespawn ?? false )
        {
            // Remove old effect if any
            if ( _despawnEffect != null )
                _despawnEffect.Destroy();

            _despawnEffect = GameObject.AddComponent<TemporaryEffect>();
            _despawnEffect.DestroyAfterSeconds = GameSettings.Instance.Performance.GibDespawnTime;
        }
        else
        {
            // Remove despawn effect if disabled
            if ( _despawnEffect != null )
            {
                _despawnEffect.Destroy();
                _despawnEffect = null;
            }
        }

        // Apply shadows setting
        var modelRenderer = Components.Get<ModelRenderer>();
        if ( modelRenderer != null )
        {
            modelRenderer.RenderType = GameSettings.Instance?.Performance.EnableGibShadows ?? true ? ModelRenderer.ShadowRenderType.On : ModelRenderer.ShadowRenderType.Off;
        }
    }

    private void OnSettingsChanged()
    {
        ApplyGibSettings();
    }

    private void RandomizeLaunch( Vector3 randomDir )
    {
        LaunchVelocity = randomDir * Game.Random.Float( 150f, 350f ) + Vector3.Up * Game.Random.Float( 80f, 220f );
        LaunchAngularVelocity = new Vector3(
            Game.Random.Float( -5f, 5f ),
            Game.Random.Float( -5f, 5f ),
            Game.Random.Float( -5f, 5f )
        );
    }

    protected override void OnFixedUpdate()
    {
        if ( _launched ) return;
        _launched = true;

        if ( LaunchVelocity == Vector3.Zero ) return;

        var rb = Components.Get<Rigidbody>();
        if ( rb == null ) return;

        rb.Velocity = LaunchVelocity;
        rb.AngularVelocity = LaunchAngularVelocity;
    }

    public bool Press( IPressable.Event e )
    {
        var backpack = e.Source?.Components.Get<PlayerBackpack>();

        if ( backpack != null && backpack.CollectedItems.Count < backpack.MaxItems )
        {
            var data = GetItemData();
            backpack.RpcTryAddItem( data.Type, data.Value );

            Consume();
            return true;
        }
        if ( backpack != null && backpack.CollectedItems.Count >= backpack.MaxItems && GameStats.CanSendToSbox( Scene ) )
        {
            Sandbox.Services.Achievements.Unlock( "scrt_hoarder" );
        }
        return false;
    }

    public bool CanPress( IPressable.Event e ) => true;
    public void Release( IPressable.Event e ) { }
    public System.Nullable<IPressable.Tooltip> GetTooltip( IPressable.Event e )
    {
        return new IPressable.Tooltip
        {
            Description = "#tooltip.gib.description",
        };
    }
}