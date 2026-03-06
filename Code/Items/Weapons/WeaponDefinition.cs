using Sandbox;

[AssetType( Name = "Weapon Definition", Extension = "weapon", Category = "ScrapYard" )]
public partial class WeaponDefinition : GameResource
{
    [Property, Group( "Identity" )] public string Id { get; set; } = "bat";
    [Property, Group( "Identity" )] public string WeaponName { get; set; } = "Batte en bois";
    [Property, Group( "Identity" )] public string Description { get; set; } = "Ça tape dur.";
    [Property, Group( "Identity" )] public GameObject WeaponPrefab { get; set; }

    private string _iconPath;
    [Property, Group( "Identity" )]
    public string IconPath
    {
        get => string.IsNullOrEmpty( _iconPath ) ? $"Resources/Weapons/Textures/{Id}.png" : _iconPath;
        set => _iconPath = value;
    }

    [Property, Group( "Stats" )] public float BaseDamage { get; set; } = 5f;
    [Property, Group( "Stats" )] public float AttackRate { get; set; } = 1.5f;
    [Property, Group( "Stats" )] public float Range { get; set; } = 130;

    [Property, Group( "Economy" )] public int UnlockCost { get; set; } = 100;
}