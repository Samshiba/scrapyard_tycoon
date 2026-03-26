using Sandbox;

public abstract class RangedWeapon : BaseWeapon
{
    protected void DoShoot()
    {
        if ( Data.ProjectilePrefab != null )
        {
            ShootProjectile();
        }
        else
        {
            ShootHitscan();
        }
        ConsumeEnergy();
    }

    private void ShootHitscan()
    {
        var ray = Scene.Camera.ScreenNormalToRay( 0.5f );

        var tr = Scene.Trace.Ray( ray, Data.Range )
            .IgnoreGameObjectHierarchy( GameObject.Root )
            .UsePhysicsWorld()
            .Run();

        if ( tr.Hit && tr.GameObject != null )
        {
            var health = tr.GameObject.Components.GetInAncestorsOrSelf<PropHealth>();

            if ( health != null )
            {
                var damageInfo = new DamageInfo
                {
                    Damage = Damage,
                    Position = tr.HitPosition
                };
                damageInfo.Tags.Add( "player" );
                health.OnDamage( damageInfo );

                if ( Data.HitEffectPrefab != null )
                {
                    var fx = Data.HitEffectPrefab.Clone( tr.HitPosition, Rotation.LookAt( tr.Normal ) );
                }
            }
        }
    }

    private void ShootProjectile()
    {
        var spawnPos = Scene.Camera.WorldPosition + Scene.Camera.WorldRotation.Forward * 100f;
        var spawnRot = Scene.Camera.WorldRotation;

        var projectileObj = Data.ProjectilePrefab.Clone( spawnPos, spawnRot );

        var projectileLogic = projectileObj.Components.GetInChildrenOrSelf<BaseProjectile>();

        if ( projectileLogic != null )
        {
            var playerGo = Components.GetInAncestors<PlayerController>()?.GameObject ?? GameObject.Root;
            projectileLogic.Initialize( this.Damage, playerGo );
        }
        else
        {
            Log.Warning( $"Le prefab {Data.ProjectilePrefab.Name} n'a pas de script héritant de BaseProjectile !" );
        }
    }
}