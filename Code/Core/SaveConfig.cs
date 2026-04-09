using Sandbox;

/// <summary>
/// Centralized configuration for SaveManager throttling, validation, and limits.
/// Modify these constants to tune save behavior.
/// </summary>
public static class SaveConfig
{
	/// <summary>
	/// Maximum time (seconds) between saves. SaveThrottler will flush pending changes after this interval.
	/// Default: 30 seconds (balanced for I/O and data freshness)
	/// </summary>
	public const float THROTTLE_INTERVAL = 30f;

	/// <summary>
	/// Scrap value that triggers immediate save (bypass throttle).
	/// e.g., unlocking a weapon with >1000 scrap earned triggers instant save
	/// </summary>
	public const float MAJOR_EVENT_SCRAP_THRESHOLD = 1000f;

	/// <summary>
	/// Maximum scrap value any player can have. Prevents exploitation via manual JSON editing.
	/// Adjust based on game balance.
	/// </summary>
	public const float MAX_SCRAP = 999_999_999f;

	/// <summary>
	/// Minimum scrap value (prevent negative balances).
	/// </summary>
	public const float MIN_SCRAP = 0f;

	/// <summary>
	/// Maximum upgrade level per upgrade. Prevents malformed saves.
	/// </summary>
	public const int MAX_UPGRADE_LEVEL = 1000;

	/// <summary>
	/// Maximum prestige level achievable.
	/// </summary>
	public const int MAX_PRESTIGE_LEVEL = 100;

	/// <summary>
	/// Maximum items in player backpack (before upgrades).
	/// </summary>
	public const int MAX_BACKPACK_ITEMS = 300;

	/// <summary>
	/// Number of weapon slots available.
	/// </summary>
	public const int WEAPON_SLOTS = 4;

	/// <summary>
	/// Number of backup save files to keep for recovery.
	/// </summary>
	public const int BACKUP_ROTATION_COUNT = 3;

	/// <summary>
	/// Maximum entries in audit log before rotation.
	/// </summary>
	public const int MAX_AUDIT_LOG_ENTRIES = 1000;

	/// <summary>
	/// Enable detailed save logging (set false in production for performance).
	/// </summary>
	public const bool DEBUG_SAVE_LOGGING = true;
}
