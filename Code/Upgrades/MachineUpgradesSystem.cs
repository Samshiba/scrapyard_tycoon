using Sandbox;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gère les upgrades des machines de l'usine
/// </summary>
public sealed class MachineUpgradesSystem : Component
{
    public static MachineUpgradesSystem Instance { get; private set; }

    private Dictionary<string, int> Upgrades => SaveManager.Instance?.CurrentFactory?.MachineUpgrades;

    protected override void OnAwake()
    {
        Instance = this;
    }

    public int GetMachineUpgradeLevel( string machineId )
    {
        if ( Upgrades == null ) return 0;
        return Upgrades.GetValueOrDefault( machineId, 0 );
    }

    public void IncreaseMachineUpgrade( string machineId, int amount = 1 )
    {
        if ( !Networking.IsHost || Upgrades == null ) return;

        if ( !Upgrades.ContainsKey( machineId ) )
            Upgrades[machineId] = 0;

        Upgrades[machineId] += amount;

        Log.Info( $"[MachineUpgradesSystem] Machine '{machineId}' upgraded to level {Upgrades[machineId]}" );
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.MachineUpgradeChanged, "Factory machines upgraded" );
    }

    public bool TryPurchaseMachineUpgrade( string machineId, double costInScrap )
    {
        if ( !Networking.IsHost || FactoryStats.Instance == null ) return false;

        if ( FactoryStats.Instance.SpendScrap( costInScrap ) )
        {
            IncreaseMachineUpgrade( machineId );
            return true;
        }
        return false;
    }

    public float GetMachineMultiplier( string machineId, float baseMultiplier = 0.1f )
    {
        int level = GetMachineUpgradeLevel( machineId );
        return 1f + (level * baseMultiplier);
    }
}
