using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public static class SkillTreeLayout
{
    // --- PARAMÈTRES VISUELS ---
    // C'est ici que tu règles la taille de ton arbre
    const float NodeDistance = 250f; // La distance en pixels entre un noeud et ses enfants
    const float MaxBranchSpread = MathF.PI * 0.7f; // Ouverture max d'une branche (ici ~126° pour éviter que ça reparte en arrière)

    public static Dictionary<string, Vector2> Compute( Dictionary<string, UpgradeDefinition> db )
    {
        var positions = new Dictionary<string, Vector2>();
        if ( db == null || db.Count == 0 ) return positions;

        // 1. Trouver le Root (le noeud central sans prérequis)
        var roots = db.Where( kvp => kvp.Value.RequiredUpgrades == null || kvp.Value.RequiredUpgrades.Count == 0 )
                      .Select( kvp => kvp.Key )
                      .ToList();

        if ( roots.Count == 0 ) roots.Add( db.Keys.First() ); // Sécurité anti-crash

        var visited = new HashSet<string>();

        // 2. Lancer la génération depuis le centre (0,0)
        // Le Root a droit à un cercle complet de 360° (soit 2 * PI en radians)
        float rootOffsetX = 0f;
        foreach ( var rootId in roots )
        {
            PlaceNodeRadial( rootId, new Vector2( rootOffsetX, 0 ), 0f, MathF.PI * 2f, db, positions, visited );

            // Si jamais tu as plusieurs arbres indépendants, on les écarte de 2000 pixels
            rootOffsetX += 2000f;
        }

        return positions;
    }

    static void PlaceNodeRadial( string nodeId, Vector2 position, float angleCenter, float angleSweep,
                                 Dictionary<string, UpgradeDefinition> db,
                                 Dictionary<string, Vector2> positions,
                                 HashSet<string> visited )
    {
        // Éviter les boucles infinies si deux noeuds se requièrent mutuellement
        if ( visited.Contains( nodeId ) ) return;
        visited.Add( nodeId );
        positions[nodeId] = position;

        // 3. Récupérer les enfants directs de ce noeud
        var children = db.Where( kvp => kvp.Value.RequiredUpgrades != null &&
                                        kvp.Value.RequiredUpgrades.Any( r => r.RequiredUpgrade.Id == nodeId ) )
                         .Select( kvp => kvp.Key )
                         .Where( id => !positions.ContainsKey( id ) )
                         .ToList();

        if ( children.Count == 0 ) return; // Fin de cette branche

        // 4. Découper "l'espace angulaire" disponible pour les enfants
        float angleStep = angleSweep / children.Count;

        // On calcule l'angle de départ pour que les enfants soient centrés par rapport au parent
        float currentAngle = angleCenter;

        // Si on n'est pas sur le Root (qui a 360°), on ajuste l'angle de départ
        // pour que le cône pointe "vers l'extérieur" et ne tourne pas sur lui-même
        if ( angleSweep < MathF.PI * 1.9f )
        {
            currentAngle = angleCenter - (angleSweep / 2f) + (angleStep / 2f);
        }

        // 5. Placer chaque enfant en cercle
        for ( int i = 0; i < children.Count; i++ )
        {
            // Les mathématiques de base pour tracer un cercle :
            // X = Cos(angle) * Rayon
            // Y = Sin(angle) * Rayon
            float childX = position.x + MathF.Cos( currentAngle ) * NodeDistance;
            float childY = position.y + MathF.Sin( currentAngle ) * NodeDistance;

            // La "part de gâteau" accordée aux enfants de cet enfant
            // On limite avec MaxBranchSpread pour éviter que les sous-branches s'entremêlent
            float childSweep = MathF.Min( angleStep, MaxBranchSpread );

            // Appel récursif pour faire pousser l'arbre
            PlaceNodeRadial( children[i], new Vector2( childX, childY ), currentAngle, childSweep, db, positions, visited );

            currentAngle += angleStep; // On passe au point suivant sur le cercle
        }
    }
}