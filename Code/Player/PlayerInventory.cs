using Sandbox;

public sealed class PlayerInventory : Component
{
    [Property] public WeaponDefinition[] EquippedWeapons { get; set; } = new WeaponDefinition[4];
    [Property] public int ActiveSlotIndex { get; set; } = 0;

    private GameObject _activeWeaponObject;

    [Property] public SkinnedModelRenderer PlayerBody { get; set; }

    protected override void OnUpdate()
    {
        if (IsProxy) return;

        if (_activeWeaponObject != null && PlayerBody != null)
        {
            if (PlayerBody.TryGetBoneTransform("hold_R", out var boneTransform))
            {
                _activeWeaponObject.Transform.World = boneTransform;
            }
            else
            {
                Log.Warning("[PlayerInventory] TryGetBoneTransform(\"hold_R\") a échoué ! Vérifier le nom du bone dans le modèle.");
            }
        }

        if (Input.Pressed("Slot1")) EquipSlot(0);
        if (Input.Pressed("Slot2")) EquipSlot(1);
        if (Input.Pressed("Slot3")) EquipSlot(2);
        if (Input.Pressed("Slot4")) EquipSlot(3);
    }

    public void EquipSlot(int index)
    {
        Log.Info(EquippedWeapons);
        Log.Info($"[PlayerInventory] EquipSlot {index} pressed. ActiveSlotIndex: {ActiveSlotIndex}, EquippedWeapons length: {EquippedWeapons.Length}");
        if (index < 0 || index >= EquippedWeapons.Length) return;
        if (EquippedWeapons[index] == null)
        {
            UnequipCurrentWeapon();
            ActiveSlotIndex = index;
            return;
        }
        if (ActiveSlotIndex == index && _activeWeaponObject != null)
        {
            UnequipCurrentWeapon();
            return;
        }

        ActiveSlotIndex = index;
        SpawnWeaponInHand();
    }

    private void UnequipCurrentWeapon()
    {
        if (_activeWeaponObject != null)
        {
            _activeWeaponObject.Destroy();
            _activeWeaponObject = null;
        }
    }

    private void SpawnWeaponInHand()
    {
        if (_activeWeaponObject != null) _activeWeaponObject.Destroy();

        var def = EquippedWeapons[ActiveSlotIndex];
        if (def != null && def.WeaponPrefab != null && PlayerBody != null)
        {
            Log.Info($"[PlayerInventory] Spawn arme : {def.WeaponName}, PlayerBody position : {PlayerBody.Transform.World.Position}");

            _activeWeaponObject = def.WeaponPrefab.Clone(Transform.World);
            _activeWeaponObject.SetParent(GameObject, true);
            Log.Info($"[PlayerInventory] Arme clonée à : {_activeWeaponObject.Transform.World.Position}, parent : {_activeWeaponObject.Parent?.Name}");

            foreach (var rb in _activeWeaponObject.Components.GetAll<Rigidbody>(FindMode.EverythingInSelfAndDescendants))
                rb.Enabled = false;
            foreach (var col in _activeWeaponObject.Components.GetAll<Collider>(FindMode.EverythingInSelfAndDescendants))
                col.Enabled = false;

            var weaponScript = _activeWeaponObject.Components.Get<BaseWeapon>(FindMode.EverythingInSelfAndDescendants);
            if (weaponScript != null)
            {
                weaponScript.Data = def;
                weaponScript.PlayerBody = PlayerBody;
                Log.Info($"[PlayerInventory] WeaponScript trouvé : {weaponScript.GetType().Name}, PlayerBody injecté : {PlayerBody.GameObject.Name}");
            }
            else
            {
                Log.Warning("[PlayerInventory] Aucun BaseWeapon trouvé dans le prefab de l'arme !");
            }

            PlayerBody.Set("holdtype", 4);
            PlayerBody.Set("holdtype_handedness", 1);
        }
        else
        {
            Log.Warning($"[PlayerInventory] SpawnWeaponInHand échoué — def:{def != null}, prefab:{def?.WeaponPrefab != null}, PlayerBody:{PlayerBody != null}");
            if (PlayerBody != null) PlayerBody.Set("holdtype", 0);
        }
    }

    public BaseWeapon ActiveWeapon => _activeWeaponObject?.Components.Get<BaseWeapon>(FindMode.EverythingInSelfAndDescendants);
}