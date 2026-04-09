using Sandbox;

public sealed class PlayerStats : Component
{
    public static PlayerStats Local { get; private set; }
    [Property] public float TotalScrap { get; private set; } = 0;

    // --- Energy System ---
    [Property, Group( "Energy" )] public float MaxEnergy { get; set; } = 100f;
    [Property, Group( "Energy" )] public float RechargeRate { get; set; } = 10f;
    [Property, Group( "Energy" )] public float ExhaustionPenalty { get; set; } = 5.0f;
    [Property, ReadOnly, Group( "Energy" )] public float CurrentEnergy { get; set; } = 100f;
    [Property, ReadOnly, Group( "Energy" )] public bool IsExhausted { get; set; } = false;

    public TimeSince TimeSinceLastAttack { get; set; }
    public RealTimeSince TimeSinceExhausted { get; set; }


    protected override void OnAwake()
    {
        if ( !IsProxy )
            Local = this;

        if ( !IsProxy && SaveManager.Instance?.Data?.Player != null )
        {
            TotalScrap = SaveManager.Instance.Data.Player.TotalScrap;
            Log.Info( $"[PlayerStats] Total scrap loaded: {TotalScrap}" );
        }
    }

    public void AddScrap( float amount )
    {
        if ( amount <= 0 ) return;

        TotalScrap += amount;
        Log.Info( $"[PlayerStats] Transaction completed: +{amount} scrap. Total balance: {TotalScrap}" );
        SaveChanges();
    }

    public bool SpendScrap( float amount )
    {
        if ( TotalScrap >= amount )
        {
            TotalScrap -= amount;
            SaveChanges();
            return true;
        }
        return false;
    }

    private void SaveChanges()
    {
        if ( !IsProxy && SaveManager.Instance != null )
        {
            SaveManager.Instance.Data.Player.TotalScrap = TotalScrap;
            
            // Notify throttler about the change (save will be batched)
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ScrapChanged, $"+{TotalScrap:F0} scrap" );
        }
    }
}