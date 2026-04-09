using Sandbox;
using System.Collections.Generic;

/// <summary>
/// Gère le déblocage des armes et outils
/// </summary>
public sealed class ItemUnlockSystem : Component
{
    public static ItemUnlockSystem Instance { get; private set; }

    private List<string> _unlockedWeapons = new();
    private List<string> _unlockedUtilities = new();

    protected override void OnAwake()
    {
        if ( Instance != null )
        {
            GameObject.Destroy();
            return;
        }
        Instance = this;
        Load();
    }

    private void Load()
    {
        if ( SaveManager.Instance?.Data?.Inventory != null )
        {
            _unlockedWeapons = SaveManager.Instance.Data.Inventory.UnlockedWeapons ?? new();
            _unlockedUtilities = SaveManager.Instance.Data.Inventory.UnlockedUtilities ?? new();
            Log.Info( $"[ItemUnlockSystem] Loaded: {_unlockedWeapons.Count} weapons and {_unlockedUtilities.Count} utilities unlocked" );
        }
    }

    public bool IsWeaponUnlocked( string weaponId )
    {
        return _unlockedWeapons.Contains( weaponId );
    }

    public bool IsUtilityUnlocked( string utilityId )
    {
        return _unlockedUtilities.Contains( utilityId );
    }

    public void UnlockWeapon( string weaponId )
    {
        if ( !Networking.IsHost ) return; // Server-only

        if ( !_unlockedWeapons.Contains( weaponId ) )
        {
            _unlockedWeapons.Add( weaponId );
            SaveChanges();
            Log.Info( $"[ItemUnlockSystem] Weapon unlocked: {weaponId}" );
        }
    }

    public void UnlockUtility( string utilityId )
    {
        if ( !Networking.IsHost ) return; // Server-only

        if ( !_unlockedUtilities.Contains( utilityId ) )
        {
            _unlockedUtilities.Add( utilityId );
            SaveChanges();
            Log.Info( $"[ItemUnlockSystem] Utility tool unlocked: {utilityId}" );
        }
    }

    private void SaveChanges()
    {
        if ( !Networking.IsHost ) return; // Server-only

        if ( SaveManager.Instance != null )
        {
            SaveManager.Instance.Data.Inventory.UnlockedWeapons = _unlockedWeapons;
            SaveManager.Instance.Data.Inventory.UnlockedUtilities = _unlockedUtilities;

            // Notify throttler (major event: unlock triggers immediate save despite throttle)
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ItemUnlocked, $"Weapons: {_unlockedWeapons.Count}, Utilities: {_unlockedUtilities.Count}" );
        }
    }
}
