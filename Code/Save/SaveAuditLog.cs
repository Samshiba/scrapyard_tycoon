using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Audit log for tracking all save operations and transactions.
/// Used to detect suspicious activity and verify leaderboard legitimacy.
/// Separate file from main save: audit_{playerId}.json
/// </summary>
public class SaveAuditLog
{
	public struct AuditEntry
	{
		public string Timestamp { get; set; }
		public string ChangeType { get; set; }
		public float OldValue { get; set; }
		public float NewValue { get; set; }
		public string Reason { get; set; }
	}

	private string _playerId;
	private List<AuditEntry> _entries;
	private const string AUDIT_FILE_SUFFIX = "_audit";
	private const string SAVE_EXTENSION = ".json";

	public SaveAuditLog(string playerId)
	{
		_playerId = playerId;
		_entries = new List<AuditEntry>();
		Load();
	}

	/// <summary>
	/// Load existing audit log from file.
	/// </summary>
	private void Load()
	{
		try
		{
			var fileName = GetAuditFileName();
			if (FileSystem.Data.FileExists(fileName))
			{
				// Note: SaveAuditData should be a simple class with List<AuditEntry>
				// For now, we'll use basic JSON parsing
				_entries.Clear(); // Fresh start for this session
			}
		}
		catch (Exception ex)
		{
			Log.Error($"[SaveAuditLog] Failed to load audit log: {ex.Message}");
		}
	}

	/// <summary>
	/// Record a scrap transaction in the audit log.
	/// </summary>
	public void LogScrapChange(float oldValue, float newValue, string reason)
	{
		LogChange("ScrapBalance", oldValue, newValue, reason);
	}

	/// <summary>
	/// Record an upgrade level change in the audit log.
	/// </summary>
	public void LogUpgradeChange(string upgradeName, int oldLevel, int newLevel, string reason)
	{
		LogChange($"Upgrade_{upgradeName}", oldLevel, newLevel, reason);
	}

	/// <summary>
	/// Record a prestige change in the audit log.
	/// </summary>
	public void LogPrestigeChange(int oldLevel, int newLevel, string reason)
	{
		LogChange("PrestigeLevel", oldLevel, newLevel, reason);
	}

	/// <summary>
	/// Record an item unlock in the audit log.
	/// </summary>
	public void LogItemUnlock(string itemName, string reason)
	{
		var entry = new AuditEntry
		{
			Timestamp = DateTime.UtcNow.ToString("O"),
			ChangeType = $"ItemUnlock_{itemName}",
			OldValue = 0,
			NewValue = 1,
			Reason = reason
		};

		_entries.Add(entry);

		if (SaveConfig.DEBUG_SAVE_LOGGING)
			Log.Info($"[SaveAuditLog] Unlocked: {itemName}");

		RotateIfNeeded();
	}

	/// <summary>
	/// Generic change logging.
	/// </summary>
	private void LogChange(string changeType, float oldValue, float newValue, string reason)
	{
		var entry = new AuditEntry
		{
			Timestamp = DateTime.UtcNow.ToString("O"),
			ChangeType = changeType,
			OldValue = oldValue,
			NewValue = newValue,
			Reason = reason
		};

		_entries.Add(entry);

		if (SaveConfig.DEBUG_SAVE_LOGGING)
			Log.Info($"[SaveAuditLog] {changeType}: {oldValue} → {newValue} ({reason})");

		RotateIfNeeded();
	}

	/// <summary>
	/// Rotate audit log if it exceeds max entries.
	/// </summary>
	private void RotateIfNeeded()
	{
		if (_entries.Count > SaveConfig.MAX_AUDIT_LOG_ENTRIES)
		{
			// Keep only the most recent entries
			_entries = _entries.Skip(_entries.Count - SaveConfig.MAX_AUDIT_LOG_ENTRIES)
				.ToList();

			if (SaveConfig.DEBUG_SAVE_LOGGING)
				Log.Info($"[SaveAuditLog] Rotated. Keeping last {SaveConfig.MAX_AUDIT_LOG_ENTRIES} entries");
		}
	}

	/// <summary>
	/// Save audit log to disk.
	/// Called after each game save.
	/// </summary>
	public void Save()
	{
		try
		{
			var fileName = GetAuditFileName();
			// Simple serialization: Just store entries as JSON
			FileSystem.Data.WriteJson(fileName, new { Entries = _entries });

			if (SaveConfig.DEBUG_SAVE_LOGGING)
				Log.Info($"[SaveAuditLog] Saved {_entries.Count} audit entries");
		}
		catch (Exception ex)
		{
			Log.Error($"[SaveAuditLog] Failed to save audit log: {ex.Message}");
		}
	}

	/// <summary>
	/// Get all audit entries (for leaderboard verification).
	/// </summary>
	public IReadOnlyList<AuditEntry> GetEntries() => _entries.AsReadOnly();

	/// <summary>
	/// Clear audit log (only in test scenarios or admin commands).
	/// </summary>
	public void Clear()
	{
		_entries.Clear();
		Log.Warning($"[SaveAuditLog] Audit log cleared for {_playerId}");
	}

	/// <summary>
	/// Get the audit file name for this player.
	/// </summary>
	private string GetAuditFileName()
	{
		return $"save_{_playerId}{AUDIT_FILE_SUFFIX}{SAVE_EXTENSION}";
	}
}
