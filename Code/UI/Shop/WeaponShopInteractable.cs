using Sandbox;

public sealed class WeaponShopInteractable : Component, Component.IPressable
{
    protected override void OnStart()
    {
        base.OnStart();

        foreach ( var child in GameObject.Children )
        {
            if ( child.Components.TryGet<Collider>( out _ ) )
            {
                var relay = child.Components.GetOrCreate<InteractableRelay>();

                relay.TargetInteractable = this;
            }
        }
    }

    public bool Press( IPressable.Event e )
    {
        if ( e.Source == null ) return false;

        // Show weapon shop UI
        if ( WeaponShopUI.Local != null )
        {
            WeaponShopUI.Local.OpenMenu();
            return true;
        }

        Log.Warning( "[WeaponShopInteractable] ERROR: WeaponShopUI component not found in scene" );
        return false;
    }

    public bool CanPress( IPressable.Event e ) => true;
    public void Release( IPressable.Event e ) { }

    public System.Nullable<IPressable.Tooltip> GetTooltip( IPressable.Event e )
    {
        return new IPressable.Tooltip
        {
            Description = "#tooltip.shop.description",
        };
    }
}