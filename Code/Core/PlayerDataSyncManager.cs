using Sandbox;

/// <summary>
/// Client-side component for synchronizing player data from server.
/// Clients NEVER modify SaveData directly - they only read from this manager.
/// Updates are received via Synced properties set by server.
/// </summary>
public class PlayerDataSyncManager : Component
{
	public static PlayerDataSyncManager Instance { get; private set; }

	/// <summary>
	/// Local read-only cache of player data (updated from server).
	/// </summary>
	[Sync] public float TotalScrap { get; set; } = 0;
	[Sync] public int GlobalUpgradeLevel { get; set; } = 0; // Example: cache for specific upgrades as needed
	[Sync] public int PrestigeLevel { get; set; } = 0;
	[Sync] public string[] EquippedWeapons { get; set; } = new string[4]; // Synced equipment state
	[Sync] public int ActiveWeaponIndex { get; set; } = 0; // Currently active weapon slot

	protected override void OnAwake()
	{
		Instance = this;

		// Only clients subscribe to updates
		if ( !Networking.IsHost )
		{
			if ( SaveConfig.DEBUG_SAVE_LOGGING )
				Log.Info( "[PlayerDataSyncManager] Initialized on client (read-only mode)" );
		}
	}

	/// <summary>
	/// Get current total scrap (read-only for clients).
	/// </summary>
	public float GetScrap()
	{
		if ( Networking.IsHost )
			return SaveManager.Instance?.Data?.Player?.TotalScrap ?? 0;
		else
			return TotalScrap; // Synced property
	}

	/// <summary>
	/// Get prestige level (read-only for clients).
	/// </summary>
	public int GetPrestigeLevel()
	{
		if ( Networking.IsHost )
			return SaveManager.Instance?.Data?.Prestige?.PrestigeLevel ?? 0;
		else
			return PrestigeLevel; // Synced property
	}

	/// <summary>
	/// Update player scrap value (server only).
	/// Clients cannot call this - it's server authority only.
	/// </summary>
	public void SyncScrap( float value )
	{
		if ( !Networking.IsHost )
			return;

		TotalScrap = value;
		if ( SaveConfig.DEBUG_SAVE_LOGGING )
			Log.Info( $"[PlayerDataSyncManager] Syncing scrap: {value}" );
	}

	/// <summary>
	/// Update prestige level (server only).
	/// </summary>
	public void SyncPrestige( int level )
	{
		if ( !Networking.IsHost )
			return;

		PrestigeLevel = level;
		if ( SaveConfig.DEBUG_SAVE_LOGGING )
			Log.Info( $"[PlayerDataSyncManager] Syncing prestige: {level}" );
	}

	/// <summary>
	/// Sync equipment from server (server-only).
	/// </summary>
	public void SyncEquippedWeapons( string[] weapons, int activeIndex )
	{
		if ( !Networking.IsHost )
			return;

		EquippedWeapons = weapons;
		ActiveWeaponIndex = activeIndex;
		if ( SaveConfig.DEBUG_SAVE_LOGGING )
			Log.Info( $"[PlayerDataSyncManager] Synced equipment: active slot {activeIndex}" );
	}

	/// <summary>
	/// Generic synced property update (used internally).
	/// </summary>
	public void SyncData( string key, int value )
	{
		if ( !Networking.IsHost )
			return;

		// Can be extended for other data types
		if ( key == "prestige" )
			PrestigeLevel = value;
	}
}
