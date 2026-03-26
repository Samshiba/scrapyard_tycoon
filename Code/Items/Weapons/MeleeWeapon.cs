using Sandbox;

public abstract class MeleeWeapon : BaseWeapon
{
    protected void DoMeleeHitbox()
    {
        var ray = Scene.Camera.ScreenNormalToRay( 0.5f );

        var tr = Scene.Trace.Ray( ray, Data.Range )
            .IgnoreGameObjectHierarchy( GameObject.Root )
            .Radius( 15f )
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
                    var fx = Data.HitEffectPrefab.Clone( tr.HitPosition );
                }
            }
        }

        ConsumeEnergy();
    }
}