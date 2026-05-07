using Sandbox;
using System.Collections.Generic;
using System.Linq;

public class AreaStreamDelivery : IWeaponDelivery
{
    public void Execute( IWeaponContext ctx )
    {
        var camera = ctx.Owner.Scene.Camera;
        var ray = camera.ScreenNormalToRay( 0.5f );

        var startPos = ray.Position;
        var direction = ray.Forward;

        float range = ctx.GetStat( WeaponStatTarget.Range );
        float radius = ctx.Data.StreamRadius > 0 ? ctx.Data.StreamRadius : 40f;

        var traces = ctx.Owner.Scene.Trace.Sphere( radius, startPos, startPos + direction * range )
            .IgnoreGameObjectHierarchy( ctx.Owner )
            .WithoutTags( "player" )
            .UsePhysicsWorld()
            .RunAll()
            .ToList();

        var hitTargets = new HashSet<GameObject>();

        foreach ( var tr in traces )
        {
            if ( !tr.Hit || tr.GameObject == null ) continue;

            var health = tr.GameObject.Components.GetInAncestorsOrSelf<PropHealth>();
            if ( health == null ) continue;

            if ( hitTargets.Contains( health.GameObject ) ) continue;
            hitTargets.Add( health.GameObject );

            var losTrace = ctx.Owner.Scene.Trace.Ray( startPos, tr.HitPosition )
                .IgnoreGameObjectHierarchy( ctx.Owner )
                .WithoutTags( "player" )
                .UsePhysicsWorld()
                .Run();

            if ( losTrace.Hit && losTrace.GameObject != tr.GameObject && losTrace.GameObject.Components.Get<PropHealth>() == null )
            {
                continue;
            }

            // Base Damage
            bool isCrit = ctx.RollCritical();

            float damage = ctx.GetStat( WeaponStatTarget.Damage );
            float baseDamage = isCrit ? damage * ctx.GetStat( WeaponStatTarget.CriticalDamage ) : damage;

            // Falloff Damage Reduction
            float distance = Vector3.DistanceBetween( startPos, tr.HitPosition );
            float maxRange = ctx.GetStat( WeaponStatTarget.Range );
            float distanceRatio = distance / maxRange;

            float falloffFactor = 1f;
            if ( distanceRatio > ctx.Data.FalloffStartRatio && ctx.Data.FalloffMinMultiplier < 1f )
            {
                float t = (distanceRatio - ctx.Data.FalloffStartRatio) / (1f - ctx.Data.FalloffStartRatio);
                falloffFactor = MathX.Lerp( 1f, ctx.Data.FalloffMinMultiplier, t );
            }

            // Final Damage
            float damageForThisTarget = baseDamage * falloffFactor;

            var damageInfo = new DamageInfo
            {
                Damage = damageForThisTarget,
                Position = tr.HitPosition
            };

            // Tags
            damageInfo.Tags.Add( "player" );
            damageInfo.Tags.Add( ctx.Data.Id );
            damageInfo.Tags.Add( "continuous" );
            damageInfo.Tags.Add( "ranged" );
            if ( isCrit ) damageInfo.Tags.Add( "critical" );

            health.OnDamage( damageInfo );

            // Stats
            string playerId = ctx.GetPlayerUniqueId();
            health.OnDamageDealt( playerId, ctx.Data.Id, baseDamage, isCrit );

            // Physics Push
            var rb = tr.GameObject.Components.GetInAncestorsOrSelf<Rigidbody>();
            if ( rb != null && rb.MotionEnabled )
            {
                float pushForce = damageForThisTarget * ctx.Data.ImpactForce;
                rb.ApplyImpulseAt( tr.HitPosition, direction * pushForce );
            }
        }
    }
}