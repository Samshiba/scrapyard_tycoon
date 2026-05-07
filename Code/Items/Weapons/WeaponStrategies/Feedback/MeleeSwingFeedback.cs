using Sandbox;
using System;

public class MeleeSwingFeedback : IWeaponFeedback
{
    private GameObject _weaponObject;
    private Rotation _baseRotation = Rotation.Identity;
    private Vector3 _basePosition = Vector3.Zero;
    private float _currentSwing = 0f;

    public void Initialize( GameObject weaponObject )
    {
        _weaponObject = weaponObject;
        _baseRotation = _weaponObject.LocalRotation;
        _basePosition = _weaponObject.LocalPosition;
    }

    public void PlayAttackFeedback( IWeaponContext ctx )
    {
        // On déclenche le balayage (100% de la force d'un coup)
        _currentSwing = 1f;

        if ( ctx.Data.AttackSound != null )
        {
            Sound.Play( ctx.Data.AttackSound, ctx.AttackTransform.Position );
        }
    }

    public void Update( float deltaTime )
    {
        if ( _weaponObject == null || _currentSwing <= 0 ) return;

        var weaponComponent = _weaponObject.Components.Get<WeaponComponent>();
        if ( weaponComponent == null ) return;

        // On ramène l'arme à sa position de repos
        float speed = weaponComponent.Data.SwingSpeed;
        _currentSwing = MathX.Lerp( _currentSwing, 0, deltaTime * speed );

        if ( _currentSwing < 0.001f )
        {
            _currentSwing = 0f;
            _weaponObject.LocalRotation = _baseRotation;
            _weaponObject.LocalPosition = _basePosition;
            return;
        }

        // On applique les rotations et translations procédurales
        var swingRotation = Rotation.FromPitch( _currentSwing * weaponComponent.Data.SwingPitch );
        _weaponObject.LocalRotation = _baseRotation * swingRotation;

        var swingOffset = new Vector3(
            _currentSwing * weaponComponent.Data.SwingForward,
            0,
            _currentSwing * weaponComponent.Data.SwingDrop
        );
        _weaponObject.LocalPosition = _basePosition + swingOffset;
    }
}