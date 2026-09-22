using Sandbox;
using System.Collections.Generic;
using System.Linq;

public sealed class TooltipManager : Component
{
    public List<TooltipEntry> ActiveTooltips { get; private set; } = new();

    private PlayerController _player;
    private PlayerInventory _inventory;

    public static TooltipManager Get( Scene scene )
    {
        return scene.GetAllComponents<TooltipManager>().FirstOrDefault();
    }

    protected override void OnStart()
    {
        _player = Components.Get<PlayerController>();
        _inventory = Components.Get<PlayerInventory>();

        if ( _player.IsProxy )
        {
            Enabled = false;
            return;
        }
    }

    protected override void OnUpdate()
    {
        ActiveTooltips.Clear();

        var weapon = _inventory?.ActiveWeapon;
        if ( weapon is ITooltipProvider provider )
        {
            ActiveTooltips.AddRange( provider.GetTooltips() );
        }

        if ( _player?.Tooltips != null )
        {
            foreach ( var t in _player.Tooltips )
            {
                ActiveTooltips.Add( new TooltipEntry
                {
                    InputAction = "use",
                    Description = t.Description,
                    Icon = t.Icon
                } );
            }
        }
    }
}