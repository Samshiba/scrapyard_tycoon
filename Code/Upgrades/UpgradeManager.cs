using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class UpgradeManager : Component
{
    public static UpgradeManager Instance { get; private set; }

    // On stocke ça dans un Dictionnaire pour trouver un upgrade en 0.001ms via son ID
    [Property] public Dictionary<string, UpgradeNode> Database { get; private set; } = new();

    protected override void OnAwake()
    {
        Instance = this;
        LoadDatabase();
    }

    private void LoadDatabase()
    {
        // On lit le fichier depuis les assets montés du jeu
        if ( FileSystem.Mounted.FileExists( "data/upgrades.json" ) )
        {
            var graph = FileSystem.Mounted.ReadJson<UpgradeGraphData>( "data/upgrades.json" );

            // On convertit la liste en dictionnaire pour la vitesse
            Database = graph.Nodes.ToDictionary( node => node.Id );

            Log.Info( $"📚 Base de données chargée : {Database.Count} améliorations." );
        }
        else
        {
            Log.Error( "Fichier data/upgrades.json introuvable !" );
        }
    }

    // --- FONCTION UTILITAIRE POUR TON FUTUR TERMINAL ---

    // Vérifie si un upgrade est débloquable (si le joueur a les bons parents)
    public bool IsNodeUnlocked( string upgradeId, GameSaveData playerSave )
    {
        if ( !Database.TryGetValue( upgradeId, out var node ) ) return false;

        foreach ( var req in node.Requirements )
        {
            // On vérifie dans la sauvegarde du joueur s'il a le niveau requis
            int playerLevel = playerSave.Player.GlobalUpgrades.GetValueOrDefault( req.RequiredId, 0 );
            if ( playerLevel < req.RequiredLevel )
            {
                return false; // Il manque un parent !
            }
        }
        return true; // Tous les parents sont validés
    }
}