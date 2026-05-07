using Sandbox;
using System.Collections.Generic;
using System;

public sealed class WeaponComponent : Component, IWeaponContext, ITooltipProvider
{
    // --- Data & References ---
    [Property] public WeaponDefinition Data { get; set; }
    [Property] public GameObject MuzzlePoint { get; set; }
    [Property] public ModelRenderer WeaponModel { get; set; }
    [Property] public SkinnedModelRenderer PlayerBody { get; set; }
    [Property] public SkinnedModelRenderer ViewmodelArms { get; set; }

    private IEnergySource _energySource;

    // --- Active Strategies ---
    private IWeaponTrigger _triggerBehavior;
    private IWeaponDelivery _deliveryBehavior;
    private IWeaponFeedback _feedbackBehavior;

    // =========================================================================
    // IWEAPONCONTEXT IMPLEMENTATION
    // =========================================================================
    WeaponDefinition IWeaponContext.Data => Data;

    GameObject IWeaponContext.Owner => Components.GetInAncestors<PlayerController>()?.GameObject ?? GameObject.Root;

    public GameObject MuzzleObject => MuzzlePoint != null
        ? MuzzlePoint
        : (WeaponModel != null ? WeaponModel.GameObject : GameObject);

    public Transform AttackTransform => MuzzleObject.Transform.World;

    public float GetStat( WeaponStatTarget stat ) => WeaponStatsCalculator.GetStat( Data, stat );
    public bool RollCritical() => Game.Random.Float( 0f, 100f ) < GetStat( WeaponStatTarget.CriticalChance );
    public bool IsExhausted() => _energySource?.IsExhausted ?? false;

    string IWeaponContext.GetPlayerUniqueId()
    {
        var backpack = Components.GetInAncestors<PlayerBackpack>();
        return backpack?.Network.Owner?.GetUniqueId() ?? backpack?.Network.Owner?.SteamId.ToString() ?? "unknown";
    }

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    protected override void OnStart()
    {
        if ( Data == null ) return;

        _energySource = Components.GetInAncestors<IEnergySource>();

        // 1. Instancier les stratégies via la Factory (on l'écrit juste en dessous)
        _triggerBehavior = WeaponStrategyFactory.CreateTrigger( Data.TriggerType );
        _deliveryBehavior = WeaponStrategyFactory.CreateDelivery( Data.DeliveryType );
        _feedbackBehavior = WeaponStrategyFactory.CreateFeedback( Data.FeedbackType );

        // 2. Initialiser le visuel (sauvegarde la position de base pour le recul/swing)
        _feedbackBehavior?.Initialize( GameObject );

        // 3. Lier l'action de tir au Trigger
        _triggerBehavior?.BindActions( () => ExecuteAttack() );
    }

    protected override void OnUpdate()
    {
        if ( IsProxy || Data == null || _energySource == null ) return;

        if ( _triggerBehavior == null ) return;

        // On récupère les inputs
        bool pressed = Input.Pressed( "attack1" );
        bool down = Input.Down( "attack1" );
        bool released = Input.Released( "attack1" );

        // On laisse le Trigger décider s'il doit appeler notre action liée (ExecuteAttack)
        _triggerBehavior.Update( this, pressed, down, released );

        // On met à jour l'esthétique (recul qui redescend, animation d'épée qui se finit)
        _feedbackBehavior?.Update( Time.Delta );
    }

    // =========================================================================
    // MÉTHODES INTERNES
    // =========================================================================
    private void ExecuteAttack()
    {
        if ( IsExhausted() || !ConsumeEnergy() ) return;

        _deliveryBehavior?.Execute( this );
        _feedbackBehavior?.PlayAttackFeedback( this );

        _energySource?.NotifyAttack( GetStat( WeaponStatTarget.AttackRate ) );
    }

    public bool ConsumeEnergy()
    {
        if ( !Data.UsesEnergy || _energySource == null ) return true;
        if ( _energySource == null ) return true;

        bool success = _energySource.TryConsumeEnergy( Data.EnergyCost );

        if ( success )
        {
            GameStats.OnEnergyConsumed( Scene, ((IWeaponContext)this).GetPlayerUniqueId(), Data.EnergyCost );
        }

        return success;
    }

    public IEnumerable<TooltipEntry> GetTooltips()
    {
        yield return new TooltipEntry { InputAction = "attack1", Description = "#tooltip.weapon.attack" };
    }
}