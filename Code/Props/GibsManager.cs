using Sandbox;
using System.Collections.Generic;

/// <summary>
/// Centralized manager for tracking and controlling active gibs in the scene.
/// Enforces MaxGibsCount setting and provides gib statistics.
/// </summary>
public class GibsManager : Component
{
    public static GibsManager Instance { get; private set; }
    private List<ResourceGib> _activeGibs = new();

    protected override void OnAwake()
    {
        if ( Instance == null )
        {
            Instance = this;
        }
        else if ( Instance != this )
        {
            Log.Warning( "[GibsManager] Multiple GibsManager instances detected. Destroying duplicate." );
            GameObject.Destroy();
        }
    }

    protected override void OnDestroy()
    {
        if ( Instance == this )
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Gets the current number of active gibs in the scene.
    /// </summary>
    public int CurrentGibCount => _activeGibs.Count;

    /// <summary>
    /// Gets the maximum allowed gibs from settings.
    /// </summary>
    public int MaxGibsCount => GameSettings.Instance?.Performance.MaxGibsCount ?? 2000;

    /// <summary>
    /// Checks if a new gib can be spawned based on the MaxGibsCount setting.
    /// </summary>
    public bool CanSpawnGib()
    {
        if ( GameSettings.Instance == null ) return true;

        return CurrentGibCount < MaxGibsCount;
    }

    /// <summary>
    /// Registers a newly spawned gib to track it.
    /// Called by ResourceGib when it spawns.
    /// </summary>
    public void RegisterGib( ResourceGib gib )
    {
        if ( gib != null && !_activeGibs.Contains( gib ) )
        {
            _activeGibs.Add( gib );
        }
    }

    /// <summary>
    /// Unregisters a gib when it's destroyed.
    /// Called by ResourceGib when it's destroyed.
    /// </summary>
    public void UnregisterGib( ResourceGib gib )
    {
        _activeGibs.Remove( gib );
    }

    public void ClearAllGibs()
    {
        foreach ( var gib in _activeGibs )
        {
            if ( gib != null && gib.IsValid )
            {
                gib.GameObject.Destroy();
            }
        }
        _activeGibs.Clear();
    }
}
