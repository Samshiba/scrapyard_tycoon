using Sandbox;

public sealed class PlayerStats : Component, IEnergySource
{
    // --- Energy System ---
    [Property, Group( "Energy" )] public float MaxEnergy { get; set; } = 100f;
    [Property, Group( "Energy" )] public float RechargeRate { get; set; } = 10f;
    [Property, Group( "Energy" )] public float ExhaustionPenalty { get; set; } = 5.0f;
    [Property, ReadOnly, Group( "Energy" )] public float CurrentEnergy { get; set; } = 100f;
    [Property, ReadOnly, Group( "Energy" )] public bool IsExhausted { get; set; } = false;

    public TimeSince TimeSinceLastAttack { get; private set; }
    public RealTimeSince TimeSinceExhausted { get; private set; }

    private float _currentRechargeDelay = 0.5f;

    protected override void OnUpdate()
    {
        // 1. Gestion de l'épuisement (Exhaustion)
        if ( IsExhausted )
        {
            if ( TimeSinceExhausted >= ExhaustionPenalty )
            {
                IsExhausted = false;
                CurrentEnergy = MaxEnergy;
                Log.Info( "[PlayerStats] Energy restored. Player ready." );
            }
            return;
        }

        // 2. Recharge progressive
        if ( CurrentEnergy < MaxEnergy && TimeSinceLastAttack > _currentRechargeDelay )
        {
            CurrentEnergy += RechargeRate * Time.Delta;
            if ( CurrentEnergy > MaxEnergy ) CurrentEnergy = MaxEnergy;
        }
    }

    // --- Implémentation de IEnergySource ---

    public bool TryConsumeEnergy( float amount )
    {
        if ( IsExhausted || CurrentEnergy < amount ) return false;

        CurrentEnergy -= amount;
        if ( CurrentEnergy <= 0 )
        {
            CurrentEnergy = 0;
            IsExhausted = true;
            TimeSinceExhausted = 0;

            Log.Warning( "[PlayerStats] Energy depleted. Player exhausted." );
        }
        return true;
    }

    public void NotifyAttack( float weaponAttackRate )
    {
        TimeSinceLastAttack = 0;
        _currentRechargeDelay = (1f / weaponAttackRate) + 0.5f;
    }
}