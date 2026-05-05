using System.Linq;
using Sandbox;

public sealed class PlayerFactoryActions : Component, Component.INetworkListener
{
    public static PlayerFactoryActions Get( Scene scene )
    {
        return scene.GetAllComponents<PlayerFactoryActions>().FirstOrDefault();
    }

    [Rpc.Broadcast]
    public void RpcRequestBuyWeapon( string weaponId )
    {
        if ( !Networking.IsHost ) return;

        var weaponDef = ResourceLibrary.GetAll<WeaponDefinition>().FirstOrDefault( w => w.Id == weaponId );
        if ( weaponDef == null ) return;

        string callerId = Rpc.Caller.GetUniqueId() ?? Rpc.Caller.SteamId.ToString();

        // Use centralized FactoryStats which handles:
        // 1. Payment validation + execution (SpendScrap)
        // 2. SaveManager updates
        // 3. [Sync] updates
        // 4. ItemUnlockSystem updates
        // 5. GameStats tracking
        // 6. SaveEventBus notifications
        var factoryStats = FactoryStats.Get( Scene );
        if ( factoryStats != null && factoryStats.SpendScrap( weaponDef.UnlockCost ) )
        {
            if ( factoryStats.UnlockWeapon( weaponId, callerId ) )
            {
                Log.Info( $"[PlayerFactoryActions] Weapon purchase successful: {weaponId} for {weaponDef.UnlockCost} scrap" );
            }
        }
        else
        {
            Log.Warning( $"[PlayerFactoryActions] Weapon purchase failed: {weaponId}" );
        }
    }

    [Rpc.Broadcast]
    public void RpcRequestBuyGlobalUpgrade( string upgradeId )
    {
        if ( !Networking.IsHost ) return;

        // string callerId = Rpc.Caller.SteamId.ToString();
        string callerId = Rpc.Caller.GetUniqueId() ?? Rpc.Caller.SteamId.ToString();

        if ( GlobalUpgradesSystem.Instance != null && GlobalUpgradesSystem.Instance.TryPurchaseUpgrade( upgradeId, callerId ) )
        {
            Log.Info( $"[PlayerFactoryActions] Global upgrade purchased: {upgradeId}" );
        }
        else
        {
            Log.Warning( $"[PlayerFactoryActions] Global upgrade purchase failed: {upgradeId}" );
        }
    }

    // NOTE: Prestige upgrades are now purchased through RpcRequestBuyGlobalUpgrade() 
    // which routes to GlobalUpgradesSystem.TryPurchaseUpgrade() for both SkillTree and Prestige types

    [Rpc.Broadcast]
    public void RpcRequestRespec()
    {
        if ( Networking.IsHost ) PrestigeSystem.Instance?.Respec();
    }

    // Tu pourras ajouter d'autres RPC ici plus tard !
    // ex: RpcRequestBuyMachineUpgrade(string machineId, double cost)
}