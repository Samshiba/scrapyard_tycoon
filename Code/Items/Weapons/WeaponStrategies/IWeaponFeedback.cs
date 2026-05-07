using Sandbox;

public interface IWeaponFeedback
{
    /// <summary>
    /// Initialise le visuel (sauvegarde la rotation de base, etc.)
    /// </summary>
    void Initialize( GameObject weaponObject );

    /// <summary>
    /// Déclenche le son, le MuzzleFlash et le Kick.
    /// </summary>
    void PlayAttackFeedback( IWeaponContext ctx );

    /// <summary>
    /// Gère le retour à la normale de l'arme (Recovery).
    /// </summary>
    void Update( float deltaTime );
}