# 🔴 CRITICAL BUG FIX PLAN - Online Save System

## SEVERITY: 🔴 CRITICAL
Your entire weapon system stopped working when you went online-first. The root cause is **client-side modifications to server-only SaveData** without RPC validation.

---

## ERROR MESSAGES EXPLAINED

### ❌ `[SaveManager] Validation FAILED: EquippedWeapons[1] '9mm' cannot be resolved from ResourceLibrary`
**What's happening:**
1. Client added weapon ID "9mm" to SaveData
2. Server tried to validate it 30 seconds later
3. `ResourceLibrary.Get<WeaponDefinition>("9mm")` returned null
4. Validation failed, save rejected, weapon lost

**Why it happens:**
- Client modifies SaveData directly without server checking first
- Weapon ID "9mm" might not be the actual weapon ID (check your resource definition)

### ❌ `[PlayerInventory] ERROR: No weapons loaded. Inventory is empty.`
**What's happening:**
1. Player joins server
2. SaveManager starts loading from disk (async)
3. Immediately: PlayerInventory.OnStart() tries to read SaveData
4. SaveData still empty or being loaded
5. Weapon array is empty, so nothing loads

---

## ROOT CAUSES (6 CRITICAL ISSUES)

### 🔥 Issue #1: Client Directly Modifies SaveData (MOST CRITICAL)
**Location:** [WeaponShopUI.razor](Code/UI/Shop/WeaponShopUI.razor#L253)
```csharp
// ❌ WRONG - Line 253
private void BuyWeapon(WeaponDefinition weapon)
{
    if ( weapon == null ) return;
    if ( PlayerStats.Local.SpendScrap( weapon.UnlockCost ) )
    {
        SaveManager.Instance.Data.Inventory.UnlockedWeapons.Add( weapon.Id ); // ❌ CLIENT MODIFIES SERVER DATA
        SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ItemUnlocked, $"Weapon: {weapon.WeaponName}" );
        StateHasChanged();
    }
}
```

**Problem:** 
- SaveManager is server-only, but clients access `.Instance.Data` directly
- No server validation before modification
- Weapon ID might be wrong or not in ResourceLibrary

**Impact:**
- Save validation fails (weapon ID invalid)
- Weapon not persisted to disk
- Desync: client thinks weapon is owned, server doesn't have it

---

### 🔥 Issue #2: No RPC for Purchase Validation
**Missing:** No server-side purchase handler
```csharp
// ✅ MISSING - Should exist on server component
[NetMulticast]
public void RpcPurchaseWeapon(string weaponId) 
{
    // Not implemented!
}
```

**Problem:**
- Client can "cheat" by editing weapon IDs or bypassing scrap check
- No server coordination if multiple clients send requests
- Server never validates the transaction

**Impact:**
- Exploitable (players can give themselves weapons)
- No cheat detection or audit trail
- Weapons lost on save validation failure

---

### 🔥 Issue #3: Race Condition on Player Load
**Location:** [GameState.cs](Code/Core/GameState.cs) + [PlayerInventory.cs](Code/Player/PlayerInventory.cs#L106)

```
Frame N: GameState.OnActive() called
  ├─ SaveManager.Load() → reads from disk (takes time)
  └─ SpawnPlayerForConnection()
     └─ PlayerInventory.OnStart() runs
        └─ Tries to read SaveManager.Data.Inventory
           └─ Data might still be empty!
```

**Problem:**
- SaveData load from disk can take a few frames
- PlayerInventory immediately tries to read in OnStart()
- Gets empty array or partial data

**Impact:**
- "No weapons loaded" error
- Weapon slots stay empty even after reload
- Player can't equip anything

---

### 🔥 Issue #4: Weapon ID Mismatch in ResourceLibrary
**Possible causes:**
1. WeaponDefinition has wrong `Id` property
2. "9mm" doesn't match actual resource ID (e.g., "weapon_9mm")
3. Weapon not added to ResourceLibrary yet

**Check:**
- Are all weapons properly registered in resource library?
- Is weapon `Id` field set correctly?
- Do weapon IDs in UnlockedWeapons match ResourceLibrary IDs?

**Impact:**
- Validation rejects saves containing invalid weapon IDs
- Weapons lost on next save attempt

---

### 🔥 Issue #5: Client Weapon Equipment Not Synced
**Location:** [PlayerInventory.cs](Code/Player/PlayerInventory.cs#L139-L150)

```csharp
private void SaveChanges()
{
    if ( IsProxy || SaveManager.Instance == null ) return;

    var weaponIds = new string[EquippedWeapons.Length];
    for ( int i = 0; i < EquippedWeapons.Length; i++ )
        weaponIds[i] = EquippedWeapons[i]?.Id;

    SaveManager.Instance.Data.Inventory.EquippedWeapons = weaponIds; // ❌ CLIENT MODIFIES SERVER DATA
    SaveManager.Instance.Data.Inventory.ActiveWeaponIndex = ActiveSlotIndex;

    SaveEventBus.NotifyChange( SaveEventBus.SaveReason.WeaponEquipped, $"Slot {ActiveSlotIndex}: {weaponName}" );
}
```

**Problem:**
- Client modifies equipped weapons directly
- No server validation
- Changes don't sync back to client (no [Sync] properties)
- Other clients don't see equipment changes

**Impact:**
- Equipment state inconsistent between client/server
- No UI feedback if save fails
- Hotbar icons don't update properly

---

### 🔥 Issue #6: No Network Sync of SaveData
**Problem:** SaveData is read directly from singleton, not synced via network

```csharp
// Current approach (broken):
if ( SaveManager.Instance?.Data?.Inventory != null ) // ❌ Direct access
{
    TotalScrap = SaveManager.Instance.Data.Player.TotalScrap;
}

// Should use [Sync] properties instead (like PlayerDataSyncManager)
[Sync] public float TotalScrap { get; set; } = 0; // ✅ Network synced
```

**Impact:**
- Clients don't get updates when server changes data
- Save validation failures not communicated to client UI
- Cursor/UI issues due to inconsistent state

---

## FIX PLAN (12 Steps)

### Step 1: Create Server-Side Purchase Handler
**File:** Create new [Code/Upgrades/TerminalShopInteractable.cs](Code/Upgrades/TerminalShopInteractable.cs)

Add RPC method that validates purchase on server:
```csharp
[NetMulticast] // Called by client, handled on server
public void RpcPurchaseWeapon( string weaponId )
{
    if ( !Networking.IsHost ) return; // Ignore on clients
    
    var weaponDef = ResourceLibrary.Get<WeaponDefinition>( weaponId );
    if ( weaponDef == null ) 
    {
        Log.Error( $"[Shop] Weapon ID not found: {weaponId}" );
        return; // Invalid weapon
    }
    
    if ( PlayerStats.Local == null ) return;
    
    // Validate player has enough scrap
    if ( !PlayerStats.Local.SpendScrap( weaponDef.UnlockCost ) )
    {
        Log.Warning( $"[Shop] Player cannot afford weapon: {weaponId}" );
        return; // Not enough scrap
    }
    
    // Add to SaveData on server
    if ( SaveManager.Instance?.Data?.Inventory != null )
    {
        if ( !SaveManager.Instance.Data.Inventory.UnlockedWeapons.Contains( weaponId ) )
        {
            SaveManager.Instance.Data.Inventory.UnlockedWeapons.Add( weaponId );
            Log.Info( $"[Shop] Weapon purchased: {weaponId}" );
            
            SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ItemUnlocked, $"Weapon: {weaponDef.WeaponName}" );
        }
    }
}
```

### Step 2: Fix WeaponShopUI to Call RPC Instead
**File:** [Code/UI/Shop/WeaponShopUI.razor](Code/UI/Shop/WeaponShopUI.razor#L253)

Change BuyWeapon() to send RPC:
```csharp
private void BuyWeapon(WeaponDefinition weapon)
{
    if ( weapon == null ) return;
    
    // ✅ Send to server for validation instead of modifying directly
    GameManager.Instance?.RpcPurchaseWeapon( weapon.Id );
    
    StateHasChanged();
}
```

**Why:** Server validates scrap, player, weapon existence before accepting

### Step 3: Add Wait for SaveData on Player Spawn
**File:** [Code/Player/PlayerInventory.cs](Code/Player/PlayerInventory.cs#L30-L40)

Add delay to ensure SaveData is loaded:
```csharp
protected override void OnStart()
{
    var allWeapons = ResourceLibrary.GetAll<WeaponDefinition>();
    foreach ( var weapon in allWeapons )
    {
        if ( !string.IsNullOrEmpty( weapon.Id ) )
            _weaponCache[weapon.Id] = weapon;
    }
    Log.Info( $"[PlayerInventory] Weapon cache loaded: {_weaponCache.Count} weapon definition(s)" );

    // ✅ Give SaveData time to load from disk
    // This is a workaround - proper fix is async loading
    if ( SaveManager.Instance?.Data?.Inventory != null )
    {
        LoadEquippedWeapons();
    }
    else
    {
        Log.Warning( "[PlayerInventory] SaveData not ready yet, will retry next frame" );
        // Retry loading next frame
    }
}
```

### Step 4: Fix Race Condition Properly
**File:** [Code/Core/SaveManager.cs](Code/Core/SaveManager.cs#L50-L70)

Add ready flag for SaveData:
```csharp
public bool IsDataReady { get; private set; } = false;

public void SetPlayerConnection( Connection connection )
{
    _currentConnection = connection;
    var playerId = GetPlayerId( connection );
    _recovery = new SaveRecovery( playerId );
    _auditLog = new SaveAuditLog( playerId );

    Load();
    IsDataReady = true; // ✅ Signal that data is loaded
    
    Log.Info( $"[SaveManager] Player {playerId} loaded (save: {GetSaveFileName()})" );
}
```

**File:** [Code/Player/PlayerInventory.cs](Code/Player/PlayerInventory.cs#L37)

Use the ready flag:
```csharp
protected override void OnStart()
{
    // ... cache building code ...
    
    // ✅ Wait for SaveManager to signal data is ready
    // For now, add to next update check:
}

protected override void OnUpdate()
{
    if ( !_weaponsLoaded && SaveManager.Instance?.IsDataReady == true )
    {
        LoadEquippedWeapons();
    }
    
    // ... rest of update code ...
}
```

### Step 5: Verify Weapon IDs Match ResourceLibrary
**File:** Verify in editor + Dev Tools

Check that:
1. WeaponDefinition.Id matches file name (e.g., "9mm" vs "weapon_9mm")
2. Weapon is registered in ResourceLibrary
3. No typos in saved weapon IDs

**Dev Tool to Check:**
```csharp
// Add to DevMenu.cs
public static void CheckWeaponIds()
{
    var weapons = ResourceLibrary.GetAll<WeaponDefinition>();
    Log.Info( "=== WEAPON IDS ===" );
    foreach ( var w in weapons )
    {
        Log.Info( $"ID: '{w.Id}' | Name: {w.WeaponName}" );
    }
    
    if ( SaveManager.Instance?.Data?.Inventory != null )
    {
        Log.Info( "=== UNLOCKED WEAPONS ===" );
        foreach ( var id in SaveManager.Instance.Data.Inventory.UnlockedWeapons )
        {
            var found = weapons.FirstOrDefault( w => w.Id == id );
            Log.Info( $"ID: '{id}' | Found: {found?.WeaponName ?? "❌ NOT FOUND"}" );
        }
    }
}
```

### Step 6: Add [Sync] Properties for SaveData
**File:** [Code/Core/PlayerDataSyncManager.cs](Code/Core/PlayerDataSyncManager.cs)

Expand synced properties to include equipment:
```csharp
[Sync] public float TotalScrap { get; set; } = 0;
[Sync] public int GlobalUpgradeLevel { get; set; } = 0;
[Sync] public int PrestigeLevel { get; set; } = 0;
[Sync] public string[] EquippedWeapons { get; set; } = new string[4]; // ✅ NEW
[Sync] public int ActiveWeaponIndex { get; set; } = 0; // ✅ NEW

public void SyncEquippedWeapons( string[] weapons, int activeIndex )
{
    if ( !Networking.IsHost ) return;
    EquippedWeapons = weapons;
    ActiveWeaponIndex = activeIndex;
}
```

### Step 7: Fix Weapon Equipment Saving
**File:** [Code/Player/PlayerInventory.cs](Code/Player/PlayerInventory.cs#L139-L150)

Don't modify SaveData directly, use RPC:
```csharp
private void SaveChanges()
{
    if ( IsProxy || SaveManager.Instance == null ) return;

    var weaponIds = new string[EquippedWeapons.Length];
    for ( int i = 0; i < EquippedWeapons.Length; i++ )
        weaponIds[i] = EquippedWeapons[i]?.Id;

    // ✅ Only on server, modify SaveData
    if ( Networking.IsHost )
    {
        SaveManager.Instance.Data.Inventory.EquippedWeapons = weaponIds;
        SaveManager.Instance.Data.Inventory.ActiveWeaponIndex = ActiveSlotIndex;
    }
    else
    {
        // ✅ On client, send RPC to server
        RpcEquipWeapons( weaponIds, ActiveSlotIndex );
    }

    var weaponName = EquippedWeapons[ActiveSlotIndex]?.WeaponName ?? "empty";
    SaveEventBus.NotifyChange( SaveEventBus.SaveReason.WeaponEquipped, $"Slot {ActiveSlotIndex}: {weaponName}" );
}

[NetMulticast]
private void RpcEquipWeapons( string[] weaponIds, int activeIndex )
{
    if ( !Networking.IsHost ) return;
    
    if ( SaveManager.Instance?.Data?.Inventory != null )
    {
        SaveManager.Instance.Data.Inventory.EquippedWeapons = weaponIds;
        SaveManager.Instance.Data.Inventory.ActiveWeaponIndex = activeIndex;
    }
}
```

### Step 8: Add Error Feedback to UI
**File:** [Code/UI/Shop/WeaponShopUI.razor](Code/UI/Shop/WeaponShopUI.razor)

Show save status:
```csharp
private string _lastError = "";
private TimeSince _errorClearTime = 10f;

protected override void OnAwake()
{
    Local = this;
    LoadWeapons();
    
    // Subscribe to save failures
    SaveEventBus.OnSaveFailed += (error) =>
    {
        _lastError = error;
        _errorClearTime = 0;
        StateHasChanged();
    };
}

// In UI, show error if validation failed:
@if ( !string.IsNullOrEmpty( _lastError ) && _errorClearTime < 3f )
{
    <div class="error-message">❌ Save Failed: @_lastError</div>
}
```

### Step 9: Verify Cursor Issue Fix
**Root Cause:** Likely fixed by fixing weapon loading

When weapons load properly, UI state will be consistent and cursor issues should disappear. If not, check:
- Mouse input event handlers
- Canvas/UI layer visibility
- Focus handling in UI components

### Step 10: Add Comprehensive Logging
**File:** [Code/Core/SaveValidator.cs](Code/Core/SaveValidator.cs#L130)

Add more details to weapon validation:
```csharp
// Validate all equipped weapon IDs can be resolved
for ( int i = 0; i < inventory.EquippedWeapons.Length; i++ )
{
    if ( !string.IsNullOrEmpty( inventory.EquippedWeapons[i] ) )
    {
        var weaponDef = ResourceLibrary.Get<WeaponDefinition>( inventory.EquippedWeapons[i] );
        if ( weaponDef == null )
        {
            // ✅ Better error message
            var available = string.Join(", ", ResourceLibrary.GetAll<WeaponDefinition>().Select(w => w.Id));
            return ValidationResult.Failure(
                $"EquippedWeapons[{i}] '{inventory.EquippedWeapons[i]}' not found. Available: [{available}]"
            );
        }
    }
}
```

### Step 11: Test Save Persistence
**File:** [Code/Dev/DevMenu.cs](Code/Dev/DevMenu.cs)

Add test commands:
```csharp
public static void TestWeaponSystem()
{
    if ( !Networking.IsHost )
    {
        Log.Warning( "[DevMenu] Only works on server" );
        return;
    }
    
    Log.Info( "=== WEAPON SYSTEM TEST ===" );
    
    // 1. Check weapons load
    var inventory = SaveManager.Instance?.Data?.Inventory;
    Log.Info( $"Unlocked weapons: {inventory?.UnlockedWeapons.Count ?? 0}" );
    
    // 2. Check weapon IDs
    foreach ( var id in inventory?.UnlockedWeapons ?? new() )
    {
        var w = ResourceLibrary.Get<WeaponDefinition>( id );
        Log.Info( $"  {id}: {(w != null ? "✅ FOUND" : "❌ NOT FOUND")}" );
    }
    
    // 3. Try save
    SaveManager.Instance.Save();
}
```

### Step 12: Monitor SaveEventBus on Client
**File:** Add monitoring code

```csharp
protected override void OnAwake()
{
    // ...
    
    SaveEventBus.OnSaveFailed += (error) =>
    {
        Log.Error( $"[Shop] Save failed: {error}" );
        // Update UI to show error
    };
    
    SaveEventBus.OnSaveComplete += (ms) =>
    {
        if ( SaveConfig.DEBUG_SAVE_LOGGING )
            Log.Info( $"[Shop] Save completed in {ms}ms" );
    };
}
```

---

## PRIORITY ORDER

**Do these in order:**
1. ✅ Step 5: Verify weapon IDs (5 min) - **DO THIS FIRST**
2. ✅ Step 1: Create RPC handler (20 min)
3. ✅ Step 2: Fix WeaponShopUI (10 min)
4. ✅ Step 4: Add SaveData ready flag (15 min)
5. ✅ Step 6: Add [Sync] properties (15 min)
6. ✅ Step 7: Fix equipment saving (15 min)
7. ✅ Step 10: Better logging (5 min)
8. ✅ Test everything (15 min)

---

## QUICK DEBUGGING

**To find the '9mm' weapon ID issue:**
```
1. Open DevMenu (press `)
2. Call CheckWeaponIds() (new method in Step 5)
3. See which IDs are "NOT FOUND"
4. Update weapon definition IDs to match ResourceLibrary
5. Delete old save files to clear cache
6. Restart game
```

**To test weapon purchase:**
```
1. dev_manual_save
2. Press E on shop terminal
3. Buy a weapon
4. Check logs for "Validation FAILED" or "Weapon purchased"
5. Close/reopen to see if weapon persisted
```

---

## EXPECTED RESULTS AFTER FIXES

✅ Weapon purchases persist to disk  
✅ Weapons load correctly on spawn  
✅ Equipment stays synced between client/server  
✅ Weapon icons visible in hotbar  
✅ Cursor visible in shop/skill tree  
✅ No validation errors on save  
✅ All "ERROR: No weapons loaded" messages gone  

