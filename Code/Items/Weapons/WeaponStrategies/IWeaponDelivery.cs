public interface IWeaponDelivery
{
    /// <summary>
    /// Appelé par le Trigger quand il décide qu'il est temps de faire des dégâts.
    /// </summary>
    void Execute( IWeaponContext ctx );
}