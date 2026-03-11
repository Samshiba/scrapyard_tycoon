using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public static class SkillTreeLayout
{
    const float GridSizeX = 200f; // Espacement généreux pour tes tooltips massifs
    const float GridSizeY = 200f;

    // Les 4 directions d'expansion strictes (Droite, Gauche, Haut, Bas)
    static readonly (int x, int y)[] Directions = {
        (1, 0),
        (-1, 0),
        (0, -1),
        (0, 1)
    };

    public static Dictionary<string, Vector2> Compute( Dictionary<string, UpgradeNode> db )
    {
        var positions = new Dictionary<string, Vector2>();
        if ( db == null || db.Count == 0 ) return positions;

        // Échiquier pour vérifier les cases occupées
        var usedSlots = new HashSet<(int x, int y)>();
        var gridPositions = new Dictionary<string, (int x, int y)>();

        // File d'attente (BFS) : NoeudId, Position parent, Direction privilégiée
        var queue = new Queue<(string Id, (int x, int y) ParentPos, (int x, int y) PrefDir)>();

        // 1. Trouver la/les racines (noeuds sans prérequis)
        var roots = db.Where( kvp => kvp.Value.Requirements.Count == 0 ).Select( kvp => kvp.Key ).ToList();
        if ( roots.Count == 0 ) roots.Add( db.Keys.First() ); // Sécurité anti-boucle

        (int x, int y) currentRootPos = (0, 0);
        foreach ( var rootId in roots )
        {
            // Trouver une place pour le Root (au cas où il y en a plusieurs)
            while ( usedSlots.Contains( currentRootPos ) ) currentRootPos = (currentRootPos.x, currentRootPos.y + 1);

            gridPositions[rootId] = currentRootPos;
            usedSlots.Add( currentRootPos );

            // On lance la croissance dans la direction par défaut (Droite)
            EnqueueChildren( rootId, currentRootPos, Directions[0], db, gridPositions, queue );
            currentRootPos = (currentRootPos.x + 2, currentRootPos.y);
        }

        // 2. Faire grandir l'arbre
        while ( queue.Count > 0 )
        {
            var item = queue.Dequeue();
            if ( gridPositions.ContainsKey( item.Id ) ) continue; // Déjà placé via un autre parent

            // Chercher la meilleure place libre
            var bestPos = FindFreeSlot( item.ParentPos, item.PrefDir, usedSlots );

            gridPositions[item.Id] = bestPos;
            usedSlots.Add( bestPos );

            // Calculer la vraie direction qu'on a prise pour la donner aux enfants
            var newDir = (bestPos.x - item.ParentPos.x, bestPos.y - item.ParentPos.y);
            newDir = (Math.Sign( newDir.Item1 ), Math.Sign( newDir.Item2 )); // Normalisation
            if ( newDir.Item1 == 0 && newDir.Item2 == 0 ) newDir = item.PrefDir;

            EnqueueChildren( item.Id, bestPos, newDir, db, gridPositions, queue );
        }

        // 3. Convertir l'échiquier (cases) en Vrais Pixels
        foreach ( var kvp in gridPositions )
        {
            // Grâce à ça, le Root(0,0) sera positionné exactement à (0,0) en pixels.
            // La vue de ta caméra (PanOffset) tombera donc naturellement dessus à l'ouverture !
            positions[kvp.Key] = new Vector2( kvp.Value.x * GridSizeX, kvp.Value.y * GridSizeY );
        }

        return positions;
    }

    // --- Fonctions utilitaires internes ---

    static void EnqueueChildren( string parentId, (int x, int y) parentPos, (int x, int y) baseDir,
                                 Dictionary<string, UpgradeNode> db,
                                 Dictionary<string, (int x, int y)> gridPositions,
                                 Queue<(string, (int x, int y), (int x, int y))> queue )
    {
        // On récupère les enfants non placés
        var children = db.Where( kvp => kvp.Value.Requirements.Any( r => r.RequiredId == parentId ) )
                         .Select( kvp => kvp.Key )
                         .Where( id => !gridPositions.ContainsKey( id ) )
                         .ToList();

        // On crée un éventail de directions (Devant, Gauche, Droite, Derrière)
        var searchDirs = GetOrthogonalSpread( baseDir );

        for ( int i = 0; i < children.Count; i++ )
        {
            // Le 1er enfant va tout droit, le 2ème à gauche, etc.
            var dir = searchDirs[i % searchDirs.Length];
            queue.Enqueue( (children[i], parentPos, dir) );
        }
    }

    static (int x, int y)[] GetOrthogonalSpread( (int x, int y) preferred )
    {
        // Si je vais à droite (1,0) -> Ma gauche est (0,-1), ma droite est (0,1)
        var left = (preferred.y, -preferred.x);
        var right = (-preferred.y, preferred.x);
        var back = (-preferred.x, -preferred.y);
        return new[] { preferred, left, right, back }; // Ordre de priorité !
    }

    static (int x, int y) FindFreeSlot( (int x, int y) origin, (int x, int y) prefDir, HashSet<(int x, int y)> usedSlots )
    {
        int distance = 1;
        while ( distance < 50 ) // Sécurité anti-boucle infinie
        {
            var dirs = GetOrthogonalSpread( prefDir );
            foreach ( var dir in dirs )
            {
                var testPos = (origin.x + dir.x * distance, origin.y + dir.y * distance);
                if ( !usedSlots.Contains( testPos ) )
                    return testPos;
            }
            distance++; // Si toute la couronne est bouchée, on regarde plus loin
        }
        return (origin.x + prefDir.x * distance, origin.y + prefDir.y * distance);
    }
}