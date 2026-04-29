using System.Linq;
using Sandbox;

public sealed class PlayerFactoryActions : Component, Component.INetworkListener
{
    public static PlayerFactoryActions Local { get; private set; }

    protected override void OnAwake()
    {
        if ( !IsProxy )
        {
            Local = this;
        }
    }

    [Rpc.Broadcast]
    public void RpcRequestBuyWeapon( string weaponId )
    {
        if ( !Networking.IsHost ) return;

        var weaponDef = ResourceLibrary.GetAll<WeaponDefinition>().FirstOrDefault( w => w.Id == weaponId );
        if ( weaponDef == null ) return;

        string callerId = Rpc.Caller.SteamId.ToString();

        if ( FactoryStats.Instance != null && FactoryStats.Instance.SpendScrap( weaponDef.UnlockCost ) )
        {
            GameStats.OnMoneySpent( callerId, weaponDef.UnlockCost );

            ItemUnlockSystem.Instance?.UnlockWeapon( weaponId, callerId );

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

        string callerId = Rpc.Caller.SteamId.ToString();

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