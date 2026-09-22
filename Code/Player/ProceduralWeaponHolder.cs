using Sandbox;
using System;

public sealed class ProceduralWeaponHolder : Component
{
    [Property, Group( "Position" )] public Vector3 HoldOffset { get; set; } = new Vector3( 12f, 6f, -10f );
    [Property, Group( "Position" )] public Angles HoldAngles { get; set; } = new Angles( 0f, 0f, 0f );

    [Property, Group( "Sway" )] public float SwayAmount { get; set; } = 0.04f;
    [Property, Group( "Sway" )] public float SwaySmoothing { get; set; } = 10f;
    [Property, Group( "Sway" )] public float SwayClamp { get; set; } = 0.15f;

    [Property, Group( "Bob" )] public float BobFrequency { get; set; } = 10f;
    [Property, Group( "Bob" )] public float BobAmplitude { get; set; } = 0.6f;

    [Property, Group( "Camera" )] public float HideDistance { get; set; } = 75f;

    [Property, Group( "Collision" )] public float PullbackSmoothness { get; set; } = 15f;
    public float WeaponLength { get; set; } = 40f;
    public float LiftAngleMultiplier { get; set; } = 1.5f;

    private float _currentPullback = 0f;

    private Vector3 _swayOffset = Vector3.Zero;
    private Vector3 _targetSwayOffset = Vector3.Zero;
    private float _bobTimer = 0f;
    private bool _wasMoving = false;

    protected override void OnStart()
    {
        // Réinitialiser les valeurs au démarrage
        _swayOffset = Vector3.Zero;
        _targetSwayOffset = Vector3.Zero;
        _bobTimer = 0f;
        _wasMoving = false;
    }

    protected override void OnEnabled()
    {
        // Réinitialiser en cas de ré-activation
        _swayOffset = Vector3.Zero;
        _targetSwayOffset = Vector3.Zero;
    }

    protected override void OnDisabled()
    {
        // Réinitialiser quand désactivé pour éviter les glitchs
        _swayOffset = Vector3.Zero;
        _targetSwayOffset = Vector3.Zero;
        _bobTimer = 0f;
        _wasMoving = false;
    }

    protected override void OnPreRender()
    {
        if ( IsProxy ) return;

        // Sway - Basé sur le mouvement de la caméra
        var look = Input.AnalogLook;
        _targetSwayOffset = new Vector3(
            MathX.Clamp( -look.yaw * SwayAmount, -SwayClamp, SwayClamp ),
            0f,
            MathX.Clamp( look.pitch * SwayAmount * 0.5f, -SwayClamp * 0.5f, SwayClamp * 0.5f )
        );

        // Smooth lerp avec clamp pour éviter les valeurs invalides
        _swayOffset = Vector3.Lerp( _swayOffset, _targetSwayOffset, Math.Min( Time.Delta * SwaySmoothing, 1f ) );

        // Bob - Basé sur le mouvement du joueur
        bool isMoving = Input.AnalogMove.Length > 0.1f;
        if ( isMoving )
        {
            _bobTimer += Time.Delta * BobFrequency;
            // Boucle le timer après une oscillation complète (2 * PI)
            if ( _bobTimer > MathF.PI * 2f )
                _bobTimer -= MathF.PI * 2f;
            _wasMoving = true;
        }
        else if ( _wasMoving )
        {
            // Transition progressive vers repos
            _bobTimer = MathX.Lerp( _bobTimer, 0f, Time.Delta * 6f );
            if ( _bobTimer < 0.01f )
            {
                _bobTimer = 0f;
                _wasMoving = false;
            }
        }

        // Formule de bob améliorée (sinusoïde complète pour un mouvement naturel)
        float bobVertical = MathF.Sin( _bobTimer ) * BobAmplitude;
        float bobHorizontal = MathF.Sin( _bobTimer * 0.5f ) * BobAmplitude * 0.3f; // Décalage horizontal moins prononcé

        var bobOffset = new Vector3(
            bobHorizontal,
            0f,
            bobVertical
        );

        // --- EVITEMENT DES MURS (Wall Pullback) ---
        float targetPullback = 0f;

        if ( Scene.Camera != null )
        {
            var camPos = Scene.Camera.WorldPosition;
            var camFwd = Scene.Camera.WorldRotation.Forward;

            // On lance un rayon depuis la caméra vers l'avant, de la longueur de l'arme
            var tr = Scene.Trace.Ray( camPos, camPos + camFwd * WeaponLength )
                .IgnoreGameObjectHierarchy( GameObject.Root )
                .WithoutTags( "trigger", "player" )
                .UsePhysicsWorld()
                .Run();

            if ( tr.Hit )
            {
                targetPullback = -(WeaponLength - tr.Distance);
            }
        }

        _currentPullback = MathX.Lerp( _currentPullback, targetPullback, Time.Delta * PullbackSmoothness );
        var wallAvoidanceOffset = new Vector3( _currentPullback, 0f, 0f );
        var wallAvoidanceRotation = Rotation.FromPitch( _currentPullback * LiftAngleMultiplier );

        // Appliquer les transformations
        if ( GameObject != null )
        {
            GameObject.LocalPosition = HoldOffset + _swayOffset + bobOffset + wallAvoidanceOffset;
            GameObject.LocalRotation = Rotation.From( HoldAngles ) * wallAvoidanceRotation;
        }
    }
}