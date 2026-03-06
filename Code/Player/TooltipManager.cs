using Sandbox;
using System.Collections.Generic;

public sealed class TooltipManager : Component
{
    public static TooltipManager Local { get; private set; }
    public List<TooltipEntry> ActiveTooltips { get; private set; } = new();

    private PlayerController _player;
    private PlayerInventory _inventory;

    protected override void OnStart()
    {
        _player = Components.Get<PlayerController>();
        _inventory = Components.Get<PlayerInventory>();

        if ( _player.IsProxy )
        {
            Enabled = false;
            return;
        }

        Local = this;
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