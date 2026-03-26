using Sandbox;

public sealed class TerminalShopInteractable : Component, Component.IPressable
{
    public bool Press( IPressable.Event e )
    {
        if ( e.Source == null ) return false;

        // ✅ WeaponShopUI maintenant
        if ( WeaponShopUI.Local != null )
        {
            WeaponShopUI.Local.OpenMenu();
            return true;
        }

        Log.Warning( "WeaponShopUI introuvable !" );
        return false;
    }

    public bool CanPress( IPressable.Event e ) => true;
    public void Release( IPressable.Event e ) { }

    public System.Nullable<IPressable.Tooltip> GetTooltip( IPressable.Event e )
    {
        return new IPressable.Tooltip
        {
            Description = LocalizationManager.GetText( "tooltip.shop.description" ),
        };
    }
}