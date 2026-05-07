using Sandbox;
using System.Linq;

public class HitscanDelivery : IWeaponDelivery
{
    public void Execute( IWeaponContext ctx )
    {
        int projectiles = ctx.Data.ProjectilesPerShot;

        float baseDamage = ctx.GetStat( WeaponStatTarget.Damage );
        float damagePerPellet = baseDamage / projectiles;

        for ( int i = 0; i < projectiles; i++ )
        {
            ShootSingleRay( ctx, damagePerPellet, i == 0 );
        }
    }

    private void ShootSingleRay( IWeaponContext ctx, float basePelletDamage, bool isCenterPellet )
    {
        var camera = ctx.Owner.Scene.Camera;
        var ray = camera.ScreenNormalToRay( 0.5f );

        var startPos = ray.Position;
        var direction = ray.Forward;

        // --- SPREAD (Dispersion) ---
        if ( ctx.Data.SpreadAngle > 0f && !isCenterPellet )
        {
            float spread = ctx.Data.SpreadAngle;
            var spreadRot = Rotation.From(
                Game.Random.Float( -spread, spread ),
                Game.Random.Float( -spread, spread ),
                0
            );
            direction = (spreadRot * Rotation.LookAt( direction )).Forward;
        }
        float range = ctx.GetStat( WeaponStatTarget.Range );

        // --- RAYCAST AVEC PIERCING ---
        var scene = ctx.Owner.Scene;
        var traces = scene.Trace.Ray( startPos, startPos + direction * range )
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
                // Base Damage
                bool isCrit = ctx.RollCritical();

                float randomVariance = Game.Random.Float( 0.9f, 1.1f );
                float damage = basePelletDamage * randomVariance;
                float baseDamage = isCrit ? damage * ctx.GetStat( WeaponStatTarget.CriticalDamage ) : damage;

                // Piercing Damage Reduction
                float piercingDamageMultiplier = MathX.Clamp( 1f - (hitCount * ctx.Data.PierceDamagePenalty), 0.1f, 1f );

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
                float damageForThisTarget = baseDamage * piercingDamageMultiplier * falloffFactor;

                var damageInfo = new DamageInfo
                {
                    Damage = damageForThisTarget,
                    Position = tr.HitPosition
                };

                // Tags
                damageInfo.Tags.Add( "player" );
                damageInfo.Tags.Add( ctx.Data.Id );
                damageInfo.Tags.Add( "ranged" );
                if ( isCrit ) damageInfo.Tags.Add( "critical" );
                if ( ctx.Data.PierceCount > 0 ) damageInfo.Tags.Add( "piercing" );

                health.OnDamage( damageInfo );

                // Stats
                string playerId = ctx.GetPlayerUniqueId();
                health.OnDamageDealt( playerId, ctx.Data.Id, damageForThisTarget, isCrit );

                // Hit Effect
                if ( ctx.Data.HitEffectPrefab != null )
                {
                    ctx.Data.HitEffectPrefab.Clone( tr.HitPosition, Rotation.LookAt( tr.Normal ) );
                }

                // Physics Push
                var rb = tr.GameObject.Components.GetInAncestorsOrSelf<Rigidbody>();
                if ( rb != null && rb.MotionEnabled )
                {
                    float pushForce = damageForThisTarget * ctx.Data.ImpactForce;
                    rb.ApplyImpulseAt( tr.HitPosition, direction * pushForce );
                }

                hitCount++;
                if ( hitCount >= maxHits ) break;
            }
            else
            {
                if ( ctx.Data.HitEffectPrefab != null )
                {
                    ctx.Data.HitEffectPrefab.Clone( tr.HitPosition, Rotation.LookAt( tr.Normal ) );
                }
                break;
            }
        }
    }
}