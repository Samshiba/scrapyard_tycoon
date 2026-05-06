using Sandbox;
using System.Linq;
using System;
using System.Collections.Generic;

public sealed class PlayerInventory : Component
{
    [Property] public WeaponDefinition[] EquippedWeapons { get; set; } = new WeaponDefinition[4];
    [Property] public int ActiveSlotIndex { get; set; } = 0;
    [Property] public SkinnedModelRenderer PlayerBody { get; set; }
    [Property] public ProceduralWeaponHolder WeaponHolder { get; set; }
    [Property] public SkinnedModelRenderer ViewmodelArms { get; set; }

    private GameObject[] _instantiatedWeapons = new GameObject[4];
    private GameObject _activeWeaponObject;
    private int _previousSlotIndex = -1;
    private Dictionary<string, WeaponDefinition> _weaponCache = new();
    private bool _weaponsLoaded = false;

    // private string MySteamId => Connection.Local.SteamId.ToString();
    private string MySteamId => Network.Owner?.GetUniqueId() ?? Connection.Local.SteamId.ToString();

    protected override void OnStart()
    {
        foreach ( var weapon in ResourceLibrary.GetAll<WeaponDefinition>() )
        {
            if ( !string.IsNullOrEmpty( weapon.Id ) ) _weaponCache[weapon.Id] = weapon;
        }

        if ( Networking.IsHost )
        {
            LoadEquippedWeapons();
        }
    }

    private void LoadEquippedWeapons()
    {
        if ( SaveManager.Get( Scene )?.ActivePlayers.TryGetValue( MySteamId, out var pData ) != true ) return;

        var savedWeaponIds = pData.EquippedWeapons;
        if ( savedWeaponIds == null ) return;

        int successCount = 0;
        for ( int i = 0; i < savedWeaponIds.Length && i < EquippedWeapons.Length; i++ )
        {
            if ( string.IsNullOrEmpty( savedWeaponIds[i] ) ) continue;

            if ( _weaponCache.TryGetValue( savedWeaponIds[i], out var weaponDef ) )
            {
                EquippedWeapons[i] = weaponDef;
                successCount++;
            }
        }

        if ( successCount > 0 )
        {
            ActiveSlotIndex = pData.ActiveWeaponIndex;
            EquipSlot( ActiveSlotIndex );
        }

        Log.Info( $"[PlayerInventory] Loaded {successCount} equipped weapons for {MySteamId}" );
    }

    protected override void OnPreRender()
    {
        if ( IsProxy || _activeWeaponObject == null || PlayerBody == null ) return;

        var def = EquippedWeapons[ActiveSlotIndex];

        if ( IsFirstPersonCamera() )
        {
            // 1. Viewmodel arms
            if ( ViewmodelArms != null )
            {
                if ( ViewmodelArms.TryGetBoneTransform( "RightHand", out var boneTransform ) )
                {
                    _activeWeaponObject.Transform.World = boneTransform;

                    if ( def != null )
                    {
                        _activeWeaponObject.WorldRotation *= Rotation.From( def.LocalHandRotation );
                        _activeWeaponObject.WorldPosition += _activeWeaponObject.WorldRotation * def.LocalHandPosition;
                    }
                }
                else
                {
                    Log.Warning( "[PlayerInventory] WARNING: RightHand bone not found on viewmodel arms animation" );
                }
            }
            // 2. Floating weapon holder
            if ( WeaponHolder != null )
            {
                _activeWeaponObject.Transform.World = WeaponHolder.GameObject.Transform.World;

                if ( def != null )
                {
                    _activeWeaponObject.WorldRotation *= Rotation.From( def.LocalHandRotation );
                    _activeWeaponObject.WorldPosition += _activeWeaponObject.WorldRotation * def.LocalHandPosition;
                }
            }

        }
        else
        {
            // 3. Third person bone attach
            if ( PlayerBody.TryGetBoneTransform( "hold_R", out var boneTransform ) )
                _activeWeaponObject.Transform.World = boneTransform;
        }
    }

    protected override void OnUpdate()
    {
        if ( IsProxy ) return;

        if ( PlayerBody != null && Scene.Camera != null )
        {
            var lookPos = Scene.Camera.WorldPosition
                        + Scene.Camera.WorldRotation.Forward * 500f;
            PlayerBody.Set( "aim_eyes", lookPos );
            PlayerBody.Set( "aim_head", lookPos );
            PlayerBody.Set( "aim_body", lookPos );
        }

        if ( Input.Pressed( "Slot1" ) ) EquipSlot( 0 );
        if ( Input.Pressed( "Slot2" ) ) EquipSlot( 1 );
        if ( Input.Pressed( "Slot3" ) ) EquipSlot( 2 );
        if ( Input.Pressed( "Slot4" ) ) EquipSlot( 3 );
    }

    private void RequestEquipSlot( int index )
    {
        if ( index < 0 || index >= EquippedWeapons.Length ) return;

        RpcSetSlot( index );
    }

    [Rpc.Broadcast]
    public void RpcSetSlot( int index )
    {
        if ( EquippedWeapons[index] == null )
        {
            UnequipCurrentWeapon();
            ActiveSlotIndex = index;
        }
        else if ( ActiveSlotIndex == index && _activeWeaponObject != null )
        {
            UnequipCurrentWeapon();
        }
        else
        {
            ActiveSlotIndex = index;
            SpawnWeaponInHand();
        }

        if ( Networking.IsHost ) SaveChanges();
    }

    private void SaveChanges()
    {
        if ( !Networking.IsHost || SaveManager.Get( Scene ) == null ) return;

        var weaponIds = EquippedWeapons.Select( w => w?.Id ).ToArray();

        if ( SaveManager.Get( Scene ).ActivePlayers.TryGetValue( MySteamId, out var pData ) )
        {
            pData.EquippedWeapons = weaponIds;
            pData.ActiveWeaponIndex = ActiveSlotIndex;

            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.WeaponEquipped, $"Slot {ActiveSlotIndex}", MySteamId );
        }
    }

    public void EquipWeapon( WeaponDefinition def, int slotIndex )
    {
        if ( slotIndex < 0 || slotIndex >= EquippedWeapons.Length ) return;

        int existing = Array.IndexOf( EquippedWeapons, def );
        if ( existing >= 0 ) EquippedWeapons[existing] = null;

        EquippedWeapons[slotIndex] = def;
        EquippedWeapons = EquippedWeapons;
        EquipSlot( slotIndex );
    }

    private void UnequipCurrentWeapon()
    {
        if ( _activeWeaponObject != null )
        {
            _activeWeaponObject.Enabled = false;
            _activeWeaponObject = null;
        }

        if ( PlayerBody != null ) PlayerBody.Set( "holdtype", 0 );
        _previousSlotIndex = -1;
    }

    public void EquipSlot( int index )
    {
        if ( index < 0 || index >= EquippedWeapons.Length ) return;

        if ( EquippedWeapons[index] == null )
        {
            UnequipCurrentWeapon();
            ActiveSlotIndex = index;
            SaveChanges();  // Save when unequipping
            return;
        }

        if ( ActiveSlotIndex == index && _activeWeaponObject != null )
        {
            UnequipCurrentWeapon();
            SaveChanges();  // Save when toggling off
            return;
        }

        ActiveSlotIndex = index;
        SpawnWeaponInHand();
        SaveChanges();
    }

    private void SpawnWeaponInHand()
    {
        if ( _previousSlotIndex >= 0 && _previousSlotIndex < _instantiatedWeapons.Length )
        {
            if ( _instantiatedWeapons[_previousSlotIndex] != null )
                _instantiatedWeapons[_previousSlotIndex].Enabled = false;
        }

        var def = EquippedWeapons[ActiveSlotIndex];
        if ( def?.WeaponPrefab == null || PlayerBody == null )
        {
            _activeWeaponObject = null;
            return;
        }

        if ( _instantiatedWeapons[ActiveSlotIndex] != null )
        {
            _activeWeaponObject = _instantiatedWeapons[ActiveSlotIndex];
            _activeWeaponObject.Enabled = true;
        }
        else
        {
            _activeWeaponObject = def.WeaponPrefab.Clone();
            _activeWeaponObject.SetParent( WeaponHolder.GameObject );
            _instantiatedWeapons[ActiveSlotIndex] = _activeWeaponObject;

            foreach ( var rb in _activeWeaponObject.Components
                .GetAll<Rigidbody>( FindMode.EverythingInSelfAndDescendants ) )
                rb.Enabled = false;
            foreach ( var col in _activeWeaponObject.Components
                .GetAll<Collider>( FindMode.EverythingInSelfAndDescendants ) )
                col.Enabled = false;

            var weaponScript = _activeWeaponObject.Components
                .Get<BaseWeapon>( FindMode.EverythingInSelfAndDescendants );
            if ( weaponScript != null )
            {
                weaponScript.Data = def;
                weaponScript.PlayerBody = PlayerBody;
                weaponScript.ViewmodelArms = ViewmodelArms;
            }

            if ( WeaponHolder != null )
            {
                WeaponHolder.WeaponLength = def.WeaponLength;
                WeaponHolder.LiftAngleMultiplier = def.LiftMultiplier;
            }
        }

        PlayerBody.Set( "holdtype", (int)def.HoldType );
        PlayerBody.Set( "holdtype_handedness", def.Handedness );

        _previousSlotIndex = ActiveSlotIndex;
    }

    private bool IsFirstPersonCamera()
    {
        if ( Scene?.Camera == null || WeaponHolder == null ) return true;

        float dist = Vector3.DistanceBetween(
            Scene.Camera.WorldPosition,
            GameObject.WorldPosition
        );
        return dist < WeaponHolder.HideDistance;
    }

    public void ResetEquippedWeaponsForPrestige()
    {
        if ( !Networking.IsHost ) return;

        // Clear all equipped weapons
        for ( int i = 0; i < EquippedWeapons.Length; i++ )
        {
            EquippedWeapons[i] = null;
        }

        // Unequip current weapon
        UnequipCurrentWeapon();

        // Reset to bat weapon in first slot
        if ( _weaponCache.TryGetValue( "bat", out var batWeapon ) )
        {
            EquippedWeapons[0] = batWeapon;
            ActiveSlotIndex = 0;
            EquipSlot( 0 );
        }

        Log.Info( $"[PlayerInventory] Reset equipped weapons for prestige - {MySteamId}" );
    }

    public BaseWeapon ActiveWeapon =>
        _activeWeaponObject?.Components.Get<BaseWeapon>( FindMode.EverythingInSelfAndDescendants );
}
