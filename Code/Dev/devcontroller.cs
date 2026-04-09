using Sandbox;
using Sandbox.UI;

public sealed class DevController : PanelComponent
{
    [Property] public float Sensitivity { get; set; } = 0.2f;

    private Angles _angles;
    private bool _freeMouse = false;

    protected override void OnUpdate()
    {
        HandleToggle();
        HandleMouseMode();
    }

    void HandleToggle()
    {
        if ( Input.Pressed( "dev" ) )
        {
            Log.Info( $"Dev Mode: {(!_freeMouse ? "ON" : "OFF")}" );
            _freeMouse = !_freeMouse;
        }
    }

    void HandleMouseMode()
    {
        // Check if any UI panel needs to control the mouse
        bool uiHasControl = false;

        // WeaponShop takes priority
        if ( WeaponShopUI.Local != null && WeaponShopUI.Local.IsOpen )
        {
            uiHasControl = true;
        }
        // Also check SkillTree
        else if ( SkillTreeUI.Local != null && SkillTreeUI.Local.IsOpen )
        {
            uiHasControl = true;
        }

        // Only control mouse if no UI panel has it
        if ( !uiHasControl )
        {
            if ( _freeMouse )
            {
                Mouse.Visibility = MouseVisibility.Visible;
                Panel.Style.PointerEvents = PointerEvents.All;
            }
            else
            {
                Mouse.Visibility = MouseVisibility.Hidden;
                Panel.Style.PointerEvents = PointerEvents.None;
            }
        }
    }

}