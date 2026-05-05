using Sandbox;

public abstract class MeleeWeapon : BaseWeapon
{
    protected void DoMeleeHitbox()
    {
        var ray = Scene.Camera.ScreenNormalToRay( 0.5f );

        var tr = Scene.Trace.Ray( ray, Range )
            .IgnoreGameObjectHierarchy( GameObject.Root )
            .Radius( 15f )
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
                if ( isCrit ) damageInfo.Tags.Add( "critical" );
                damageInfo.Tags.Add( Data.Id );
                health.OnDamage( damageInfo );

                // Track damage for stats
                var backpack = Components.GetInAncestors<PlayerBackpack>();
                // var playerSteamId = backpack?.Network.Owner?.SteamId.ToString() ?? "unknown";
                var playerSteamId = backpack?.Network.Owner?.GetUniqueId() ?? backpack?.Network.Owner?.SteamId.ToString() ?? "unknown";
                health.OnDamageDealt( playerSteamId, Data.Id, finalDamage, isCrit );

                if ( Data.HitEffectPrefab != null )
                {
                    var fx = Data.HitEffectPrefab.Clone( tr.HitPosition );
                }
            }
        }

        ConsumeEnergy();
    }
}