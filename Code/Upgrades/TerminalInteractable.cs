using Sandbox;

public sealed class TerminalInteractable : Component, Component.IPressable
{
    public bool Press( IPressable.Event e )
    {
        if ( e.Source == null ) return false;

        // ✅ SkillTreeUI maintenant
        if ( SkillTreeUI.Local != null )
        {
            SkillTreeUI.Local.OpenMenu();
            return true;
        }

        Log.Warning( "SkillTreeUI introuvable !" );
        return false;
    }

    public bool CanPress( IPressable.Event e ) => true;
    public void Release( IPressable.Event e ) { }

    public System.Nullable<IPressable.Tooltip> GetTooltip( IPressable.Event e )
    {
        return new IPressable.Tooltip
        {
            Description = LocalizationManager.GetText( "tooltip.terminal.description" ),
        };
    }
}