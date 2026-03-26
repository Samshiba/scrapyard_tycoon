using Sandbox;
using System.Linq;
using System;
using System.Collections.Generic;

public sealed class PlayerInventory : Component
{
    public static PlayerInventory Local { get; private set; }

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

    protected override void OnAwake()
    {
        Local = this;
    }

    protected override void OnStart()
    {
        var allWeapons = ResourceLibrary.GetAll<WeaponDefinition>();
        foreach ( var weapon in allWeapons )
        {
            if ( !string.IsNullOrEmpty( weapon.Id ) )
                _weaponCache[weapon.Id] = weapon;
        }
        Log.Info( $"✅ {_weaponCache.Count} arme(s) chargée(s)" );

        LoadEquippedWeapons();
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
                    Log.Warning( "⚠️ OS NON TROUVÉ" );
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

    private void LoadEquippedWeapons()
    {
        if ( IsProxy || SaveManager.Instance?.Data?.Inventory == null ) return;

        var savedWeaponIds = SaveManager.Instance.Data.Inventory.EquippedWeapons;
        if ( savedWeaponIds == null ) return;

        int successCount = 0, failedCount = 0;
        for ( int i = 0; i < savedWeaponIds.Length && i < EquippedWeapons.Length; i++ )
        {
            if ( string.IsNullOrEmpty( savedWeaponIds[i] ) ) continue;
            if ( _weaponCache.TryGetValue( savedWeaponIds[i], out var weaponDef ) )
            { EquippedWeapons[i] = weaponDef; successCount++; }
            else
            { Log.Warning( $"⚠️ Arme non trouvée : '{savedWeaponIds[i]}' slot {i}" ); failedCount++; }
        }

        if ( successCount > 0 )
        {
            ActiveSlotIndex = SaveManager.Instance.Data.Inventory.ActiveWeaponIndex;
            Log.Info( $"✅ {successCount} armes OK, {failedCount} manquantes, slot actif {ActiveSlotIndex}" );
            _weaponsLoaded = true;
            EquipSlot( ActiveSlotIndex );
        }
        else
        {
            Log.Error( "❌ Aucune arme chargée" );
            _weaponsLoaded = true;
        }
    }

    private void SaveChanges()
    {
        if ( IsProxy || SaveManager.Instance == null ) return;

        var weaponIds = new string[EquippedWeapons.Length];
        for ( int i = 0; i < EquippedWeapons.Length; i++ )
            weaponIds[i] = EquippedWeapons[i]?.Id;

        SaveManager.Instance.Data.Inventory.EquippedWeapons = weaponIds;
        SaveManager.Instance.Data.Inventory.ActiveWeaponIndex = ActiveSlotIndex;
        SaveManager.Instance.Save();
    }

    public void EquipWeapon( WeaponDefinition def, int slotIndex )
    {
        if ( slotIndex < 0 || slotIndex >= EquippedWeapons.Length ) return;

        int existing = Array.IndexOf( EquippedWeapons, def );
        if ( existing >= 0 ) EquippedWeapons[existing] = null;

        EquippedWeapons[slotIndex] = def;
        EquipSlot( ActiveSlotIndex );
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
            return;
        }

        if ( ActiveSlotIndex == index && _activeWeaponObject != null )
        {
            UnequipCurrentWeapon();
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

    public BaseWeapon ActiveWeapon =>
        _activeWeaponObject?.Components.Get<BaseWeapon>( FindMode.EverythingInSelfAndDescendants );
}