public interface IWeaponTrigger
{
    /// <summary>
    /// Appelé à chaque frame. Retourne "true" si l'arme doit déclencher une attaque ce frame.
    /// </summary>
    bool Update( IWeaponContext ctx, bool inputPressed, bool inputDown, bool inputReleased );

    /// <summary>
    /// Permet d'injecter la méthode de tir (Delivery) et de feedback (Recul).
    /// </summary>
    void BindActions( System.Action onFireEvent );
}