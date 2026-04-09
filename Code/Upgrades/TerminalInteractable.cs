using Sandbox;

public sealed class TerminalInteractable : Component, Component.IPressable
{
    public bool Press( IPressable.Event e )
    {
        if ( e.Source == null ) return false;

        // Show skill tree UI
        if ( SkillTreeUI.Local != null )
        {
            SkillTreeUI.Local.OpenMenu();
            return true;
        }

        Log.Warning( "[TerminalInteractable] ERROR: SkillTreeUI component not found in scene" );
        return false;
    }

    public bool CanPress( IPressable.Event e ) => true;
    public void Release( IPressable.Event e ) { }

    public System.Nullable<IPressable.Tooltip> GetTooltip( IPressable.Event e )
    {
        return new IPressable.Tooltip
        {
            Description = "#tooltip.terminal.description",
        };
    }
}