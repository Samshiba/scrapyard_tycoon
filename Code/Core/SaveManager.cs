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
    }

    public void Save()
    {
        try
        {
            FileSystem.Data.WriteJson( SaveFileName, Data );
            Log.Info( "💾 Partie sauvegardée avec succès !" );
        }
        catch ( Exception e )
        {
            Log.Error( $"Erreur lors de la sauvegarde : {e.Message}" );
        }
    }

    public void Load()
    {
        if ( FileSystem.Data.FileExists( SaveFileName ) )
        {
            try
            {
                Data = FileSystem.Data.ReadJson<GameSaveData>( SaveFileName );
                Log.Info( "📂 Partie chargée !" );
            }
            catch ( Exception e )
            {
                Log.Error( $"Erreur de lecture de sauvegarde : {e.Message}. Création d'une nouvelle." );
                Data = new GameSaveData();
            }
        }
        else
        {
            Log.Info( "🆕 Nouvelle partie." );
            Data = new GameSaveData();
        }
    }
}