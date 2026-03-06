using Sandbox;
using System.Collections.Generic;
using System;

public sealed class BatWeapon : BaseWeapon
{
    [Property] public ModelRenderer BatModel { get; set; }

    private bool _isSwinging = false;
    private TimeSince _swingTimer;
    private Rotation _baseRotation = Rotation.Identity;

    private const float SwingWindupDuration = 0.1f;
    private const float SwingStrikeDuration = 0.2f;
    private const float SwingRecoveryDuration = 0.15f;
    private const float TotalSwingDuration = SwingWindupDuration + SwingStrikeDuration + SwingRecoveryDuration; // ~0.45s
    private const float MaxSwingRotation = 90f;

    protected override void OnUpdate()
    {
        base.OnUpdate();
        UpdateSwingAnimation();
    }

    protected override void OnAttackStart( AttackType attackType )
    {
        if ( attackType == AttackType.Primary )
        {
            _baseRotation = LocalRotation;
            _isSwinging = true;
            _swingTimer = 0;
        }
    }

    private void UpdateSwingAnimation()
    {
        if ( !_isSwinging || BatModel == null ) return;

        float progress = _swingTimer / TotalSwingDuration;

        if ( progress >= 1f )
        {
            _isSwinging = false;
            LocalRotation = Rotation.Identity;
            return;
        }

        float rotation;

        if ( progress < SwingWindupDuration / TotalSwingDuration )
        {
            float windupProgress = progress / (SwingWindupDuration / TotalSwingDuration);
            rotation = -MaxSwingRotation * EaseInQuad( windupProgress );
        }
        else if ( progress < (SwingWindupDuration + SwingStrikeDuration) / TotalSwingDuration )
        {
            float strikeProgress = (progress - SwingWindupDuration / TotalSwingDuration) / (SwingStrikeDuration / TotalSwingDuration);
            rotation = -MaxSwingRotation + (-MaxSwingRotation * strikeProgress);
        }
        else
        {
            float recoveryProgress = (progress - (SwingWindupDuration + SwingStrikeDuration) / TotalSwingDuration) / (SwingRecoveryDuration / TotalSwingDuration);
            rotation = -MaxSwingRotation * (1 - EaseOutQuad( recoveryProgress ));
        }

        LocalRotation = _baseRotation * Rotation.FromRoll( rotation );
    }

    // Ease functions for smoother animation
    private float EaseInQuad( float t ) => t * t;
    private float EaseOutQuad( float t ) => 1 - (1 - t) * (1 - t);

    protected override void PrimaryAttack()
    {
        var ray = Scene.Camera.ScreenNormalToRay( 0.5f );

        var tr = Scene.Trace.Ray( ray, Data.Range )
            .IgnoreGameObjectHierarchy( GameObject.Root )
            .Run();

        if ( tr.Hit && tr.GameObject != null )
        {
            var health = tr.GameObject.Components.GetInAncestorsOrSelf<PropHealth>();

            if ( health != null )
            {
                var damageInfo = new DamageInfo
                {
                    Damage = Data.BaseDamage,
                    Position = tr.HitPosition
                };
                damageInfo.Tags.Add( "player" );
                health.OnDamage( damageInfo );
            }
        }
    }

    public override IEnumerable<TooltipEntry> GetTooltips()
    {
        yield return new TooltipEntry { InputAction = "attack1", Description = "Swing" };
    }

    protected override void SecondaryAttack()
    {
        // No secondary attack for the bat
    }

}