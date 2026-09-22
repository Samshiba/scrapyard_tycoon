using Sandbox;

public class ProjectileDelivery : IWeaponDelivery
{
    public void Execute( IWeaponContext ctx )
    {
        if ( ctx.Data.ProjectilePrefab == null )
        {
            Log.Warning( $"[ProjectileDelivery] Aucun ProjectilePrefab défini pour {ctx.Data.Id}!" );
            return;
        }

        int projectiles = ctx.Data.ProjectilesPerShot;

        float baseDamage = ctx.GetStat( WeaponStatTarget.Damage );
        float damagePerPellet = baseDamage / projectiles;

        for ( int i = 0; i < projectiles; i++ )
        {
            ShootSingleProjectile( ctx, damagePerPellet );
        }
    }

    private void ShootSingleProjectile( IWeaponContext ctx, float basePelletDamage )
    {
        // 1. On trouve où le joueur regarde (Caméra -> Viseur)
        var camera = ctx.Owner.Scene.Camera;
        var ray = camera.ScreenNormalToRay( 0.5f );
        var trace = ctx.Owner.Scene.Trace.Ray( ray.Position, ray.Position + ray.Forward * 5000f )
            .IgnoreGameObjectHierarchy( ctx.Owner )
            .UsePhysicsWorld()
            .Run();

        Vector3 targetPoint = trace.Hit ? trace.HitPosition : ray.Position + ray.Forward * 5000f;

        var startPos = ctx.AttackTransform.Position;
        var baseRot = Rotation.LookAt( targetPoint - startPos );

        // 2. Application du Spread (Dispersion)
        Rotation finalRot = baseRot;
        if ( ctx.Data.SpreadAngle > 0f )
        {
            float spread = ctx.Data.SpreadAngle;
            var spreadRot = Rotation.From(
                Game.Random.Float( -spread, spread ),
                Game.Random.Float( -spread, spread ),
                0
            );
            finalRot *= spreadRot;
        }

        // 3. Spawn du Projectile
        var projectileObj = ctx.Data.ProjectilePrefab.Clone( startPos, finalRot );
        var projectileLogic = projectileObj.Components.GetInChildrenOrSelf<BaseProjectile>();

        if ( projectileLogic != null )
        {
            // 4. Calcul des dégâts et transmission des infos
            bool isCrit = ctx.RollCritical();

            float randomVariance = Game.Random.Float( 0.9f, 1.1f );
            float damage = basePelletDamage * randomVariance;
            float finalDamage = isCrit ? damage * ctx.GetStat( WeaponStatTarget.CriticalDamage ) : damage;

            projectileLogic.Initialize( finalDamage, ctx.Owner, ctx.Data.Id, ctx.Data.PierceCount, ctx.GetPlayerUniqueId() );

            if ( isCrit )
                projectileLogic.Tags.Add( "critical" );
        }
        else
        {
            Log.Warning( $"[ProjectileDelivery] Le prefab {ctx.Data.ProjectilePrefab.Name} n'a pas de BaseProjectile!" );
        }
    }
}