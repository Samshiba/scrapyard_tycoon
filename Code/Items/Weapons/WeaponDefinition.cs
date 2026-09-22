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

public enum TriggerBehavior
{
    SemiAuto,   // Tire une fois par clic (Pistolet, Pompe classique)
    FullAuto,   // Tire en boucle tant qu'on maintient (Fusil d'assaut)
    Burst,      // Tire X balles par clic (Fusil à rafale)
    Continuous  // Actif tant qu'on maintient (Laser, Lance-flammes, Tournevis)
}

public enum DeliveryBehavior
{
    Hitscan,    // Rayon instantané
    Projectile, // Instancie un objet physique
    MeleeSweep, // Zone de dégâts au corps-à-corps
    AreaStream,  // Dégâts de zone continus
}

public enum FeedbackBehavior
{
    None,
    GunRecoil,  // Recul classique avec Kick et Recovery
    MeleeSwing,  // Animation de balayage/rotation de l'arme
    ContinuousStream,  // Effets continus tant que l'arme est active
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

    // -- Archetype ---
    [Property, Feature( "Archetype" ), Group( "Archetype" )] public TriggerBehavior TriggerType { get; set; } = TriggerBehavior.SemiAuto;
    [Property, Feature( "Archetype" ), Group( "Archetype" )] public DeliveryBehavior DeliveryType { get; set; } = DeliveryBehavior.Hitscan;
    [Property, Feature( "Archetype" ), Group( "Archetype" )] public FeedbackBehavior FeedbackType { get; set; } = FeedbackBehavior.GunRecoil;

    // -- Archetype Settings ---
    // TriggerType
    [Property, Feature( "Archetype" ), Group( "Trigger Settings" ), ShowIf( nameof( TriggerType ), TriggerBehavior.Burst )]
    public int BurstCount { get; set; } = 3;

    [Property, Feature( "Archetype" ), Group( "Trigger Settings" ), ShowIf( nameof( TriggerType ), TriggerBehavior.Burst )]
    public float BurstFireRate { get; set; } = 0.1f;

    // DeliveryType
    [Property, Feature( "Archetype" ), Group( "Delivery Settings" ), ShowIf( nameof( DeliveryType ), DeliveryBehavior.Hitscan )]
    public int ProjectilesPerShot { get; set; } = 1;

    [Property, Feature( "Archetype" ), Group( "Delivery Settings" ), ShowIf( nameof( DeliveryType ), DeliveryBehavior.Hitscan )]
    public float SpreadAngle { get; set; } = 0.5f;

    [Property, Feature( "Archetype" ), Group( "Delivery Settings" ), ShowIf( nameof( DeliveryType ), DeliveryBehavior.Hitscan )]
    public int PierceCount { get; set; } = 0;

    [Property, Feature( "Archetype" ), Group( "Delivery Settings" ), ShowIf( nameof( DeliveryType ), DeliveryBehavior.Hitscan )]
    public float PierceDamagePenalty { get; set; } = 0.2f;

    [Property, Feature( "Archetype" ), Group( "Delivery Settings" ), ShowIf( nameof( DeliveryType ), DeliveryBehavior.Projectile )]
    public GameObject ProjectilePrefab { get; set; }

    [Property, Feature( "Archetype" ), Group( "Delivery Settings" ), ShowIf( nameof( DeliveryType ), DeliveryBehavior.MeleeSweep )]
    public float SweepRadius { get; set; } = 25f;

    [Property, Feature( "Archetype" ), Group( "Delivery Settings" ), ShowIf( nameof( DeliveryType ), DeliveryBehavior.AreaStream )]
    public float StreamRadius { get; set; } = 40f;

    // FeedbackType
    [Property, Feature( "Archetype" ), Group( "Feedback Settings" ), ShowIf( nameof( FeedbackType ), FeedbackBehavior.GunRecoil )]
    public float RecoilKick { get; set; } = 15f;

    [Property, Feature( "Archetype" ), Group( "Feedback Settings" ), ShowIf( nameof( FeedbackType ), FeedbackBehavior.GunRecoil )]
    public float RecoilPushback { get; set; } = 3f;

    [Property, Feature( "Archetype" ), Group( "Feedback Settings" ), ShowIf( nameof( FeedbackType ), FeedbackBehavior.GunRecoil )]
    public float RecoilRecovery { get; set; } = 10f;

    [Property, Feature( "Archetype" ), Group( "Feedback Settings" ), ShowIf( nameof( FeedbackType ), FeedbackBehavior.GunRecoil )]
    public GameObject MuzzleFlashPrefab { get; set; }

    [Property, Feature( "Archetype" ), Group( "Feedback Settings" ), ShowIf( nameof( FeedbackType ), FeedbackBehavior.GunRecoil )]
    public bool InvertRecoilPitch { get; set; } = false;

    [Property, Feature( "Archetype" ), Group( "Feedback Settings" ), ShowIf( nameof( FeedbackType ), FeedbackBehavior.MeleeSwing )]
    public float SwingPitch { get; set; } = 70f;

    [Property, Feature( "Archetype" ), Group( "Feedback Settings" ), ShowIf( nameof( FeedbackType ), FeedbackBehavior.MeleeSwing )]
    public float SwingForward { get; set; } = 15f;

    [Property, Feature( "Archetype" ), Group( "Feedback Settings" ), ShowIf( nameof( FeedbackType ), FeedbackBehavior.MeleeSwing )]
    public float SwingDrop { get; set; } = -10f;

    [Property, Feature( "Archetype" ), Group( "Feedback Settings" ), ShowIf( nameof( FeedbackType ), FeedbackBehavior.MeleeSwing )]
    public float SwingSpeed { get; set; } = 12f;

    [Property, Feature( "Archetype" ), Group( "Feedback Settings" ), ShowIf( nameof( FeedbackType ), FeedbackBehavior.ContinuousStream )]
    public GameObject StreamEffectPrefab { get; set; }

    // --- Stats ---
    [Property, Group( "Stats" )] public float DamageBase { get; set; }
    [Property, Group( "Stats" )] public float AttackRateBase { get; set; }
    [Property, Group( "Stats" )] public float CriticalChanceBase { get; set; } = 5.0f;
    [Property, Group( "Stats" )] public float CriticalDamageBase { get; set; } = 2.0f;
    [Property, Group( "Stats" )] public float RangeBase { get; set; }
    [Property, Group( "Stats" )] public float ImpactForce { get; set; } = 500f;
    [Property, Group( "Stats" )] public float FalloffStartRatio { get; set; } = 0.5f;
    [Property, Group( "Stats" )] public float FalloffMinMultiplier { get; set; } = 1.0f;

    // --- Energy System ---
    [Property, Group( "Energy" )] public bool UsesEnergy { get; set; } = true;
    [Property, Group( "Energy" )] public float EnergyCost { get; set; } = 10f;

    // --- Collision & Wall Avoidance ---
    [Property, Feature( "Animation" ), Group( "Collision" )] public float WeaponLength { get; set; } = 40f;
    [Property, Feature( "Animation" ), Group( "Collision" )] public float LiftMultiplier { get; set; } = 1.5f;

    // --- AudioVisual ---
    [Property, Feature( "Animation" ), Group( "Feedback" )] public SoundEvent AttackSound { get; set; }
    [Property, Feature( "Animation" ), Group( "Feedback" )] public SoundEvent ExhaustionSound { get; set; }
    [Property, Feature( "Animation" ), Group( "Feedback" )] public GameObject HitEffectPrefab { get; set; }

    // --- Animation (3rd Person) ---
    [Property, Feature( "Animation" ), Group( "3d Person" )] public string AnimationTriggerName { get; set; }
    [Property, Feature( "Animation" ), Group( "3d Person" )] public WeaponHoldType HoldType { get; set; } = WeaponHoldType.Melee;
    [Property, Feature( "Animation" ), Group( "3d Person" )] public int Handedness { get; set; } = 1; // 1 = Droite, 2 = Deux mains

    // --- Viewmodel (1st Person) ---
    [Property, Feature( "Animation" ), Group( "Viewmodel (1st Person)" )] public Vector3 LocalHandPosition { get; set; } = Vector3.Zero;
    [Property, Feature( "Animation" ), Group( "Viewmodel (1st Person)" )] public Angles LocalHandRotation { get; set; } = Angles.Zero;
    [Property, Feature( "Animation" ), Group( "Viewmodel (1st Person)" )] public string ViewmodelIdleAnim { get; set; } = "idle";
    [Property, Feature( "Animation" ), Group( "Viewmodel (1st Person)" )] public string ViewmodelFireAnim { get; set; } = "b_fire";

    // --- Economy ---
    [Property, Group( "Economy" )] public double UnlockCost { get; set; }
    [Property, Group( "Economy" )] public int RequiredSpawnerTier { get; set; }

    // --- Localization Keys ---
    [Group( "Localization" ), Feature( "Other" )]
    public string LocalizationKeyName
    {
        get => $"#weapon.{RequiredSpawnerTier}.{Category.ToString().ToLower()}.{Id}.name";
    }
    [Group( "Localization" ), Feature( "Other" )]
    public string LocalizationKeyDescription
    {
        get => $"#weapon.{RequiredSpawnerTier}.{Category.ToString().ToLower()}.{Id}.description";
    }
}


/*
Pistolet classique : SemiAuto + Hitscan + GunRecoil (Projectiles: 1)

Fusil d'assaut : FullAuto + Hitscan + GunRecoil (Projectiles: 1)

Fusil à pompe (Shotgun) : SemiAuto + Hitscan + GunRecoil (Projectiles: 8, Spread: 5)

Sniper perforant : SemiAuto + Hitscan + GunRecoil (Projectiles: 1, Pierce: 3)

Lance-roquettes : SemiAuto + Projectile + GunRecoil (Projectiles: 1)

Canon à dispersion plasma : Burst + Projectile + GunRecoil (Projectiles: 5, Spread: 10)
*/