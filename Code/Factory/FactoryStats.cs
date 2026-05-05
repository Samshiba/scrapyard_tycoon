using System.Collections.Generic;
using System.Linq;
using Sandbox;

public sealed class FactoryStats : Component, Component.INetworkListener
{
    // REAL TIME STATS
    [Sync][Property] public double TotalScrap { get; private set; } = 0;
    [Sync][Property] public int PrestigePoints { get; private set; } = 0;
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
    [Sync] public int TotalPrestigePoints { get; set; } = 0;

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
            PrestigePoints = SaveManager.Get( Scene ).CurrentFactory.PrestigePoints;
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
            TotalPrestigePoints = fStats.PrestigePoints;

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
        SaveManager.Get( Scene ).CurrentFactory.WorldState.RunScrapGained += amount;

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

    /// <summary>
    /// Add prestige points to the factory (earned from prestige reset or refund).
    /// Automatically syncs to SaveManager, [Sync], and SaveEventBus on all paths.
    /// When trackStats = true, also triggers GameStats.OnPrestige() for stat tracking.
    /// </summary>
    public void AddPrestige( int amount, bool trackStats = true )
    {
        if ( !Networking.IsHost ) return;

        var factory = SaveManager.Get( Scene )?.CurrentFactory;
        if ( factory == null ) return;

        PrestigePoints += amount;
        factory.PrestigePoints = PrestigePoints;

        // Only track stats if this is a real prestige reset (not a respec refund)
        if ( trackStats )
        {
            GameStats.OnPrestige( Scene, amount );
        }

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PrestigePointsChanged, $"+{amount} prestige points" );
    }

    /// <summary>
    /// Spend prestige points (for prestige upgrades). Returns false if insufficient points.
    /// Automatically syncs to SaveManager, [Sync], and SaveEventBus on success.
    /// </summary>
    public bool SpendPrestige( int amount )
    {
        if ( !Networking.IsHost ) return false;

        if ( PrestigePoints >= amount )
        {
            PrestigePoints -= amount;

            var factory = SaveManager.Get( Scene )?.CurrentFactory;
            if ( factory != null )
            {
                factory.PrestigePoints = PrestigePoints;
            }

            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.PrestigePointsChanged, $"-{amount} prestige points" );
            return true;
        }
        return false;
    }

    /// <summary>
    /// Wipe scrap and run stats for prestige reset.
    /// Automatically syncs to SaveManager and [Sync].
    /// </summary>
    public void WipeScrapForPrestige()
    {
        if ( !Networking.IsHost ) return;

        TotalScrap = 0;
        _scrapSinceLastTick = 0;

        var factory = SaveManager.Get( Scene )?.CurrentFactory;
        if ( factory != null )
        {
            factory.TotalScrap = 0;
            factory.WorldState.RunScrapGained = 0;
        }

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ScrapChanged, "Wiped for prestige" );
    }

    /// <summary>
    /// Unlock a weapon for the player. Handles SaveManager, [Sync], GameStats, and SaveEventBus.
    /// Returns true if successfully unlocked (or already unlocked).
    /// </summary>
    public bool UnlockWeapon( string weaponId, string steamId = null )
    {
        if ( !Networking.IsHost ) return false;

        var itemUnlock = ItemUnlockSystem.Get( Scene );
        if ( itemUnlock == null ) return false;

        // Check if already unlocked
        if ( itemUnlock.IsWeaponUnlocked( weaponId ) )
        {
            return false; // Already unlocked
        }

        // Add to net list and SaveManager
        itemUnlock.UnlockedWeapons.Add( weaponId );

        var factory = SaveManager.Get( Scene )?.CurrentFactory;
        if ( factory != null )
        {
            factory.UnlockedWeapons = [.. itemUnlock.UnlockedWeapons];
        }

        // Track stat if steamId provided
        if ( !string.IsNullOrEmpty( steamId ) )
        {
            GameStats.OnWeaponBought( Scene, steamId );
        }

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ItemUnlocked, $"Weapon: {weaponId}" );
        Log.Info( $"[FactoryStats] Weapon unlocked: {weaponId}" );

        return true;
    }

    /// <summary>
    /// Unlock a utility for the player. Handles SaveManager, [Sync], and SaveEventBus.
    /// </summary>
    public bool UnlockUtility( string utilityId )
    {
        if ( !Networking.IsHost ) return false;

        var itemUnlock = ItemUnlockSystem.Get( Scene );
        if ( itemUnlock == null ) return false;

        // Check if already unlocked
        if ( itemUnlock.IsUtilityUnlocked( utilityId ) )
        {
            return false;
        }

        // Add to net list and SaveManager
        itemUnlock.UnlockedUtilities.Add( utilityId );

        var factory = SaveManager.Get( Scene )?.CurrentFactory;
        if ( factory != null )
        {
            factory.UnlockedUtilities = [.. itemUnlock.UnlockedUtilities];
        }

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ItemUnlocked, $"Utility: {utilityId}" );
        Log.Info( $"[FactoryStats] Utility unlocked: {utilityId}" );

        return true;
    }

    /// <summary>
    /// Increment factory tier. Handles SaveManager and [Sync].
    /// </summary>
    public void IncrementTier()
    {
        if ( !Networking.IsHost ) return;

        Tier++;

        var factory = SaveManager.Get( Scene )?.CurrentFactory;
        if ( factory != null )
        {
            factory.Tier++;
        }

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.GlobalUpgradeChanged, $"Tier incremented to {Tier}" );
    }

    /// <summary>
    /// Purchase a global upgrade (SkillTree or Prestige type).
    /// Handles payment validation, SaveManager updates, [Sync], stat tracking, and tier increments.
    /// Returns true if purchase successful.
    /// </summary>
    public bool PurchaseUpgrade( string upgradeId, string steamId = null )
    {
        if ( !Networking.IsHost ) return false;

        var factory = SaveManager.Get( Scene )?.CurrentFactory;
        var globalUpgrades = GlobalUpgradesSystem.Instance;

        if ( factory == null || globalUpgrades == null ) return false;

        // Get upgrade definition
        if ( !UpgradeManager.Instance.Database.TryGetValue( upgradeId, out var node ) )
        {
            return false;
        }

        int currentLevel = globalUpgrades.GetUpgradeLevel( upgradeId );
        if ( currentLevel >= node.MaxLevel ) return false;

        // Validate unlock requirements
        if ( !UpgradeManager.Instance.IsNodeUnlocked( upgradeId, factory ) ) return false;

        double cost = node.GetCostForLevel( currentLevel );
        bool paymentSuccess = false;

        // Attempt payment based on type
        if ( node.Type == UpgradeType.SkillTree )
        {
            if ( SpendScrap( cost ) )
            {
                paymentSuccess = true;
                // Scrap spending already tracked by SpendScrap()
                if ( !string.IsNullOrEmpty( steamId ) )
                {
                    GameStats.OnMoneySpent( Scene, steamId, cost );
                }
            }
        }
        else if ( node.Type == UpgradeType.Prestige )
        {
            if ( SpendPrestige( (int)cost ) )
            {
                paymentSuccess = true;
                // Prestige doesn't need additional stat tracking for spending
            }
        }
        else
        {
            Log.Warning( $"[FactoryStats] Unknown upgrade type for {upgradeId}" );
            return false;
        }

        if ( paymentSuccess )
        {
            // Update upgrade level in both layers
            factory.GlobalUpgrades[upgradeId] = currentLevel + 1;
            globalUpgrades.SyncedUpgrades[upgradeId] = currentLevel + 1;

            // Handle tier increment for root_node
            if ( upgradeId == "root_node" )
            {
                IncrementTier();
            }

            // Track upgrade purchase stat
            if ( !string.IsNullOrEmpty( steamId ) )
            {
                GameStats.OnUpgradeBought( Scene, steamId );
            }

            // Rebuild upgrade cache
            globalUpgrades.RebuildCache();

            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.GlobalUpgradeChanged, $"{upgradeId} → Level {currentLevel + 1}" );
            Log.Info( $"[FactoryStats] Upgrade purchased: {upgradeId} (Level {currentLevel + 1})" );

            return true;
        }

        Log.Warning( $"[FactoryStats] Insufficient funds for upgrade: {upgradeId}" );
        return false;
    }

    /// <summary>
    /// Wipe global upgrades for prestige reset, keeping only prestige-type upgrades.
    /// Handles SaveManager and SyncedUpgrades cleanup.
    /// </summary>
    public void WipeGlobalUpgradesForPrestige()
    {
        if ( !Networking.IsHost ) return;

        var factory = SaveManager.Get( Scene )?.CurrentFactory;
        var globalUpgrades = GlobalUpgradesSystem.Instance;

        if ( factory == null || globalUpgrades == null ) return;

        // Keep only prestige upgrades
        var prestigeUpgradesToKeep = factory.GlobalUpgrades
            .Where( kvp => UpgradeManager.Instance.Database.TryGetValue( kvp.Key, out var def ) && def.Type == UpgradeType.Prestige )
            .ToDictionary( kvp => kvp.Key, kvp => kvp.Value );

        // Wipe non-prestige upgrades from both layers
        var nonPrestigeKeys = factory.GlobalUpgrades
            .Where( kvp => !prestigeUpgradesToKeep.ContainsKey( kvp.Key ) )
            .Select( kvp => kvp.Key )
            .ToList();

        foreach ( var key in nonPrestigeKeys )
        {
            factory.GlobalUpgrades.Remove( key );
            globalUpgrades.SyncedUpgrades.Remove( key );
        }

        factory.GlobalUpgrades = prestigeUpgradesToKeep;

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.GlobalUpgradeChanged, $"Wiped {nonPrestigeKeys.Count} upgrades (kept prestige only)" );
    }
}