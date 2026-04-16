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

    public float Damage
    {
        get
        {
            if ( Data == null ) return 0;

            float baseDamage = Data.DamageBase;
            float totalMultiplier = 1.0f;

            if ( SaveManager.Instance?.CurrentFactory?.GlobalUpgrades != null )
            {
                var upgrades = SaveManager.Instance.CurrentFactory.GlobalUpgrades;
                string typeName = Data.DamageType.ToString().ToLower();
                float[] bonusValues = { 0.05f, 0.25f, 1.0f, 5.0f };

                for ( int i = 0; i < bonusValues.Length; i++ )
                {
                    string upgradeKey = $"{typeName}_damage_{i + 1}";
                    totalMultiplier += upgrades.GetValueOrDefault( upgradeKey, 0 ) * bonusValues[i];
                }
            }

            return baseDamage * totalMultiplier;
        }
    }

    public float AttackRate
    {
        get
        {
            if ( Data == null ) return 1f;

            float baseRate = Data.AttackRateBase;
            float totalMultiplier = 1.0f;

            if ( SaveManager.Instance?.CurrentFactory?.GlobalUpgrades != null )
            {
                var upgrades = SaveManager.Instance.CurrentFactory.GlobalUpgrades;
                float[] bonusValues = { 0.05f, 0.25f, 1.0f, 5.0f };

                for ( int i = 0; i < bonusValues.Length; i++ )
                {
                    string upgradeKey = $"weapon_attack_rate_{i + 1}";
                    totalMultiplier += upgrades.GetValueOrDefault( upgradeKey, 0 ) * bonusValues[i];
                }
            }

            return baseRate * totalMultiplier;
        }
    }

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

    // --- Abstract & Virtual Methods ---

    public virtual IEnumerable<TooltipEntry> GetTooltips()
    {
        yield return new TooltipEntry { InputAction = "attack1", Description = "#tooltip.weapon.attack" };
    }

    protected abstract void PerformAttack();

}