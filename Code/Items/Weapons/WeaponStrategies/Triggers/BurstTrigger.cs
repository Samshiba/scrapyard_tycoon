using Sandbox;
using System;

public class BurstTrigger : IWeaponTrigger
{
    private Action _onFire;
    private TimeSince _timeSinceLastShot;
    private TimeSince _timeSinceBurstShot;

    private bool _isBursting = false;
    private int _shotsRemaining = 0;

    public void BindActions( Action onFireEvent ) => _onFire = onFireEvent;

    public bool Update( IWeaponContext ctx, bool inputPressed, bool inputDown, bool inputReleased )
    {
        float attackRate = ctx.GetStat( WeaponStatTarget.AttackRate );

        // 1. Déclencher une nouvelle rafale
        if ( !_isBursting && inputPressed && _timeSinceLastShot >= (1f / attackRate) )
        {
            _isBursting = true;
            _shotsRemaining = ctx.Data.BurstCount;
            _timeSinceBurstShot = ctx.Data.BurstFireRate; // Pour tirer la 1ère balle tout de suite
        }

        // 2. Gérer la rafale en cours
        if ( _isBursting )
        {
            if ( _timeSinceBurstShot >= ctx.Data.BurstFireRate )
            {
                _onFire?.Invoke();
                _shotsRemaining--;
                _timeSinceBurstShot = 0;

                if ( _shotsRemaining <= 0 )
                {
                    _isBursting = false;
                    _timeSinceLastShot = 0; // Le cooldown global commence APRES la rafale
                }
            }
            return true; // L'arme est active ce frame
        }

        return false;
    }
}