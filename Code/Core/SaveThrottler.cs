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
	private bool _pendingChanges = false;
	private Queue<(SaveEventBus.SaveReason reason, string details)> _changeQueue;
	private bool _forceImmediateSave = false;

	protected override void OnAwake()
	{
		Instance = this;
		_changeQueue = new Queue<(SaveEventBus.SaveReason, string)>();

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
			return; // Only server saves

		_timeSinceLastSave += Time.Delta;

		// Check if we should flush pending changes
		if ( _pendingChanges &&
			(_timeSinceLastSave >= SaveConfig.THROTTLE_INTERVAL || _forceImmediateSave) )
		{
			Log.Info( $"[SaveThrottler] OnFixedUpdate: Flushing! (pending={_pendingChanges}, timeSince={_timeSinceLastSave:F2}s, throttle={SaveConfig.THROTTLE_INTERVAL}s, forceImmediate={_forceImmediateSave})" );
			FlushPendingChanges();
			_timeSinceLastSave = 0f;
			_forceImmediateSave = false;
		}
	}

	/// <summary>
	/// Handle incoming save requests from SaveEventBus.
	/// Major events trigger immediate save, others are queued.
	/// </summary>
	private void OnSaveNeeded( SaveEventBus.SaveReason reason, string details )
	{
		// Queue the change
		_changeQueue.Enqueue( (reason, details) );
		_pendingChanges = true;

		Log.Info( $"[SaveThrottler] OnSaveNeeded: {reason} - {details}. Queue size: {_changeQueue.Count}" );

		// Determine if this is a "major event" that should bypass throttle
		bool isMajorEvent = reason switch
		{
			SaveEventBus.SaveReason.ItemUnlocked => true,
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
		else
		{
			Log.Info( $"[SaveThrottler] Minor event '{reason}' - queuing for throttled save (interval: {SaveConfig.THROTTLE_INTERVAL}s, time since last: {_timeSinceLastSave:F2}s)" );
		}
	}

	/// <summary>
	/// Flush all queued changes by calling SaveManager.Save().
	/// </summary>
	private void FlushPendingChanges()
	{
		if ( !_pendingChanges || _changeQueue.Count == 0 )
		{
			Log.Info( $"[SaveThrottler] FlushPendingChanges: No changes to flush (_pendingChanges={_pendingChanges}, queue={_changeQueue.Count})" );
			return;
		}

		int changeCount = _changeQueue.Count;
		Log.Info( $"[SaveThrottler] FlushPendingChanges: Flushing {changeCount} queued change(s)" );

		while ( _changeQueue.Count > 0 )
		{
			var (reason, details) = _changeQueue.Dequeue();
			Log.Info( $"[SaveThrottler]   - {reason}: {details}" );
		}

		// Trigger actual save in SaveManager
		if ( SaveManager.Instance != null )
		{
			Log.Info( $"[SaveThrottler] Calling SaveManager.Instance.Save()" );
			SaveManager.Instance.Save();
			Log.Info( $"[SaveThrottler] SaveManager.Save() completed" );
		}
		else
		{
			Log.Error( $"[SaveThrottler] SaveManager.Instance is null!" );
		}

		_pendingChanges = false;
	}

	/// <summary>
	/// Force an immediate save (used for graceful shutdown, etc).
	/// </summary>
	public void ForceFlush()
	{
		Log.Info( "[SaveThrottler] ForceFlush() called - IMMEDIATELY flushing pending changes" );
		if ( _pendingChanges && _changeQueue.Count > 0 )
		{
			FlushPendingChanges();
		}
		else
		{
			Log.Info( "[SaveThrottler] ForceFlush() - No pending changes to flush" );
		}
	}

	/// <summary>
	/// Get current number of queued changes (for debugging).
	/// </summary>
	public int GetPendingChangeCount() => _changeQueue.Count;

	/// <summary>
	/// Get seconds until next automatic flush.
	/// </summary>
	public float GetTimeToNextFlush()
	{
		var remaining = SaveConfig.THROTTLE_INTERVAL - _timeSinceLastSave;
		return remaining < 0 ? 0 : remaining;
	}
}
