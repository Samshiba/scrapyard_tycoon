using Sandbox;

public sealed class FactoryStats : Component, Component.INetworkListener
{
    public static FactoryStats Instance { get; private set; }

    [Sync][Property] public double TotalScrap { get; private set; } = 0;

    private bool _isLoaded = false;

    protected override void OnAwake()
    {
        Instance = this;
    }

    protected override void OnUpdate()
    {
        if ( Networking.IsHost && !_isLoaded && SaveManager.Instance?.IsFactoryReady == true )
        {
            TotalScrap = SaveManager.Instance.CurrentFactory.TotalScrap;
            _isLoaded = true;
        }
    }

    public void AddScrap( double amount )
    {
        if ( !Networking.IsHost ) return;

        TotalScrap += amount;

        SaveManager.Instance.CurrentFactory.TotalScrap = TotalScrap;

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ScrapChanged, $"+{amount} scrap" );
    }

    public bool SpendScrap( double amount )
    {
        if ( !Networking.IsHost ) return false;

        if ( TotalScrap >= amount )
        {
            TotalScrap -= amount;

            SaveManager.Instance.CurrentFactory.TotalScrap = TotalScrap;

            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ScrapChanged, $"-{amount} scrap" );
            return true;
        }
        return false;
    }
}