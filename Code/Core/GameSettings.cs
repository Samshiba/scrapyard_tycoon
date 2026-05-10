using Sandbox;
using System;
using System.Text.Json.Serialization;

public class GameSettings
{
    public static GameSettings Instance { get; private set; }
    private const string FileName = "user_settings.json";

    [JsonIgnore] public Action OnSettingsChanged;

    // ==========================================
    // CATEGORIES
    // ==========================================
    public GameplaySettings Gameplay { get; set; } = new();
    public PerformanceSettings Performance { get; set; } = new();
    public UISettings UI { get; set; } = new();
    public AudioSettings Audio { get; set; } = new();

    // ==========================================
    // PARAMETERS
    // ==========================================
    public class GameplaySettings
    {
        public bool ShowFloatingDamageNumbers { get; set; } = true; // todo implement
        public bool EnablePropHitFlashes { get; set; } = true;
        public bool EnableDarkPropHitFlashes { get; set; } = false;
        public bool EnableWeaponVFX { get; set; } = true;
    }

    public class PerformanceSettings
    {
        // Gibs settings
        public int MaxGibsCount { get; set; } = 2000;
        public bool EnableGibsAutoDespawn { get; set; } = false;
        public float GibDespawnTime { get; set; } = 120f;
        public bool EnableGibShadows { get; set; } = true;

        // Props settings
        public bool EnablePropShadows { get; set; } = true;
    }

    public class UISettings
    {
        public string CrosshairColor { get; set; } = Color.White.ToString(); // todo add to UI
        public float CrosshairSize { get; set; } = 8.0f;
        public bool ShowEnergyBar { get; set; } = true;
    }

    public class AudioSettings
    {
        public bool EnableWeaponSounds { get; set; } = true;
        public bool EnablePropsSounds { get; set; } = true;
        public bool EnableSellerSounds { get; set; } = true;
    }

    // ==========================================
    // METHODS
    // ==========================================

    public static void Load()
    {
        if ( FileSystem.Data.FileExists( FileName ) )
        {
            Instance = FileSystem.Data.ReadJson<GameSettings>( FileName );
            Log.Info( "[Settings] Loaded user settings from file." );
        }
        else
        {
            Instance = new GameSettings();
            Log.Info( "[Settings] No settings file found, using default settings." );
        }
    }

    public static void Save()
    {
        FileSystem.Data.WriteJson( FileName, Instance );
        Log.Info( "[Settings] Settings saved to file." );

        Instance.OnSettingsChanged?.Invoke();
    }
}