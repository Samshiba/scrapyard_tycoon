using Sandbox;

public sealed class DestroyAfterTime : Component
{
    [Property, Description("Temps en secondes avant la destruction du GameObject")] 
    public float TimeToLive { get; set; } = 2f;
    
    private TimeSince _timeAlive;

    protected override void OnStart()
    {
        _timeAlive = 0;
    }

    protected override void OnUpdate()
    {
        if (_timeAlive >= TimeToLive)
        {
            GameObject.Destroy();
        }
    }
}