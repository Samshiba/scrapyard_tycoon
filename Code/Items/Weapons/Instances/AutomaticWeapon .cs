using Sandbox;
using System.Collections.Generic;
using System;

public sealed class AutomaticWeapon : RangedWeapon
{
    // --- Data & References ---
    [Property, Group( "Visuals" )] public ModelRenderer GunModel { get; set; }
    [Property, Group( "Visuals" )] public GameObject MuzzleFlashPrefab { get; set; }
    [Property, Group( "Visuals" )] public GameObject MuzzlePoint { get; set; }

    [Property, Group( "Animation" )] public float RecoilKick { get; set; } = 15f;
    [Property, Group( "Animation" )] public float RecoilPushback { get; set; } = 3f;
    [Property, Group( "Animation" )] public float RecoilRecovery { get; set; } = 10f;

    protected override bool IsAutomatic => true;

    private Rotation _baseRotation = Rotation.Identity;
    private Vector3 _basePosition = Vector3.Zero;
    private float _currentRecoil = 0f;

    // --- Lifecycle ---

    protected override void OnStart()
    {
        _baseRotation = LocalRotation;
        _basePosition = LocalPosition;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if ( _currentRecoil > 0 )
        {
            _currentRecoil = MathX.Lerp( _currentRecoil, 0, Time.Delta * RecoilRecovery );

            if ( _currentRecoil < 0.001f )
            {
                _currentRecoil = 0f;

                LocalRotation = _baseRotation;
                LocalPosition = _basePosition;
                return;
            }

            LocalRotation = _baseRotation * Rotation.FromPitch( _currentRecoil );

            float kickback = _currentRecoil * (-RecoilPushback / 10f);
            LocalPosition = _basePosition + new Vector3( kickback, 0, 0 );
        }
    }

    // --- Methods ---

    protected override void PerformAttack()
    {
        _currentRecoil = RecoilKick;

        if ( MuzzleFlashPrefab != null )
        {
            // Utiliser le MuzzlePoint si disponible, sinon fallback sur GunModel
            var muzzlePos = MuzzlePoint != null
                ? MuzzlePoint.WorldPosition
                : GunModel != null
                    ? GunModel.WorldPosition + GunModel.WorldRotation.Forward * 15f
                    : WorldPosition;

            var muzzleFlash = MuzzleFlashPrefab.Clone( muzzlePos );
        }

        DoShoot();
    }

    public override IEnumerable<TooltipEntry> GetTooltips()
    {
        yield return new TooltipEntry { InputAction = "attack1", Description = "Fire" };
    }
}