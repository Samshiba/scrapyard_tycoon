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

        // string callerId = Rpc.Caller.SteamId.ToString();
        string callerId = Rpc.Caller.GetUniqueId() ?? Rpc.Caller.SteamId.ToString();

        if ( FactoryStats.Get( Scene ) != null && FactoryStats.Get( Scene ).SpendScrap( weaponDef.UnlockCost ) )
        {
            GameStats.OnMoneySpent( Scene, callerId, weaponDef.UnlockCost );

            ItemUnlockSystem.Get( Scene )?.UnlockWeapon( weaponId, callerId );

            Log.Info( $"[PlayerFactoryActions] Weapon purchase successful: {weaponId} for {weaponDef.UnlockCost} scrap" );
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

    // Tu pourras ajouter d'autres RPC ici plus tard !
    // ex: RpcRequestBuyMachineUpgrade(string machineId, double cost)
}