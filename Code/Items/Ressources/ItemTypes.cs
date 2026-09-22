using System.Text.Json.Serialization;
using Sandbox;

public enum ResourceType
{
    Wood,
    Metal,
    Plastic,
    Glass,
    Battery
}

public static class ResourceHelper
{
    public static string CategoryToLocalizedString( this ResourceType resourceType )
    {
        return $"#prop.resource.type.{resourceType.ToString().ToLower()}";
    }
}

public struct ItemData
{
    public ResourceType Type { get; set; }
    public float Value { get; set; }
}

public struct ItemStack
{
    [JsonPropertyName( "type" )] public ResourceType Type { get; set; }
    [JsonPropertyName( "value" )] public float Value { get; set; }
    [JsonPropertyName( "count" )] public int Count { get; set; }
}