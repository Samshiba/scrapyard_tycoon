using Sandbox;

public abstract class BaseProjectile : Component
{
    // --- Data & References ---

    public float Damage { get; protected set; }
    public GameObject Shooter { get; protected set; }

    [Property, Group("Base Stats")] public float Speed { get; set; } = 1000f;
    [Property, Group("Base Stats")] public float MaxLifeTime { get; set; } = 5f;

    protected TimeSince TimeSinceFired;

    // --- Lifecycle ---

    public virtual void Initialize(float damage, GameObject shooter)
    {
        Damage = damage;
        Shooter = shooter;
        TimeSinceFired = 0;
    }

    protected override void OnUpdate()
    {
        if (TimeSinceFired > MaxLifeTime)
        {
            GameObject.Destroy();
            return;
        }

        ProcessMovementAndCollision();
    }

    // --- Abstract Methods ---
    
    protected abstract void ProcessMovementAndCollision();
}