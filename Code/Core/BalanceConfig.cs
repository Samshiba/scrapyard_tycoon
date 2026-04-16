using Sandbox;
using System.Linq;

[AssetType( Name = "Scrap Config", Extension = "config", Category = "ScrapYard" )]
public partial class BalanceConfig : GameResource
{
    [Property, Group( "HP Balancing" )] public float BaseHP { get; set; } = 30f;
    [Property, Group( "HP Balancing" )] public float HPMult { get; set; } = 5.0f;

    [Property, Group( "Economy Balancing" )] public float BaseValue { get; set; } = 5.0f;
    [Property, Group( "Economy Balancing" )] public float ValueMult { get; set; } = 2.0f;
    [Property, Group( "Economy Balancing" )] public float JackpotBonus { get; set; } = 1.5f;

    [Property, Group( "Gibs Balancing" )] public int BaseGibs { get; set; } = 5;
    [Property, Group( "Gibs Balancing" )] public int MaxGibs { get; set; } = 50;
    [Property, Group( "Gibs Balancing" )] public int GibsPerTier { get; set; } = 5;
    [Property, Group( "Gibs Balancing" )] public GameObject GibPrefab { get; set; }

    [Property, Group( "Progression" )] public int MaxSpawnerTier { get; set; } = 10;

    public static BalanceConfig Instance => ResourceLibrary.GetAll<BalanceConfig>().FirstOrDefault();
}