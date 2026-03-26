using Sandbox;
using System.Collections.Generic;
using System;

public sealed class HitWeapon : MeleeWeapon
{
    // --- Data & References ---
    [Property] public ModelRenderer WeaponModel { get; set; }

    [Property, Group( "Animation" )] public float SwingPitch { get; set; } = 70f;
    [Property, Group( "Animation" )] public float SwingForward { get; set; } = 15f;
    [Property, Group( "Animation" )] public float SwingDrop { get; set; } = -10f;
    [Property, Group( "Animation" )] public float SwingSpeed { get; set; } = 12f;

    private Rotation _baseRotation = Rotation.Identity;
    private Vector3 _basePosition = Vector3.Zero;

    private float _currentSwing = 0f;

    // --- Lifecycle ---

    protected override void OnStart()
    {
        _baseRotation = LocalRotation;
        _basePosition = LocalPosition;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();

        if ( _currentSwing > 0 )
        {
            _currentSwing = MathX.Lerp( _currentSwing, 0, Time.Delta * SwingSpeed );

            if ( _currentSwing < 0.001f )
            {
                _currentSwing = 0f;

                LocalRotation = _baseRotation;
                LocalPosition = _basePosition;
                return;
            }

            var swingRotation = Rotation.FromPitch( _currentSwing * SwingPitch );
            LocalRotation = _baseRotation * swingRotation;

            var swingOffset = new Vector3(
                _currentSwing * SwingForward,
                0,
                _currentSwing * SwingDrop
            );
            LocalPosition = _basePosition + swingOffset;
        }
    }

    // --- Methods ---

    protected override void PerformAttack()
    {
        _currentSwing = 1f;

        DoMeleeHitbox();
    }

    public override IEnumerable<TooltipEntry> GetTooltips()
    {
        yield return new TooltipEntry { InputAction = "attack1", Description = "Hit" };
    }

}