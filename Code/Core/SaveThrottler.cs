using Sandbox;
using System;
using System.Collections.Generic;

/// <summary>
/// Throttles save requests to prevent excessive disk I/O.
/// Queue changes from SaveEventBus and flush every THROTTLE_INTERVAL seconds.
/// Major events (unlock, prestige) bypass throttle and save immediately.
/// </summary>
public class SaveThrottler : Component
{
	public static SaveThrottler Instance { get; private set; }

	private float _timeSinceLastSave = 0f;
	private bool _forceImmediateSave = false;

	// Dirty flags
	private bool _isFactoryDirty = false;
	private HashSet<string> _dirtyPlayers = new();

	protected override void OnAwake()
	{
		Instance = this;
		// Subscribe to save events
		SaveEventBus.OnSaveNeeded += OnSaveNeeded;
	}

	protected override void OnDestroy()
	{
		SaveEventBus.OnSaveNeeded -= OnSaveNeeded;
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
			return;

		_timeSinceLastSave += Time.Delta;

		// Check if we should flush pending changes
		if ( (_isFactoryDirty || _dirtyPlayers.Count > 0) &&
			 (_timeSinceLastSave >= SaveConfig.THROTTLE_INTERVAL || _forceImmediateSave) )
		{
			Log.Info( $"[SaveThrottler] OnFixedUpdate: Flushing! (timeSince={_timeSinceLastSave:F2}s, throttle={SaveConfig.THROTTLE_INTERVAL}s, forceImmediate={_forceImmediateSave})" );

			ForceFlush();
		}
	}

	/// <summary>
	/// Handle incoming save requests from SaveEventBus.
	/// Major events trigger immediate save, others are queued.
	/// </summary>
	private void OnSaveNeeded( SaveEventBus.SaveReason reason, string details, string targetSteamId )
	{
		if ( string.IsNullOrEmpty( targetSteamId ) )
			_isFactoryDirty = true;
		else
			_dirtyPlayers.Add( targetSteamId );

		// Determine if this is a "major event" that should bypass throttle
		bool isMajorEvent = reason switch
		{
			SaveEventBus.SaveReason.PrestigeChanged => true,
			SaveEventBus.SaveReason.PlayerDisconnect => true,
			SaveEventBus.SaveReason.ManualSave => true,
			SaveEventBus.SaveReason.CrashRecovery => true,
			_ => false
		};

		if ( isMajorEvent )
		{
			Log.Info( $"[SaveThrottler] Major event '{reason}' - forcing immediate save" );
			_forceImmediateSave = true;
		}
	}

	/// <summary>
	/// Force an immediate save (used for graceful shutdown, etc).
	/// </summary>
	public void ForceFlush()
	{
		Log.Info( "[SaveThrottler] ForceFlush() called - IMMEDIATELY flushing pending changes" );
		if ( SaveManager.Instance != null )
		{
			SaveManager.Instance.FlushDirtyData( _isFactoryDirty, _dirtyPlayers );
		}

		_isFactoryDirty = false;
		_dirtyPlayers.Clear();
		_timeSinceLastSave = 0f;
		_forceImmediateSave = false;
	}

	/// <summary>
	/// Get seconds until next automatic flush.
	/// </summary>
	public float GetTimeToNextFlush()
	{
		var remaining = SaveConfig.THROTTLE_INTERVAL - _timeSinceLastSave;
		return remaining < 0 ? 0 : remaining;
	}
}
