using System.Collections.Generic;
using System.Text;

/// <summary>
/// Structure représentant une étape de calcul dans la formule
/// </summary>
public struct FormulaComponent
{
    public string Label { get; set; }
    public float Value { get; set; }
    public string Type { get; set; } // "base", "additive", "multiplicative", "compound"
}

/// <summary>
/// Classe représentant une formule complète avec ses étapes de calcul
/// </summary>
public class WeaponStatFormula
{
    public float BaseValue { get; set; }
    public float AdditiveBonus { get; set; }
    public float MultiplicativeBonus { get; set; }
    public float CompoundMultiplier { get; set; }
    public float FinalValue { get; set; }

    /// <summary>
    /// Génère une formule mathématique propre et lisible (Standard UX ARPG).
    /// Ex: "(100 + 20) × 1.15 × 1.05 = 144.9" ou juste "100 × 1.15 = 115"
    /// </summary>
    public string GetCleanFormulaString( int decimalPlaces = 1 )
    {
        var parts = new List<string>();
        string format = $"F{decimalPlaces}";

        // 1. Gérer la Base et l'Additive avec des parenthèses (pour la priorité mathématique)
        if ( System.MathF.Abs( AdditiveBonus ) > 0.001f )
        {
            string sign = AdditiveBonus > 0 ? "+" : "-";
            string additiveVal = System.MathF.Abs( AdditiveBonus ).ToString( format );

            // Les parenthèses sont cruciales pour que le joueur comprenne que l'addition se fait AVANT la multiplication
            parts.Add( $"({BaseValue.ToString( format )} {sign} {additiveVal})" );
        }
        else
        {
            // Pas de bonus plat ? On affiche juste la base sans parenthèses
            parts.Add( BaseValue.ToString( format ) );
        }

        // 2. Gérer les Multiplicateurs (×)
        if ( System.MathF.Abs( MultiplicativeBonus ) > 0.001f )
        {
            float mult = 1f + MultiplicativeBonus;
            parts.Add( $"× {mult.ToString( format )}" );
        }

        // 3. Gérer le Multiplicateur Exponentiel (Compound)
        if ( System.MathF.Abs( CompoundMultiplier - 1f ) > 0.001f )
        {
            parts.Add( $"× {CompoundMultiplier.ToString( format )}" );
        }

        // S'il n'y a eu AUCUN bonus, on retourne juste la valeur finale
        if ( System.MathF.Abs( AdditiveBonus ) <= 0.001f && System.MathF.Abs( MultiplicativeBonus ) <= 0.001f && System.MathF.Abs( CompoundMultiplier - 1f ) <= 0.001f )
        {
            return FinalValue.ToString( format );
        }

        // 4. Assemblage final avec un seul signe égal
        string formulaLeft = string.Join( " ", parts );
        return $"{formulaLeft} = {FinalValue.ToString( format )}";
    }

    /// <summary>
    /// Retourne un breakdown détaillé des modificateurs
    /// </summary>
    public string GetDetailedBreakdown()
    {
        var sb = new StringBuilder();

        sb.AppendLine( $"Base: {BaseValue}" );

        if ( AdditiveBonus != 0 )
            sb.AppendLine( $"Additive Bonus: {(AdditiveBonus > 0 ? "+" : "")}{AdditiveBonus.ToString( "F1" )}" );

        float afterAdditive = BaseValue + AdditiveBonus;

        if ( MultiplicativeBonus != 0 )
        {
            float multiplier = 1f + MultiplicativeBonus;
            float afterMult = afterAdditive * multiplier;
            sb.AppendLine( $"Multiplicative: ×{multiplier.ToString( "F2" )} ({afterAdditive} × {multiplier.ToString( "F2" )} = {afterMult.ToString( "F1" )})" );
        }

        if ( CompoundMultiplier != 1f )
        {
            float beforeCompound = (BaseValue + AdditiveBonus) * (1f + MultiplicativeBonus);
            float afterCompound = beforeCompound * CompoundMultiplier;
            sb.AppendLine( $"Compound: ×{CompoundMultiplier.ToString( "F2" )} ({beforeCompound.ToString( "F1" )} × {CompoundMultiplier.ToString( "F2" )} = {afterCompound.ToString( "F1" )})" );
        }

        sb.AppendLine( $"Final: {FinalValue.ToString( "F1" )}" );

        return sb.ToString();
    }
}
