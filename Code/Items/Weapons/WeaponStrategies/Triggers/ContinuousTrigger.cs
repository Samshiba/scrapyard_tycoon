using Sandbox;
using System;

public class ContinuousTrigger : IWeaponTrigger
{
    private Action _onFire;
    private TimeSince _timeSinceLastTick;

    public void BindActions( Action onFireEvent ) => _onFire = onFireEvent;

    public bool Update( IWeaponContext ctx, bool inputPressed, bool inputDown, bool inputReleased )
    {
        if ( inputDown && !ctx.IsExhausted() )
        {
            float tickRate = ctx.GetStat( WeaponStatTarget.AttackRate );

            // Tick Damage à une cadence définie par AttackRate, tant que le trigger est maintenu et que l'arme n'est pas à court d'énergie.
            if ( _timeSinceLastTick >= (1f / tickRate) )
            {
                _onFire?.Invoke();
                _timeSinceLastTick = 0;
            }
            return true;
        }

        return false;
    }
}