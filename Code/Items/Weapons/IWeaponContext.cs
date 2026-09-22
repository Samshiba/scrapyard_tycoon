using Sandbox;

/// <summary>
/// Interface que ton futur WeaponComponent exposera à ses stratégies.
/// Cela permet aux stratégies de lire les stats, l'énergie, et la position sans être fortement couplées.
/// </summary>
public interface IWeaponContext
{
    WeaponDefinition Data { get; }
    GameObject Owner { get; }
    Transform AttackTransform { get; }
    GameObject MuzzleObject { get; }

    float GetStat( WeaponStatTarget stat );
    bool ConsumeEnergy();
    bool IsExhausted();
    bool RollCritical();
    string GetPlayerUniqueId();
}