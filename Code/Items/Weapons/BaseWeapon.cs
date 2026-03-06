using Sandbox;
using System.Collections.Generic;

public enum AttackType
{
    Primary,
    Secondary,
    Special
}

public abstract class BaseWeapon : Component, ITooltipProvider
{
    [Property] public WeaponDefinition Data { get; set; }

    public SkinnedModelRenderer PlayerBody { get; set; }

    protected TimeSince TimeSinceLastAttack;
    protected AttackType? QueuedAttack { get; set; }

    protected override void OnUpdate()
    {
        if ( IsProxy || Data == null ) return;

        // Check for new attack input
        if ( Input.Pressed( "attack1" ) )
        {
            QueueAttack( AttackType.Primary );
        }
        else if ( Input.Pressed( "attack2" ) )
        {
            QueueAttack( AttackType.Secondary );
        }

        // Execute queued attack if cooldown allows
        if ( QueuedAttack.HasValue && TimeSinceLastAttack >= (1f / Data.AttackRate) )
        {
            ExecuteAttack( QueuedAttack.Value );
            QueuedAttack = null;
        }
    }

    protected void QueueAttack( AttackType attackType )
    {
        QueuedAttack = attackType;
    }

    protected void ExecuteAttack( AttackType attackType )
    {
        TimeSinceLastAttack = 0;
        OnAttackStart( attackType );

        if ( attackType == AttackType.Primary )
        {
            PrimaryAttack();
        }
        else
        {
            SecondaryAttack();
        }
    }

    public virtual IEnumerable<TooltipEntry> GetTooltips()
    {
        yield return new TooltipEntry { InputAction = "attack1", Description = "Attaquer" };
        yield return new TooltipEntry { InputAction = "attack2", Description = "Attaque secondaire" };
    }

    /// <summary>
    /// Called when an attack is about to start. Use this to trigger animations.
    /// </summary>
    protected virtual void OnAttackStart( AttackType attackType ) { }

    protected abstract void PrimaryAttack();

    protected abstract void SecondaryAttack();
}