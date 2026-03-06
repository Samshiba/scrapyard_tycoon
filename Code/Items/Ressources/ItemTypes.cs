using Sandbox;

public enum ResourceType
{
    Wood,
    Metal,
    Plastic,
    Glass,
    Battery
}

public struct ItemData
{
    public ResourceType Type { get; set; }
    public float Value { get; set; }
}