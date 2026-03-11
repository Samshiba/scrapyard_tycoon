using Sandbox;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gère les upgrades des machines de l'usine
/// </summary>
public sealed class MachineUpgradesSystem : Component
{
    public static MachineUpgradesSystem Instance { get; private set; }

    private Dictionary<string, int> _machineUpgrades = new();

    protected override void OnAwake()
    {
        Instance = this;
    }

    private void Load()
    {
        if ( SaveManager.Instance?.Data?.Factory != null )
        {
            _machineUpgrades = SaveManager.Instance.Data.Factory.MachineUpgrades ?? new();
            Log.Info( $"🏭 {_machineUpgrades.Count} upgrades machines chargés" );
        }
    }

    public int GetMachineUpgradeLevel( string machineId )
    {
        return _machineUpgrades.ContainsKey( machineId ) ? _machineUpgrades[machineId] : 0;
    }

    public void IncreaseMachineUpgrade( string machineId, int amount = 1 )
    {
        if ( !_machineUpgrades.ContainsKey( machineId ) )
            _machineUpgrades[machineId] = 0;

        _machineUpgrades[machineId] += amount;
        SaveChanges();
        Log.Info( $"🔧 Machine {machineId} → Niveau {_machineUpgrades[machineId]}" );
    }

    public bool TryPurchaseMachineUpgrade( string machineId, float costInScrap )
    {
        var playerStats = Scene.GetAllComponents<PlayerStats>().FirstOrDefault();
        if ( playerStats == null ) return false;

        if ( playerStats.SpendScrap( costInScrap ) )
        {
            IncreaseMachineUpgrade( machineId );
            return true;
        }
        return false;
    }

    /// <summary>
    /// Obtient un multiplicateur basé sur le niveau d'upgrade de la machine
    /// Exemple : niveau 3 → 1.3x
    /// </summary>
    public float GetMachineMultiplier( string machineId, float baseMultiplier = 0.1f )
    {
        int level = GetMachineUpgradeLevel( machineId );
        return 1f + (level * baseMultiplier);
    }

    private void SaveChanges()
    {
        if ( SaveManager.Instance != null )
        {
            SaveManager.Instance.Data.Factory.MachineUpgrades = _machineUpgrades;
            SaveManager.Instance.Save();
        }
    }
}
