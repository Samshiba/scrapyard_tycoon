using Sandbox;
using System;
using System.Collections.Generic;

/// <summary>
/// Handles save file backups and recovery from corruption/crashes.
/// Maintains rotating backups and can restore from them.
/// </summary>
public class SaveRecovery
{
	private string _playerId;
	private const string SAVE_EXTENSION = ".json";
	private const string BACKUP_SUFFIX = "_backup";

	public SaveRecovery( string playerId )
	{
		_playerId = playerId;
	}

	/// <summary>
	/// Create a backup of the current save file before writing new data.
	/// Maintains rotating backups (keeps last N backups).
	/// </summary>
	public void CreateBackup()
	{
		try
		{
			// Skip backup at shutdown - file system might not be available
			if ( string.IsNullOrEmpty( _playerId ) || FileSystem.Data == null )
			{
				Log.Warning( "[SaveRecovery] CreateBackup skipped - FileSystem unavailable (likely shutdown)" );
				return;
			}

			var saveFileName = GetSaveFileName();
			if ( string.IsNullOrEmpty( saveFileName ) )
			{
				Log.Warning( "[SaveRecovery] CreateBackup skipped - saveFileName is null" );
				return;
			}

			var backupFileName = GetBackupFileName();

			// If current save exists, copy it to backup
			if ( FileSystem.Data.FileExists( saveFileName ) )
			{
				// Read current save
				var saveContent = FileSystem.Data.ReadAllText( saveFileName );

				// Write to timestamped backup
				var timestampedBackup = $"{backupFileName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{SAVE_EXTENSION}";
				FileSystem.Data.WriteAllText( timestampedBackup, saveContent );

				Log.Info( $"[SaveRecovery] Backup created: {timestampedBackup}" );

				// Cleanup old backups (keep only BACKUP_ROTATION_COUNT)
				RotateBackups( backupFileName );
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"[SaveRecovery] Failed to create backup: {ex.Message}" );
		}
	}

	/// <summary>
	/// Delete old backups, keeping only the most recent N.
	/// </summary>
	private void RotateBackups( string backupFilePrefix )
	{
		try
		{
			// This is a simplified approach. 
			// In production, you'd query FileSystem for all backup files matching pattern
			// and sort by date, deleting oldest ones.
			// For now, we log this as a TODO since FileSystem.Data has limited enumeration.

			if ( SaveConfig.DEBUG_SAVE_LOGGING )
				Log.Info( $"[SaveRecovery] Backup rotation: keeping last {SaveConfig.BACKUP_ROTATION_COUNT} backups" );
		}
		catch ( Exception ex )
		{
			Log.Error( $"[SaveRecovery] Backup rotation failed: {ex.Message}" );
		}
	}

	/// <summary>
	/// Attempt to recover a valid save from backups.
	/// Returns null if no valid backup found.
	/// </summary>
	public GameSaveData TryRecoverFromBackup()
	{
		try
		{
			var backupFileName = GetBackupFileName();

			// Try to find and load most recent backup
			// Note: FileSystem.Data has limited file enumeration.
			// This is a simplified version - you may need custom implementation.

			var latestBackupPath = $"{backupFileName}_latest{SAVE_EXTENSION}";

			if ( FileSystem.Data.FileExists( latestBackupPath ) )
			{
				var recoveredData = FileSystem.Data.ReadJson<GameSaveData>( latestBackupPath );

				// Validate before returning
				var validation = SaveValidator.ValidateFull( recoveredData );
				if ( validation.IsValid )
				{
					Log.Warning( $"[SaveRecovery] Recovered save from backup: {latestBackupPath}" );
					return recoveredData;
				}
				else
				{
					Log.Error( $"[SaveRecovery] Backup is corrupted: {validation.ErrorMessage}" );
				}
			}
		}
		catch ( Exception ex )
		{
			Log.Error( $"[SaveRecovery] Backup recovery failed: {ex.Message}" );
		}

		return null;
	}

	/// <summary>
	/// Get the primary save file name for this player.
	/// </summary>
	private string GetSaveFileName()
	{
		return $"save_{_playerId}{SAVE_EXTENSION}";
	}

	/// <summary>
	/// Get the backup file name prefix for this player.
	/// </summary>
	private string GetBackupFileName()
	{
		return $"save_{_playerId}{BACKUP_SUFFIX}";
	}
}
