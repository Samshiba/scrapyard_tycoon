using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class LinearProjectile : BaseProjectile
{
    [Property, Group( "Visuals" )] public GameObject HitEffectPrefab { get; set; }

    protected override void ProcessMovementAndCollision()
    {
        var startPos = WorldPosition;
        var endPos = startPos + WorldRotation.Forward * Speed * Time.Delta;

        var traces = Scene.Trace.Ray( startPos, endPos )
            .IgnoreGameObjectHierarchy( Shooter )
            .IgnoreGameObjectHierarchy( GameObject )
            .WithoutTags( "player" )
            .UsePhysicsWorld()
            .RunAll()
            .OrderBy( t => t.Distance )
            .ToList();

        bool hasHitWall = false;

        foreach ( var tr in traces )
        {
            if ( !tr.Hit || tr.GameObject == null ) continue;

            if ( HitTargets.Contains( tr.GameObject ) ) continue;

            var health = tr.GameObject.Components.GetInAncestorsOrSelf<PropHealth>();

            if ( health != null )
            {
                HitTargets.Add( tr.GameObject );

                var damageInfo = new DamageInfo
                {
                    Damage = Damage,
                    Position = tr.HitPosition
                };

                if ( Tags.Has( "critical" ) ) damageInfo.Tags.Add( "critical" );
                health.OnDamage( damageInfo );

                // Stats
                health.OnDamageDealt( ShooterId, WeaponId, Damage, Tags.Has( "critical" ) );

                if ( HitEffectPrefab != null )
                {
                    HitEffectPrefab.Clone( tr.HitPosition, Rotation.LookAt( tr.Normal ) );
                }

                PierceCount--;
                if ( PierceCount < 0 )
                {
                    hasHitWall = true;
                    break;
                }
            }
            else
            {
                if ( HitEffectPrefab != null )
                {
                    HitEffectPrefab.Clone( tr.HitPosition, Rotation.LookAt( tr.Normal ) );
                }
                hasHitWall = true;
                break;
            }
        }

        if ( hasHitWall )
        {
            GameObject.Destroy();
        }
        else
        {
            WorldPosition = endPos;
        }
    }
}