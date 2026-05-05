using System.Collections.Generic;
using System.Linq;
using Sandbox;

public sealed class FactoryStats : Component, Component.INetworkListener
{
    // REAL TIME STATS
    [Sync][Property] public double TotalScrap { get; private set; } = 0;
    [Sync][Property] public double CurrentSPS { get; private set; } = 0;
    [Sync][Property] public int Tier { get; set; } = 1;

    // STATS TO SAVE
    [Sync] public double ScrapGained { get; set; } = 0;
    [Sync] public double LargestScrapGain { get; set; } = 0;
    [Sync] public double TotalMoneySpent { get; set; } = 0;
    [Sync] public double LargestSinglePurchase { get; set; } = 0;
    [Sync] public int GibsDropped { get; set; } = 0;

    [Sync] public double TotalDamage { get; set; } = 0;
    [Sync] public double HighestDamageHit { get; set; } = 0;
    [Sync] public int TotalAttacks { get; set; } = 0;
    [Sync] public int TotalCriticalHits { get; set; } = 0;
    [Sync] public double CriticalDamage { get; set; } = 0;
    [Sync] public long TargetsDestroyed { get; set; } = 0;

    [Sync] public float TimePlayed { get; set; } = 0f;
    [Sync] public int TimesPrestiged { get; set; } = 0;
    [Sync] public int PrestigePoints { get; set; } = 0;

    [Sync] public int WeaponsBought { get; set; } = 0;
    [Sync] public int UpgradesBought { get; set; } = 0;

    [Sync] public double EnergyConsumed { get; set; } = 0;
    [Sync] public int ExhaustionPenalties { get; set; } = 0;

    private bool _isLoaded = false;

    // --- Variables pour le calcul du SPS ---
    private Queue<double> _scrapHistory = new();
    private TimeSince _timeSinceLastTick = 0;
    private double _scrapSinceLastTick = 0;
    private const int SPS_WINDOW_SECONDS = 5;

    public static FactoryStats Get( Scene scene )
    {
        return scene.GetAllComponents<FactoryStats>().FirstOrDefault();
    }

    protected override void OnUpdate()
    {
        if ( !Networking.IsHost ) return;

        if ( !_isLoaded && SaveManager.Get( Scene )?.IsFactoryReady == true )
        {
            var fStats = SaveManager.Get( Scene ).CurrentFactory.Stats;

            TotalScrap = SaveManager.Get( Scene ).CurrentFactory.TotalScrap;
            Tier = SaveManager.Get( Scene ).CurrentFactory.Tier;

            ScrapGained = fStats.ScrapGained;
            LargestScrapGain = fStats.LargestScrapGain;
            TotalMoneySpent = fStats.TotalMoneySpent;
            LargestSinglePurchase = fStats.LargestSinglePurchase;
            GibsDropped = fStats.GibsDropped;

            TotalDamage = fStats.TotalDamage;
            HighestDamageHit = fStats.HighestDamageHit;
            TotalAttacks = fStats.TotalAttacks;
            TotalCriticalHits = fStats.TotalCriticalHits;
            CriticalDamage = fStats.CriticalDamage;
            TargetsDestroyed = fStats.TargetsDestroyed;

            TimePlayed = fStats.TimePlayed;
            TimesPrestiged = fStats.TimesPrestiged;
            PrestigePoints = fStats.PrestigePoints;

            WeaponsBought = fStats.WeaponsBought;
            UpgradesBought = fStats.UpgradesBought;

            EnergyConsumed = fStats.EnergyConsumed;
            ExhaustionPenalties = fStats.ExhaustionPenalties;

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

    public void AddScrap( double amount )
    {
        if ( !Networking.IsHost ) return;

        TotalScrap += amount;
        _scrapSinceLastTick += amount;

        SaveManager.Get( Scene ).CurrentFactory.TotalScrap = TotalScrap;

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ScrapChanged, $"+{amount} scrap" );
    }

    public bool SpendScrap( double amount )
    {
        if ( !Networking.IsHost ) return false;

        if ( TotalScrap >= amount )
        {
            TotalScrap -= amount;

            SaveManager.Get( Scene ).CurrentFactory.TotalScrap = TotalScrap;

            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ScrapChanged, $"-{amount} scrap" );
            return true;
        }
        return false;
    }
}