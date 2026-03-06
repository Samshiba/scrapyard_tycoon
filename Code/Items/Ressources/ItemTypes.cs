using Sandbox;

public enum ResourceType
{
    Scrap,
    Energy,
    Plastic,
    Battery
}

public struct ItemData
{
    public ResourceType Type { get; set; }
    public float Value { get; set; }
}