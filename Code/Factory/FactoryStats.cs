using System.Collections.Generic;
using System.Linq;
using Sandbox;

public sealed class FactoryStats : Component, Component.INetworkListener
{
    public static FactoryStats Instance { get; private set; }

    [Sync][Property] public double TotalScrap { get; private set; } = 0;

    [Sync][Property] public double CurrentSPS { get; private set; } = 0;

    private bool _isLoaded = false;

    // --- Variables pour le calcul du SPS ---
    private Queue<double> _scrapHistory = new();
    private TimeSince _timeSinceLastTick = 0;
    private double _scrapSinceLastTick = 0;
    private const int SPS_WINDOW_SECONDS = 10;

    protected override void OnAwake()
    {
        Instance = this;
    }

    protected override void OnUpdate()
    {
        if ( Networking.IsHost )
        {
            if ( !_isLoaded && SaveManager.Instance?.IsFactoryReady == true )
            {
                TotalScrap = SaveManager.Instance.CurrentFactory.TotalScrap;
                _isLoaded = true;
            }

            if ( _isLoaded && _timeSinceLastTick >= 1f )
            {
                _scrapHistory.Enqueue( _scrapSinceLastTick );

                if ( _scrapHistory.Count > SPS_WINDOW_SECONDS )
                {
                    _scrapHistory.Dequeue();
                }

                CurrentSPS = _scrapHistory.Average();

                _scrapSinceLastTick = 0;
                _timeSinceLastTick = 0;
            }
        }
    }

    public void AddScrap( double amount )
    {
        if ( !Networking.IsHost ) return;

        TotalScrap += amount;
        _scrapSinceLastTick += amount;

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