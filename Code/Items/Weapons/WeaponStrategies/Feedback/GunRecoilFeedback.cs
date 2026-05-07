using Sandbox;
using System;

public class GunRecoilFeedback : IWeaponFeedback
{
    private GameObject _weaponObject;
    private Rotation _baseRotation = Rotation.Identity;
    private Vector3 _basePosition = Vector3.Zero;
    private float _currentRecoil = 0f;

    public void Initialize( GameObject weaponObject )
    {
        _weaponObject = weaponObject;
        _baseRotation = _weaponObject.LocalRotation;
        _basePosition = _weaponObject.LocalPosition;
    }

    public void PlayAttackFeedback( IWeaponContext ctx )
    {
        // 1. On applique le coup de recul (Kick)
        _currentRecoil = ctx.Data.RecoilKick;

        // 2. Jouer le son d'attaque
        if ( ctx.Data.AttackSound != null )
        {
            Sound.Play( ctx.Data.AttackSound, ctx.AttackTransform.Position );
        }

        // 3. Faire spawner le Muzzle Flash
        if ( ctx.Data.MuzzleFlashPrefab != null )
        {
            var muzzleFlash = ctx.Data.MuzzleFlashPrefab.Clone( ctx.AttackTransform.Position, ctx.AttackTransform.Rotation );
            muzzleFlash.SetParent( ctx.MuzzleObject );
        }

        // Note : Si tu as des animations de personnages (3rd person) ou bras (1st person), 
        // c'est ici qu'il faut appeler PlayerBody.Set( ctx.Data.AnimationTriggerName, true );
    }

    public void Update( float deltaTime )
    {
        if ( _weaponObject == null ) return;

        // Si l'arme a du recul, on la ramène doucement à sa position d'origine (Recovery)
        if ( _currentRecoil > 0 )
        {
            // On récupère la vitesse de récupération depuis le contexte (on pourrait aussi le stocker)
            var weaponComponent = _weaponObject.Components.Get<WeaponComponent>();
            float recoverySpeed = weaponComponent?.Data.RecoilRecovery ?? 10f;
            float pushbackForce = weaponComponent?.Data.RecoilPushback ?? 3f;

            _currentRecoil = MathX.Lerp( _currentRecoil, 0, deltaTime * recoverySpeed );

            if ( _currentRecoil < 0.001f )
            {
                _currentRecoil = 0f;
                _weaponObject.LocalRotation = _baseRotation;
                _weaponObject.LocalPosition = _basePosition;
                return;
            }

            // On applique la rotation (l'arme se lève)
            bool invertPitch = weaponComponent?.Data.InvertRecoilPitch ?? false;
            float recoilAmount = invertPitch ? -_currentRecoil : _currentRecoil;
            _weaponObject.LocalRotation = _baseRotation * Rotation.FromPitch( recoilAmount );

            // On applique la translation (l'arme recule vers le joueur)
            float kickback = _currentRecoil * (-pushbackForce / 10f);
            _weaponObject.LocalPosition = _basePosition + new Vector3( kickback, 0, 0 );
        }
    }
}