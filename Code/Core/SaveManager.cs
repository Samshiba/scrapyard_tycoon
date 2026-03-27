using Sandbox;
using System.Text.Json;
using System;

public sealed class SaveManager : Component
{
    public static SaveManager Instance { get; private set; }

    public GameSaveData Data { get; private set; }
    private const string SaveFileName = "scrapyard_tycoon_save.json";

    protected override void OnAwake()
    {
        Instance = this;
        Load();
        
        // Initialiser le système de localisation avec la langue sauvegardée
        LocalizationManager.Initialize( Data.CurrentLanguage );
    }

    public void Save()
    {
        try
        {
            // Store current language setting
            Data.CurrentLanguage = LocalizationManager.GetLanguage();
            
            FileSystem.Data.WriteJson( SaveFileName, Data );
            Log.Info( "[SaveManager] Game saved successfully" );
        }
        catch ( Exception e )
        {
            Log.Error( $"[SaveManager] ERROR: Failed to save game - {e.Message}" );
        }
    }

    public void Load()
    {
        if ( FileSystem.Data.FileExists( SaveFileName ) )
        {
            try
            {
                Data = FileSystem.Data.ReadJson<GameSaveData>( SaveFileName );
                Log.Info( "[SaveManager] Save file loaded successfully" );
            }
            catch ( Exception e )
            {
                Log.Error( $"[SaveManager] ERROR: Failed to read save file - {e.Message}. Creating new save." );
                Data = new GameSaveData();
            }
        }
        else
        {
            Log.Info( "[SaveManager] No existing save file found. Starting new game." );
            Data = new GameSaveData();
        }
    }
}