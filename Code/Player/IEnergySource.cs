public interface IEnergySource
{
    float CurrentEnergy { get; }
    float MaxEnergy { get; }
    bool IsExhausted { get; }

    bool TryConsumeEnergy( float amount );

    void NotifyAttack( float weaponAttackRate );
}