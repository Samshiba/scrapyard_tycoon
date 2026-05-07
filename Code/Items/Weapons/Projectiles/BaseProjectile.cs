using Sandbox;
using System.Collections.Generic;

public abstract class BaseProjectile : Component
{
    // --- Data & References ---
    public float Damage { get; protected set; }
    public GameObject Shooter { get; protected set; }
    public string WeaponId { get; protected set; }
    public int PierceCount { get; protected set; }
    public string ShooterId { get; protected set; }
    protected HashSet<GameObject> HitTargets = new HashSet<GameObject>();

    [Property, Group( "Base Stats" )] public float Speed { get; set; } = 1000f;
    [Property, Group( "Base Stats" )] public float MaxLifeTime { get; set; } = 5f;

    protected TimeSince TimeSinceFired;

    // --- Lifecycle ---

    public virtual void Initialize( float damage, GameObject shooter, string weaponId = "", int pierceCount = 0, string shooterId = "unknown" )
    {
        Damage = damage;
        Shooter = shooter;
        WeaponId = weaponId;
        PierceCount = pierceCount;
        ShooterId = shooterId;
        TimeSinceFired = 0;
        HitTargets.Clear();
    }

    protected override void OnUpdate()
    {
        if ( TimeSinceFired > MaxLifeTime )
        {
            GameObject.Destroy();
            return;
        }

        ProcessMovementAndCollision();
    }

    // --- Abstract Methods ---
    protected abstract void ProcessMovementAndCollision();
}