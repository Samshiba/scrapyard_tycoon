using System.Collections.Generic;

public struct TooltipEntry
{
    public string InputAction;
    public string Description;
    public string Icon;
}

public interface ITooltipProvider
{
    IEnumerable<TooltipEntry> GetTooltips();
}