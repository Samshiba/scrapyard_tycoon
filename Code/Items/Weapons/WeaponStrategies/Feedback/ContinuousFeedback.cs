using Sandbox;

public class ContinuousFeedback : IWeaponFeedback
{
    private GameObject _weaponObject;
    private GameObject _activeStreamParticle;

    public void Initialize( GameObject weaponObject )
    {
        _weaponObject = weaponObject;
    }

    public void PlayAttackFeedback( IWeaponContext ctx )
    {
        // Handled in Update() since it's continuous.
    }

    public void Update( float deltaTime )
    {
        if ( _weaponObject == null ) return;

        var ctx = _weaponObject.Components.Get<WeaponComponent>();
        if ( ctx == null ) return;

        bool isFiring = Input.Down( "attack1" ) && !ctx.IsExhausted();

        if ( isFiring )
        {
            if ( _activeStreamParticle == null && ctx.Data.StreamEffectPrefab != null )
            {
                Log.Info( "Spawning continuous stream particle effect." );
                _activeStreamParticle = ctx.Data.StreamEffectPrefab.Clone( ctx.AttackTransform.Position, ctx.AttackTransform.Rotation );
                _activeStreamParticle.SetParent( ctx.MuzzleObject );

                if ( ctx.Data.AttackSound != null )
                {
                    Sound.Play( ctx.Data.AttackSound, ctx.AttackTransform.Position );
                }
            }
        }
        else
        {
            if ( _activeStreamParticle != null )
            {
                _activeStreamParticle.Destroy();
                _activeStreamParticle = null;
            }
        }
    }
}