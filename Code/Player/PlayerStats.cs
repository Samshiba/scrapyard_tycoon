using Sandbox;

public sealed class PlayerStats : Component
{
    // --- Energy System ---
    [Property, Group( "Energy" )] public float MaxEnergy { get; set; } = 100f;
    [Property, Group( "Energy" )] public float RechargeRate { get; set; } = 10f;
    [Property, Group( "Energy" )] public float ExhaustionPenalty { get; set; } = 5.0f;
    [Property, ReadOnly, Group( "Energy" )] public float CurrentEnergy { get; set; } = 100f;
    [Property, ReadOnly, Group( "Energy" )] public bool IsExhausted { get; set; } = false;

    public TimeSince TimeSinceLastAttack { get; set; }
    public RealTimeSince TimeSinceExhausted { get; set; }
}