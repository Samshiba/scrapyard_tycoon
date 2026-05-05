using Sandbox;
using System.Collections.Generic;

public sealed class ImpactProjectile : BaseProjectile
{
    [Property, Group( "Explosion" )] public float ExplosionForce { get; set; } = 25000f;
    [Property, Group( "Explosion" )] public float ExplosionRadius { get; set; } = 200f;
    [Property, Group( "Explosion" )] public GameObject ExplosionEffectPrefab { get; set; }
    [Property, Group( "Explosion" )] public SoundEvent ExplosionSound { get; set; }

    protected override void ProcessMovementAndCollision()
    {
        var startPos = WorldPosition;
        var endPos = startPos + WorldRotation.Forward * Speed * Time.Delta;

        var tr = Scene.Trace.Ray( startPos, endPos )
            .IgnoreGameObjectHierarchy( Shooter )
            .IgnoreGameObjectHierarchy( GameObject )
            .WithoutTags( "player" )
            .UsePhysicsWorld()
            .Run();

        if ( tr.Hit )
        {
            WorldPosition = tr.HitPosition;
            Explode();
        }
        else
        {
            WorldPosition = endPos;
        }
    }

    private void Explode()
    {
        if ( ExplosionSound != null )
        {
            Sound.Play( ExplosionSound, WorldPosition );
        }

        if ( ExplosionEffectPrefab != null )
        {
            var fx = ExplosionEffectPrefab.Clone( WorldPosition );
        }


        var overlaps = Scene.Trace.Sphere( ExplosionRadius, WorldPosition, WorldPosition )
            .UsePhysicsWorld()
            .RunAll();

        var hitTargets = new HashSet<GameObject>();

        foreach ( var hit in overlaps )
        {
            if ( hit.GameObject == null || hit.GameObject == Shooter ) continue;

            if ( hitTargets.Contains( hit.GameObject ) ) continue;
            hitTargets.Add( hit.GameObject );

            // --- 1. La Ligne de Vue (Line of Sight) ---
            Vector3 targetCenter = hit.GameObject.WorldPosition;
            Vector3 surfacePoint = targetCenter; // Par défaut, on garde le centre

            var surfaceTrace = Scene.Trace.Ray( WorldPosition, targetCenter )
                .IgnoreGameObjectHierarchy( Shooter )
                .UsePhysicsWorld()
                .Run();

            if ( surfaceTrace.Hit && surfaceTrace.GameObject == hit.GameObject )
            {
                surfacePoint = surfaceTrace.HitPosition;
            }

            // --- 2. Calcul du Falloff  ---
            float distance = Vector3.DistanceBetween( WorldPosition, surfacePoint );
            float falloff = 1f - (distance / ExplosionRadius);
            falloff = MathX.Clamp( falloff, 0.25f, 1f );

            // --- 3. Dégâts ---
            var health = hit.GameObject.Components.GetInAncestorsOrSelf<PropHealth>();
            if ( health != null )
            {
                double finalDamage = this.Damage * falloff;
                var damageInfo = new DamageInfo
                {
                    Damage = (float)finalDamage,
                    Position = WorldPosition
                };
                damageInfo.Tags.Add( "explosion" );
                health.OnDamage( damageInfo );

                // Track damage and attacker for stats
                var backpack = Shooter?.Components.Get<PlayerBackpack>();
                // var shooterSteamId = backpack?.Network.Owner?.SteamId.ToString() ?? "unknown";
                var shooterSteamId = backpack?.Network.Owner?.GetUniqueId() ?? backpack?.Network.Owner?.SteamId.ToString() ?? "unknown";
                health.OnDamageDealt( shooterSteamId, WeaponId, finalDamage, false );
            }

            // --- 4. Souffle Physique ---
            var rb = hit.GameObject.Components.GetInAncestorsOrSelf<Rigidbody>();
            if ( rb != null && rb.MotionEnabled )
            {
                Vector3 pushDir = (targetCenter - WorldPosition).Normal;
                pushDir += Vector3.Up * 0.5f;
                pushDir = pushDir.Normal;

                rb.ApplyImpulse( pushDir * ExplosionForce * falloff );
            }
        }

        GameObject.Destroy();
    }
}