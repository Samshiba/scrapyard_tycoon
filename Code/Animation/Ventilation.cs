using Sandbox;

public sealed class SimpleFan : Component
{
    [Property] public float SpinSpeed { get; set; } = 300f;

    protected override void OnUpdate()
    {
        LocalRotation *= Rotation.FromRoll( SpinSpeed * Time.Delta );
    }
}