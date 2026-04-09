using Sandbox;

public sealed class SimpleButton : Component, Component.IPressable
{
    [Property] public GameObject MovingPart { get; set; }
    [Property] public Vector3 PressOffset { get; set; } = new Vector3( 0, 0, -1f );
    [Property] public float PressSpeed { get; set; } = 10f;
    [Property] public PropSpawner Spawner { get; set; }

    private Vector3 _startPosition;
    private bool _isPressed;

    protected override void OnStart()
    {
        if ( MovingPart != null )
        {
            _startPosition = MovingPart.LocalPosition;
        }
    }

    protected override void OnUpdate()
    {
        if ( MovingPart == null ) return;

        var targetPosition = _isPressed ? _startPosition + PressOffset : _startPosition;

        MovingPart.LocalPosition = Vector3.Lerp( MovingPart.LocalPosition, targetPosition, Time.Delta * PressSpeed );

        if ( _isPressed && Vector3.DistanceBetween( MovingPart.LocalPosition, targetPosition ) < 0.05f )
        {
            _isPressed = false;
        }
    }

    public bool CanPress( IPressable.Event e ) => true;

    public bool Press( IPressable.Event e )
    {
        if ( _isPressed ) return false;

        _isPressed = true;
        Spawner?.SpawnProp();

        return true;
    }

    public void Release( IPressable.Event e )
    {
    }

    public System.Nullable<IPressable.Tooltip> GetTooltip( IPressable.Event e )
    {
        return new IPressable.Tooltip
        {
            Description = "#tooltip.spawn.description",
        };
    }
}