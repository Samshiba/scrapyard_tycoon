using Sandbox;
using System.Collections.Generic;

[AssetType(Name = "Prop Definition", Extension = "prop", Category = "ScrapYard")]
public partial class PropDefinition : GameResource
{
    [Property, Group("Identity")] public string PropID { get; set; } = "Item";
    [Property, Group("Identity")] public int Tier { get; set; } = 1;
    [Property, Group("Identity")] public Model Model { get; set; }

    [Property, Group("Stats"), Range(1, 5)] public int RarityMod { get; set; } = 1;

    [Property, Group("Sub-Destruction")] public List<PropDrop> SubProps { get; set; } = new();
}