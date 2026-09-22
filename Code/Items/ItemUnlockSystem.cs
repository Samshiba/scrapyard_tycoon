using Sandbox;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Gère le déblocage des armes et outils
/// </summary>
public sealed class ItemUnlockSystem : Component
{
    [Sync] public NetList<string> UnlockedWeapons { get; private set; } = new();
    [Sync] public NetList<string> UnlockedUtilities { get; private set; } = new();

    public static ItemUnlockSystem Get( Scene scene )
    {
        return scene.GetAllComponents<ItemUnlockSystem>().FirstOrDefault();
    }

    protected override void OnStart()
    {
        if ( !Networking.IsHost ) return;

        _ = LoadUnlockedItemsAsync();
    }

    private async System.Threading.Tasks.Task LoadUnlockedItemsAsync()
    {
        // Wait for SaveManager to load factory data
        while ( !SaveManager.Get( Scene )?.IsFactoryReady ?? true )
        {
            await System.Threading.Tasks.Task.Delay( 50 );
        }

        if ( SaveManager.Get( Scene )?.CurrentFactory != null )
        {
            var factory = SaveManager.Get( Scene ).CurrentFactory;

            if ( factory.UnlockedWeapons != null )
            {
                foreach ( var w in factory.UnlockedWeapons ) UnlockedWeapons.Add( w );
            }

            if ( factory.UnlockedUtilities != null )
            {
                foreach ( var u in factory.UnlockedUtilities ) UnlockedUtilities.Add( u );
            }

            Log.Info( $"[ItemUnlockSystem] Loaded: {UnlockedWeapons.Count} weapons and {UnlockedUtilities.Count} utilities unlocked" );
        }
    }

    public bool IsWeaponUnlocked( string weaponId )
    {
        return UnlockedWeapons.Contains( weaponId );
    }

    public bool IsUtilityUnlocked( string utilityId )
    {
        return UnlockedUtilities.Contains( utilityId );
    }

    public void UnlockWeapon( string weaponId, string steamId )
    {
        if ( !Networking.IsHost ) return;

        if ( !UnlockedWeapons.Contains( weaponId ) )
        {
            UnlockedWeapons.Add( weaponId );
            SaveChanges();
            Log.Info( $"[ItemUnlockSystem] Weapon unlocked: {weaponId}" );
        }
    }

    public void UnlockUtility( string utilityId, string steamId )
    {
        if ( !Networking.IsHost ) return;

        if ( !UnlockedUtilities.Contains( utilityId ) )
        {
            UnlockedUtilities.Add( utilityId );
            SaveChanges();
            Log.Info( $"[ItemUnlockSystem] Utility tool unlocked: {utilityId}" );
        }
    }

    private void SaveChanges()
    {
        if ( !Networking.IsHost || SaveManager.Get( Scene )?.CurrentFactory == null ) return;

        SaveManager.Get( Scene ).CurrentFactory.UnlockedWeapons = [.. UnlockedWeapons];
        SaveManager.Get( Scene ).CurrentFactory.UnlockedUtilities = [.. UnlockedUtilities];

        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ItemUnlocked, $"Weapons: {UnlockedWeapons.Count}, Utilities: {UnlockedUtilities.Count}" );
    }
}
