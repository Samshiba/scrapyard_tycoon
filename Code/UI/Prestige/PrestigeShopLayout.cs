using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

public static class PrestigeShopLayout
{
    // --- PARAMÈTRES VISUELS (À bidouiller pour trouver le look parfait) ---

    // C'est l'angle d'or en radians (environ 137.5 degrés). 
    // C'est le secret mathématique pour que les points ne s'alignent JAMAIS en ligne droite.
    const float GoldenAngle = 2.3999632f;

    // L'espacement de base par rapport au centre pour la toute première amélioration
    const float BaseRadius = 150f;

    // À quel point la spirale s'écarte du centre à chaque nœud
    const float DistanceScale = 180f;

    // Le chaos ! De combien de pixels on peut décaler l'amélioration au hasard (X et Y)
    // Plus la valeur est haute, plus c'est éparpillé et chaotique.
    const float RandomNoiseAmount = 30f;

    public static Dictionary<string, Vector2> Compute( Dictionary<string, UpgradeDefinition> db )
    {
        var positions = new Dictionary<string, Vector2>();
        if ( db == null || db.Count == 0 ) return positions;

        // 1. Le Tri Crucial : On trie toutes les améliorations par leur prix (BaseCost)
        // Ainsi, l'index 0 sera l'amélioration la moins chère (au centre).
        var sortedUpgrades = db.OrderBy( kvp => kvp.Value.BaseCost ).ToList();

        // On utilise Random.Shared pour générer notre "bruit" chaotique
        var random = Random.Shared;

        // 2. Génération de la Constellation Organique
        for ( int i = 0; i < sortedUpgrades.Count; i++ )
        {
            var id = sortedUpgrades[i].Key;

            // --- L'ANGLE ---
            // On multiplie l'index par l'angle d'or. 
            // Ça fait tourner la position autour du centre de manière optimale.
            float angle = i * GoldenAngle;

            // --- LA DISTANCE ---
            // On utilise la racine carrée de l'index (MathF.Sqrt). 
            // C'est ce qui permet d'avoir une densité homogène : 
            // si on utilisait juste "i", ça ferait une spirale en forme de fin tuyau.
            float radius = BaseRadius + DistanceScale * MathF.Sqrt( i );

            // --- CALCUL DE LA POSITION PARFAITE ---
            // Les maths de base du cercle : Cosinus pour X, Sinus pour Y
            float perfectX = MathF.Cos( angle ) * radius;
            float perfectY = MathF.Sin( angle ) * radius;

            // --- LE CHAOS (Éparpillement) ---
            // On génère un nombre entre -1.0 et 1.0, qu'on multiplie par notre paramètre de bruit.
            float noiseX = (random.NextSingle() * 2f - 1f) * RandomNoiseAmount;
            float noiseY = (random.NextSingle() * 2f - 1f) * RandomNoiseAmount;

            // On assigne la position finale (Position Parfaite + Chaos)
            positions[id] = new Vector2( perfectX + noiseX, perfectY + noiseY );
        }

        return positions;
    }
}