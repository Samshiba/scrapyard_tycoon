using Sandbox;

public sealed class PrestigeShopInteractable : Component, Component.IPressable
{
    public bool Press( IPressable.Event e )
    {
        if ( e.Source == null ) return false;

        // Show prestige shop UI
        if ( PrestigeShopUI.Local != null )
        {
            PrestigeShopUI.Local.OpenMenu();
            return true;
        }

        Log.Warning( "[PrestigeShopInteractable] ERROR: PrestigeShopUI component not found in scene" );
        return false;
    }

    public bool CanPress( IPressable.Event e ) => true;
    public void Release( IPressable.Event e ) { }

    public System.Nullable<IPressable.Tooltip> GetTooltip( IPressable.Event e )
    {
        return new IPressable.Tooltip
        {
            Description = "#tooltip.prestigeshop.description",
        };
    }
}