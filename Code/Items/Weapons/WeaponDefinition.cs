using Sandbox;

public enum DamageType
{
    Physical,
    Magical,
    Electric,
    Poison
}

public enum WeaponCategory
{
    Melee,
    Pistol,
    Rifle,
    Heavy,
    Special
}

public enum WeaponHoldType
{
    None = 0,
    Pistol = 1,
    Rifle = 2,
    Shotgun = 3,
    RPG = 4,
    Melee = 5
}

public static class WeaponHelper
{
    public static string CategoryToLocalizedString( this WeaponCategory category )
    {
        return $"#weapon.category.{category.ToString().ToLower()}";
    }

    public static string DamageTypeToLocalizedString( this DamageType damageType )
    {
        return $"#weapon.damagetype.{damageType.ToString().ToLower()}";
    }
}


[AssetType( Name = "Weapon Definition", Extension = "weapon", Category = "ScrapYard" )]
public partial class WeaponDefinition : GameResource
{
    // --- Identity ---
    [Property, Group( "Identity" )] public string Id { get; set; }
    [Property, Group( "Identity" )] public GameObject WeaponPrefab { get; set; }

    [Group( "Identity" )]
    public string IconPath
    {
        get => $"Weapons/Textures/{Id}.prefab.png";
    }

    // --- Filters ---
    [Property, Group( "Filters" )] public DamageType DamageType { get; set; }
    [Property, Group( "Filters" )] public WeaponCategory Category { get; set; }

    // --- Stats ---
    [Property, Group( "Stats" )] public float DamageBase { get; set; }
    [Property, Group( "Stats" )] public float AttackRateBase { get; set; }
    [Property, Group( "Stats" )] public float CriticalChanceBase { get; set; } = 5.0f;
    [Property, Group( "Stats" )] public float CriticalDamageBase { get; set; } = 2.0f;
    [Property, Group( "Stats" )] public float RangeBase { get; set; }

    // --- Energy System ---
    [Property, Group( "Energy" )] public bool UsesEnergy { get; set; } = true;
    [Property, Group( "Energy" )] public float EnergyCost { get; set; } = 10f;

    // --- Collision & Wall Avoidance ---
    [Property, Group( "Collision" )] public float WeaponLength { get; set; } = 40f;
    [Property, Group( "Collision" )] public float LiftMultiplier { get; set; } = 1.5f;

    // --- AudioVisual ---
    [Property, Group( "Feedback" )] public SoundEvent AttackSound { get; set; }
    [Property, Group( "Feedback" )] public SoundEvent ExhaustionSound { get; set; }
    [Property, Group( "Feedback" )] public GameObject HitEffectPrefab { get; set; }
    [Property, Group( "Feedback" )] public GameObject ProjectilePrefab { get; set; }

    // --- Animation (3rd Person) ---
    [Property, Group( "Animation" )] public string AnimationTriggerName { get; set; }
    [Property, Group( "Animation" )] public WeaponHoldType HoldType { get; set; } = WeaponHoldType.Melee;
    [Property, Group( "Animation" )] public int Handedness { get; set; } = 1; // 1 = Droite, 2 = Deux mains

    // --- Viewmodel (1st Person) ---
    [Property, Group( "Viewmodel" )] public Vector3 LocalHandPosition { get; set; } = Vector3.Zero;
    [Property, Group( "Viewmodel" )] public Angles LocalHandRotation { get; set; } = Angles.Zero;
    [Property, Group( "Viewmodel" )] public string ViewmodelIdleAnim { get; set; } = "idle";
    [Property, Group( "Viewmodel" )] public string ViewmodelFireAnim { get; set; } = "b_fire";

    // --- Economy ---
    [Property, Group( "Economy" )] public double UnlockCost { get; set; }
    [Property, Group( "Economy" )] public int RequiredSpawnerTier { get; set; }

    // --- Localization Keys ---
    [Group( "Localization" )]
    public string LocalizationKeyName
    {
        get => $"#weapon.{RequiredSpawnerTier}.{Category.ToString().ToLower()}.{Id}.name";
    }
    [Group( "Localization" )]
    public string LocalizationKeyDescription
    {
        get => $"#weapon.{RequiredSpawnerTier}.{Category.ToString().ToLower()}.{Id}.description";
    }
}