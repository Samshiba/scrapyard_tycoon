using System.Collections.Generic;

public class StatModifiers
{
    public float Additive { get; set; } = 0f;
    public float Multiplicative { get; set; } = 1f;
    public float Compound { get; set; } = 1f;
    public Dictionary<string, float> Conversions { get; set; } = new();
}