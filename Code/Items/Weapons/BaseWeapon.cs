using Sandbox;
using System.Collections.Generic;

public abstract class BaseWeapon : Component, ITooltipProvider
{
    // --- Data & References ---
    [Property] public WeaponDefinition Data { get; set; }

    public SkinnedModelRenderer PlayerBody { get; set; }
    public SkinnedModelRenderer ViewmodelArms { get; set; }
    private PlayerStats _playerStats;

    protected virtual bool IsAutomatic => false;

    public float Damage => WeaponStatsCalculator.GetStat( Data, WeaponStatTarget.Damage );

    public float AttackRate => WeaponStatsCalculator.GetStat( Data, WeaponStatTarget.AttackRate );

    public float CriticalChance => WeaponStatsCalculator.GetStat( Data, WeaponStatTarget.CriticalChance );

    public float CriticalDamage => WeaponStatsCalculator.GetStat( Data, WeaponStatTarget.CriticalDamage );

    public float Range => WeaponStatsCalculator.GetStat( Data, WeaponStatTarget.Range );

    // --- Lifecycle ---

    protected override void OnUpdate()
    {
        if ( _playerStats == null )
            _playerStats = PlayerStats.Local;

        if ( IsProxy || Data == null || _playerStats == null ) return;

        // 1. Energy Management (Exhaustion Recovery)
        if ( _playerStats.IsExhausted )
        {
            if ( _playerStats.TimeSinceExhausted >= _playerStats.ExhaustionPenalty )
            {
                _playerStats.IsExhausted = false;
                _playerStats.CurrentEnergy = _playerStats.MaxEnergy;
                Log.Info( "[BaseWeapon] Energy restored. Weapon ready to fire." );
            }
            else
            {
                return;
            }
        }

        // 2. Energy Recharge
        if ( _playerStats.CurrentEnergy < _playerStats.MaxEnergy && _playerStats.TimeSinceLastAttack > (1f / AttackRate) + 0.5f )
        {
            _playerStats.CurrentEnergy += _playerStats.RechargeRate * Time.Delta;

            if ( _playerStats.CurrentEnergy >= _playerStats.MaxEnergy ) _playerStats.CurrentEnergy = _playerStats.MaxEnergy;
        }

        // 3. Attack Input
        bool shouldAttack = IsAutomatic
            ? Input.Down( "attack1" )
            : Input.Pressed( "attack1" );

        if ( shouldAttack && _playerStats.TimeSinceLastAttack >= (1f / AttackRate) )
        {
            ExecuteAttack();
        }
    }

    // --- Methods ---

    protected void ExecuteAttack()
    {
        if ( _playerStats.IsExhausted ) return;

        // Sound
        if ( Data.AttackSound != null )
            Sound.Play( Data.AttackSound, WorldPosition );

        // Animation (3rd Person)
        if ( PlayerBody != null && !string.IsNullOrEmpty( Data.AnimationTriggerName ) )
            PlayerBody.Set( Data.AnimationTriggerName, true );

        // Animation (1st Person)
        if ( ViewmodelArms != null && !string.IsNullOrEmpty( Data.ViewmodelFireAnim ) )
            ViewmodelArms.Set( Data.ViewmodelFireAnim, true );

        _playerStats.TimeSinceLastAttack = 0;
        PerformAttack();
    }

    protected void ConsumeEnergy()
    {
        if ( !Data.UsesEnergy || _playerStats == null ) return;

        _playerStats.CurrentEnergy -= Data.EnergyCost;

        if ( _playerStats.CurrentEnergy <= 0 )
        {
            _playerStats.CurrentEnergy = 0;
            _playerStats.IsExhausted = true;
            _playerStats.TimeSinceExhausted = 0;

            if ( Data.ExhaustionSound != null )
                Sound.Play( Data.ExhaustionSound, WorldPosition );

            Log.Warning( "[BaseWeapon] WARNING: Weapon energy depleted. Player exhausted." );
        }
    }

    public float GetEnergyPercentage()
    {
        if ( _playerStats == null || _playerStats.MaxEnergy <= 0 ) return 0f;
        return _playerStats.CurrentEnergy / _playerStats.MaxEnergy;
    }

    public bool ShouldShowEnergyBar()
    {
        if ( _playerStats == null ) return false;
        return Data != null && Data.UsesEnergy &&
            (_playerStats.CurrentEnergy < _playerStats.MaxEnergy ||
             _playerStats.CurrentEnergy <= 0 ||
             _playerStats.TimeSinceLastAttack < 1f);
    }

    public bool ShouldHit()
    {
        float rollChance = Game.Random.Float( 0f, 100f );
        return rollChance < CriticalChance;
    }

    public float GetFinalDamage( bool isCrit )
    {
        return isCrit ? Damage * CriticalDamage : Damage;
    }

    public float GetDPS() => WeaponStatsCalculator.GetDPS( Data );

    // --- Abstract & Virtual Methods ---

    public virtual IEnumerable<TooltipEntry> GetTooltips()
    {
        yield return new TooltipEntry { InputAction = "attack1", Description = "#tooltip.weapon.attack" };
    }

    protected abstract void PerformAttack();

}