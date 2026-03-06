using Sandbox;

// On hérite de Component, on écoute IPressable (pour le joueur), ET on signe IWorldItem
public sealed class ResourceGib : Component, Component.IPressable, IWorldItem
{
    [Property] public ResourceType Type { get; set; } = ResourceType.Scrap;
    [Property] public float Value { get; set; }
    [Property] public Vector3 LaunchVelocity { get; set; }
    [Property] public Vector3 LaunchAngularVelocity { get; set; }

    private bool _launched = false;

    public ItemData GetItemData()
    {
        return new ItemData { Type = this.Type, Value = this.Value };
    }

    public void Consume()
    {
        GameObject.Destroy();
    }

    public void Initialize(ResourceType type, float value, Vector3 randomDir)
    {
        Type = type;
        Value = value;
        RandomizeLaunch(randomDir);
    }

    private void RandomizeLaunch(Vector3 randomDir)
    {
        LaunchVelocity = randomDir * Game.Random.Float(150f, 350f) + Vector3.Up * Game.Random.Float(80f, 220f);
        LaunchAngularVelocity = new Vector3(
            Game.Random.Float(-5f, 5f),
            Game.Random.Float(-5f, 5f),
            Game.Random.Float(-5f, 5f)
        );
    }

    protected override void OnFixedUpdate()
    {
        if (_launched) return;
        _launched = true;

        if (LaunchVelocity == Vector3.Zero) return;

        var rb = Components.Get<Rigidbody>();
        if (rb == null) return;

        rb.Velocity = LaunchVelocity;
        rb.AngularVelocity = LaunchAngularVelocity;
    }

    public bool Press(IPressable.Event e)
    {
        var backpack = e.Source?.Components.Get<PlayerBackpack>();

        if (backpack != null && backpack.TryAddItem(GetItemData()))
        {
            Consume();
            return true;
        }
        return false;
    }

    public bool CanPress(IPressable.Event e) => true;
    public void Release(IPressable.Event e) { }
    public System.Nullable<IPressable.Tooltip> GetTooltip(IPressable.Event e)
    {
        return new IPressable.Tooltip
        {
            Title = "Scrap Gib",
            Description = "collect scrap",
            Icon = "scrap_icon"
        };
    }
}