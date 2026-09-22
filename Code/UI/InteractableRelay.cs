using Sandbox;

public sealed class InteractableRelay : Component, Component.IPressable
{
    public Component.IPressable TargetInteractable { get; set; }

    public bool Press( IPressable.Event e )
    {
        return TargetInteractable?.Press( e ) ?? false;
    }

    public bool CanPress( IPressable.Event e )
    {
        return TargetInteractable?.CanPress( e ) ?? false;
    }

    public void Release( IPressable.Event e )
    {
        TargetInteractable?.Release( e );
    }

    public System.Nullable<IPressable.Tooltip> GetTooltip( IPressable.Event e )
    {
        return TargetInteractable?.GetTooltip( e );
    }
}