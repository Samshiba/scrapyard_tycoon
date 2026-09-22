using System;

public static class WeaponStrategyFactory
{
    public static IWeaponTrigger CreateTrigger( TriggerBehavior type )
    {
        return type switch
        {
            TriggerBehavior.SemiAuto => new SemiAutoTrigger(),
            TriggerBehavior.FullAuto => new FullAutoTrigger(),
            TriggerBehavior.Burst => new BurstTrigger(),
            TriggerBehavior.Continuous => new ContinuousTrigger(),
            _ => new SemiAutoTrigger()
        };
    }

    public static IWeaponDelivery CreateDelivery( DeliveryBehavior type )
    {
        return type switch
        {
            DeliveryBehavior.Hitscan => new HitscanDelivery(),
            DeliveryBehavior.Projectile => new ProjectileDelivery(),
            DeliveryBehavior.MeleeSweep => new MeleeSweepDelivery(),
            DeliveryBehavior.AreaStream => new AreaStreamDelivery(),
            _ => new HitscanDelivery()
        };
    }

    public static IWeaponFeedback CreateFeedback( FeedbackBehavior type )
    {
        return type switch
        {
            FeedbackBehavior.GunRecoil => new GunRecoilFeedback(),
            FeedbackBehavior.MeleeSwing => new MeleeSwingFeedback(),
            FeedbackBehavior.ContinuousStream => new ContinuousFeedback(),
            FeedbackBehavior.None => null,
            _ => null
        };
    }
}