using Sandbox;
using System;

public class FullAutoTrigger : IWeaponTrigger
{
    private Action _onFire;
    private TimeSince _timeSinceLastShot;

    public void BindActions( Action onFireEvent ) => _onFire = onFireEvent;

    public bool Update( IWeaponContext ctx, bool inputPressed, bool inputDown, bool inputReleased )
    {
        float attackRate = ctx.GetStat( WeaponStatTarget.AttackRate );

        if ( inputDown && _timeSinceLastShot >= (1f / attackRate) )
        {
            _onFire?.Invoke();
            _timeSinceLastShot = 0;
            return true;
        }
        return false;
    }
}