using Sandbox;
using System.Collections.Generic;

/// <summary>
/// Gère les achats d'upgrades en liant la Sauvegarde (SaveManager), l'Argent (PlayerStats) et le Catalogue (UpgradeManager).
/// </summary>
public sealed class GlobalUpgradesSystem : Component
{
    public static GlobalUpgradesSystem Instance { get; private set; }

    protected override void OnAwake()
    {
        Instance = this;
    }

    // Un "raccourci" propre pour pointer directement vers la vraie sauvegarde
    private Dictionary<string, int> PlayerUpgrades => SaveManager.Instance?.Data?.Player?.GlobalUpgrades;

    public int GetUpgradeLevel( string upgradeId )
    {
        if ( PlayerUpgrades == null ) return 0;

        // S'il n'a pas l'upgrade, on renvoie 0. Sinon, on renvoie son niveau.
        return PlayerUpgrades.GetValueOrDefault( upgradeId, 0 );
    }

    /// <summary>
    /// Tente d'acheter le prochain niveau d'une amélioration.
    /// Ne prend plus le prix en paramètre : il le calcule tout seul via le JSON !
    /// </summary>
    public bool TryPurchaseUpgrade( string upgradeId )
    {
        // 1. Sécurité : Vérifier que tous nos systèmes sont bien chargés
        if ( PlayerUpgrades == null || UpgradeManager.Instance == null || PlayerStats.Local == null )
            return false;

        // 2. Vérifier si l'amélioration existe dans notre fichier JSON
        if ( !UpgradeManager.Instance.Database.TryGetValue( upgradeId, out var node ) )
        {
            Log.Warning( $"Tentative d'achat d'un upgrade inconnu : {upgradeId}" );
            return false;
        }

        int currentLevel = GetUpgradeLevel( upgradeId );

        // 3. Vérifier si on n'a pas déjà maxé cette compétence
        if ( currentLevel >= node.MaxLevel )
        {
            Log.Info( $"❌ Niveau maximum déjà atteint pour {node.Name}." );
            return false;
        }

        // 4. Vérifier les dépendances (L'arbre de compétences)
        if ( !UpgradeManager.Instance.IsNodeUnlocked( upgradeId, SaveManager.Instance.Data ) )
        {
            Log.Info( $"❌ Compétences parentes requises pour débloquer {node.Name}." );
            return false;
        }

        // 5. Calculer le prix du PROCHAIN niveau via la formule du JSON
        float cost = node.GetCostForLevel( currentLevel );

        // 6. Tenter de payer
        if ( PlayerStats.Local.SpendScrap( cost ) )
        {
            // On incrémente le niveau directement dans la sauvegarde globale
            PlayerUpgrades[upgradeId] = currentLevel + 1;

            // On sauvegarde la partie sur le disque !
            SaveManager.Instance.Save();

            Log.Info( $"✅ Achat réussi : {node.Name} (Niveau {currentLevel + 1}) pour {cost} Scrap." );
            return true;
        }

        Log.Info( $"❌ Fonds insuffisants pour {node.Name}. Requis: {cost} Scrap." );
        return false;
    }
}