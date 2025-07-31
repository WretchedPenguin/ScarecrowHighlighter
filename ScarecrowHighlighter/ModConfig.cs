using StardewModdingAPI;

namespace ScarecrowHighlighter;

public class ModConfig
{
    public SButton ToggleHighlightButton { get; set; } = SButton.L;
    public bool HighlightOnHold { get; set; } = true;
    public bool HighlightOnHovered { get; set; } = true;
    public bool HighlightSource { get; set; } = true;
    public bool HighlightSprinklers { get; set; } = true;
}