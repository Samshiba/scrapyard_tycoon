using Sandbox;
using System.Linq;

public class MeleeSweepDelivery : IWeaponDelivery
{
    public void Execute( IWeaponContext ctx )
    {
        float range = ctx.GetStat( WeaponStatTarget.Range );
        float radius = ctx.Data.SweepRadius;

        var camera = ctx.Owner.Scene.Camera;
        var ray = camera.ScreenNormalToRay( 0.5f );

        var startPos = ray.Position;
        var direction = ray.Forward;

        var traces = ctx.Owner.Scene.Trace.Sphere( radius, startPos, startPos + direction * range )
            .IgnoreGameObjectHierarchy( ctx.Owner )
            .WithoutTags( "player" )
            .UsePhysicsWorld()
            .RunAll()
            .OrderBy( t => t.Distance )
            .ToList();

        int hitCount = 0;
        int maxHits = ctx.Data.PierceCount + 1;

        foreach ( var tr in traces )
        {
            if ( !tr.Hit || tr.GameObject == null ) continue;

            var health = tr.GameObject.Components.GetInAncestorsOrSelf<PropHealth>();

            if ( health != null )
            {
                bool isCrit = ctx.RollCritical();
                float damage = ctx.GetStat( WeaponStatTarget.Damage );
                float finalDamage = isCrit ? damage * ctx.GetStat( WeaponStatTarget.CriticalDamage ) : damage;

                var damageInfo = new DamageInfo
                {
                    Damage = finalDamage,
                    Position = tr.HitPosition
                };

                // Tags
                damageInfo.Tags.Add( "player" );
                damageInfo.Tags.Add( ctx.Data.Id );
                damageInfo.Tags.Add( "melee" );
                if ( isCrit ) damageInfo.Tags.Add( "critical" );

                health.OnDamage( damageInfo );

                // Stats
                string playerId = ctx.GetPlayerUniqueId();
                health.OnDamageDealt( playerId, ctx.Data.Id, finalDamage, isCrit );

                // Hit Effect
                if ( ctx.Data.HitEffectPrefab != null )
                {
                    ctx.Data.HitEffectPrefab.Clone( tr.HitPosition, Rotation.LookAt( tr.Normal ) );
                }

                // Physics Push
                var rb = tr.GameObject.Components.GetInAncestorsOrSelf<Rigidbody>();
                if ( rb != null && rb.MotionEnabled )
                {
                    float pushForce = finalDamage * ctx.Data.ImpactForce;
                    rb.ApplyImpulseAt( tr.HitPosition, direction * pushForce );
                }

                hitCount++;
                if ( hitCount >= maxHits ) break;
            }
            else
            {
                // Si on tape un mur, on s'arrête (l'arme rebondit virtuellement)
                if ( ctx.Data.HitEffectPrefab != null )
                    ctx.Data.HitEffectPrefab.Clone( tr.HitPosition, Rotation.LookAt( tr.Normal ) );
                break;
            }
        }
    }
}