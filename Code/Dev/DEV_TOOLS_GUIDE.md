# Dev Tools for Save System Debugging

## Quick Start

### Console Hotkeys (Add to game input controller)
- **F12**: Print entire save data to console
- **F11**: Add 10,000 test scrap
- **F10**: Print debug logging status
- **CTRL+F12**: Reset save to defaults

### Manual Dev Calls (from code or console)
```csharp
// From any script or console:
var inspector = new DevSaveInspector();

// Print save data
inspector.PrintSave();

// Add test scrap
inspector.AddTestScrap( 5000 );

// Manually trigger save
inspector.ManualSave();

// Check throttler status
inspector.PrintThrottlerStatus();

// Set specific amounts (for edge case testing)
inspector.SetScrapDebug( 1000 );
inspector.AddUpgradeDebug( "upgrade_id", 5 );
inspector.UnlockWeaponDebug( "weapon_id" );

// Export save as JSON
inspector.ExportSaveAsJson();
```

---

## Debugging Upgrade Purchase Failure

### Symptoms
- "Cannot buy upgrade" even with sufficient scrap
- No error in logs
- Purchase button doesn't respond

### Troubleshooting Checklist

**1. Verify SaveManager is initialized**
```csharp
Log.Info( $"SaveManager.Instance: {SaveManager.Instance}" );
Log.Info( $"SaveManager.Instance.Data: {SaveManager.Instance?.Data}" );
Log.Info( $"SaveManager.Instance.Data.Player: {SaveManager.Instance?.Data?.Player}" );
Log.Info( $"SaveManager.Instance.Data.Player.GlobalUpgrades: {SaveManager.Instance?.Data?.Player?.GlobalUpgrades}" );
```

**2. Check PlayerStats balance**
```csharp
Log.Info( $"PlayerStats.Local.TotalScrap: {PlayerStats.Local?.TotalScrap ?? -1}" );
Log.Info( $"SaveManager.Instance.Data.Player.TotalScrap: {SaveManager.Instance?.Data?.Player?.TotalScrap ?? -1}" );
```

**3. Verify upgrade exists in database**
```csharp
var exists = UpgradeManager.Instance.Database.TryGetValue( upgradeId, out var node );
Log.Info( $"Upgrade {upgradeId} exists: {exists}" );
if ( exists )
    Log.Info( $"  Name: {node.Name}, MaxLevel: {node.MaxLevel}" );
```

**4. Check upgrade level**
```csharp
var currentLevel = GlobalUpgradesSystem.Instance.GetUpgradeLevel( upgradeId );
Log.Info( $"Current level: {currentLevel}" );
```

**5. Verify parent upgrade requirements**
```csharp
var unlocked = UpgradeManager.Instance.IsNodeUnlocked( upgradeId, SaveManager.Instance.Data );
Log.Info( $"Upgrade unlocked (parents satisfied): {unlocked}" );
```

**6. Force a save and check persistence**
```csharp
// Trigger save immediately
SaveEventBus.NotifyChange( SaveEventBus.SaveReason.ManualSave, "DEBUG ForceSave" );
SaveManager.Instance.Save();

// Check if SaveManager.Data was updated
Log.Info( $"After save - GlobalUpgrades: {SaveManager.Instance.Data.Player.GlobalUpgrades}" );
```

---

## Accessing Save Files

### Location
```
S&Box Data Folder:
%APPDATA%\Sandbox\data\

Save file pattern:
save_{steamId}.json
save_{steamId}_audit.json  (audit log)
save_{steamId}_backup_{date}_{time}.json
```

### View Raw JSON
```csharp
var inspector = new DevSaveInspector();
inspector.ExportSaveAsJson();
// Copy output to text editor, validate JSON
```

### Edit Save Files Manually
1. **Backup first** - copy current save file
2. **Edit JSON** - use VS Code + JSON formatter
3. **Save file** 
4. **Restart game** - SaveValidator will validate on load

### Common Manual Edits (for testing)
```json
// Add scrap
"TotalScrap": 99999,

// Add upgrade
"GlobalUpgrades": {
  "upgrade_id_1": 5,
  "upgrade_id_2": 2
}

// Unlock weapon
"UnlockedWeapons": ["rifle_a", "shotgun_b"]
```

---

## Throttler Status

### Check What's Queued
```csharp
Log.Info( $"Pending changes in queue: {SaveThrottler.Instance.GetPendingChangeCount()}" );
Log.Info( $"Seconds until next save: {SaveThrottler.Instance.GetTimeToNextFlush():F2}" );
```

### Force Immediate Save
```csharp
SaveThrottler.Instance.ForceFlush();
// Executes SaveManager.Save() immediately, clearing queue
```

### Verify Batching Working
1. Trigger multiple upgrades quickly
2. Check logs for "Flushing N queued changes"
3. Should see only 1-2 file writes instead of N

---

## Validator Debugging

### Test Bounds Checking
```csharp
// These should reject/clamp:
SaveValidator.ClampScrap( -500 );           // Should return 0
SaveValidator.ClampScrap( 9999999999 );     // Should clamp to MAX_SCRAP
SaveValidator.ClampUpgradeLevel( -1 );      // Should return 0
SaveValidator.ClampUpgradeLevel( 2000 );    // Should clamp to MAX_UPGRADE_LEVEL
```

### Test Full Validation
```csharp
var result = SaveValidator.ValidateFull( SaveManager.Instance.Data );
if ( !result.IsValid )
{
    Log.Error( $"Validation failed: {result.ErrorMessage}" );
}
```

### Test Weapon Resolution
```csharp
bool isValid = SaveValidator.IsWeaponIdValid( "rifle_a" );
Log.Info( $"Weapon ID 'rifle_a' is valid: {isValid}" );
```

---

## Audit Log Inspection

### View Recent Transactions
```csharp
var auditLog = SaveManager.Instance.GetAuditLog();
foreach ( var entry in auditLog.GetEntries() )
{
    Log.Info( $"{entry.Timestamp}: {entry.ChangeType} " +
        $"{entry.OldValue} → {entry.NewValue} ({entry.Reason})" );
}
```

### Detect Suspicious Activity
```csharp
// Look for impossible transitions:
// - Scrap: value < 0 or > MAX_SCRAP
// - Prestige: jumped multiple levels instantly
// - Upgrades: level changed by >1 in single transaction
```

---

## Recovery Mode

### If Save Corrupted
1. **Check backups exist**: Look for `save_{steamId}_backup_*.json` files
2. **Recovery automatic**: SaveRecovery attempts restore on load
3. **Check logs**: Search for "[SaveRecovery]" entries
4. **Manual recovery**: Delete corrupted save, restart game (starts fresh)

### If Save Lost
1. **Check backup rotation**: Last 3 backups kept
2. **Check FileSystem.Data folder**: Ensure not deleted
3. **Contact**: Check if developer has backup of user's save

---

## Dev Console Commands (Future)

```
// Add these to your dev console if you have one:
/save_print          - Print entire save data
/save_scrap <amount> - Set scrap to amount
/save_upgrade <id> <lvl> - Set upgrade level
/save_flush          - Force save immediately
/save_reset          - Reset to defaults
```

---

## Checklist Before Release

- [ ] SaveConfig.DEBUG_SAVE_LOGGING = false (production)
- [ ] Verify no leftover Dev Components in scenes
- [ ] Remove or hide DevMenu hotkeys
- [ ] Test upgrade purchasing works end-to-end
- [ ] Verify save files created with correct per-player isolation
- [ ] Manual save edits are rejected by validator
- [ ] Crash/kill test → recovery from backup works

---

## Performance Profiling

### Measure Save Time
```csharp
var sw = System.Diagnostics.Stopwatch.StartNew();
SaveManager.Instance.Save();
sw.Stop();
Log.Info( $"Save took {sw.ElapsedMilliseconds}ms" );
```

### Monitor I/O
```csharp
// With DEBUG_SAVE_LOGGING enabled, each save prints:
// "[SaveManager] Save SUCCESS: save_12345.json (45ms)"
//
// Track these to ensure saves < 10ms typical
```

### Check Queue Efficiency
```csharp
// Log shows:
// "[SaveThrottler] Flushing 23 queued changes..."
//
// This means 23 systems called NotifyChange(), only 1 disk write
// Efficiency = 23x reduction in I/O
```
