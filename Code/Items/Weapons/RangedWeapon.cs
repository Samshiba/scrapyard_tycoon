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

        var tr = Scene.Trace.Ray( ray, Range )
            .IgnoreGameObjectHierarchy( GameObject.Root )
            .UsePhysicsWorld()
            .Run();

        if ( tr.Hit && tr.GameObject != null )
        {
            var health = tr.GameObject.Components.GetInAncestorsOrSelf<PropHealth>();

            if ( health != null )
            {
                bool isCrit = ShouldCrit();
                float finalDamage = GetFinalDamage( isCrit );
                var damageInfo = new DamageInfo
                {
                    Damage = finalDamage,
                    Position = tr.HitPosition
                };
                damageInfo.Tags.Add( "player" );
                damageInfo.Tags.Add( Data.Id );
                if ( isCrit ) damageInfo.Tags.Add( "critical" );
                health.OnDamage( damageInfo );

                // Track damage for stats
                var backpack = Components.GetInAncestors<PlayerBackpack>();
                // var playerSteamId = backpack?.Network.Owner?.SteamId.ToString() ?? "unknown";
                var playerSteamId = backpack?.Network.Owner?.GetUniqueId() ?? backpack?.Network.Owner?.SteamId.ToString() ?? "unknown";
                health.OnDamageDealt( playerSteamId, Data.Id, finalDamage, isCrit );

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
            bool isCrit = ShouldCrit();
            float finalDamage = GetFinalDamage( isCrit );
            projectileLogic.Initialize( finalDamage, playerGo, Data.Id );
            if ( isCrit )
                projectileLogic.Tags.Add( "critical" );
        }
        else
        {
            Log.Warning( $"[RangedWeapon] ERROR: Projectile prefab '{Data.ProjectilePrefab.Name}' does not have a script inheriting from BaseProjectile" );
        }
    }
}