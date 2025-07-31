using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Object = StardewValley.Object;

namespace ScarecrowHighlighter;

public class ScarecrowHighlighterMod : Mod
{
    private bool _alwaysDisplayHighlighting;

    private ModConfig _config = null!;

    private readonly HighlightedDrawer _drawer = new();
    private readonly Dictionary<string, int> _radiusByQualifiedId = new();

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);

        _config = Helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.GameLaunched += BuildHighlightingList;
        helper.Events.GameLoop.GameLaunched += RegisterModConfigMenu;

        helper.Events.Display.RenderedWorld += DisplayHighlighting;
        helper.Events.Input.ButtonPressed += CheckToggleHighlightButton;
    }


    /// <summary>
    /// Builds a list of objects to highlight based on the name of registered items.
    /// The radius from the scarecrow is taken from the official method <see cref="Object.GetRadiusForScarecrow"/>
    /// </summary>
    private void BuildHighlightingList(object? sender, GameLaunchedEventArgs e)
    {
        foreach (var itemType in ItemRegistry.ItemTypes.Where(x => x.Identifier == "(BC)"))
        {
            foreach (var id in itemType.GetAllIds())
            {
                var data = itemType.GetData(id);

                if (!data.InternalName.Contains("arecrow", StringComparison.OrdinalIgnoreCase)) continue;

                var radius = data.InternalName.Contains("deluxe", StringComparison.OrdinalIgnoreCase) ? 17 : 9;
                _radiusByQualifiedId.Add(data.QualifiedItemId, radius);
            }
        }
    }

    private void RegisterModConfigMenu(object? sender, GameLaunchedEventArgs e)
    {
        // get Generic Mod Config Menu's API (if it's installed)
        var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
            return;

        configMenu.Register(
            mod: ModManifest,
            reset: () => _config = new ModConfig(),
            save: () => Helper.WriteConfig(_config)
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.Config_HighlightOnHold_Name,
            tooltip: I18n.Config_HighlightOnHold_Tooltip,
            getValue: () => _config.HighlightOnHold,
            setValue: value => _config.HighlightOnHold = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.Config_HighlightOnHover_Name,
            tooltip: I18n.Config_HighlightOnHover_Tooltip,
            getValue: () => _config.HighlightOnHovered,
            setValue: value => _config.HighlightOnHovered = value
        );

        configMenu.AddKeybind(
            mod: ModManifest,
            name: I18n.Config_ToggleHighlight_Name,
            getValue: () => _config.ToggleHighlightButton,
            setValue: value => _config.ToggleHighlightButton = value
        );
    }

    private void DisplayHighlighting(object? sender, RenderedWorldEventArgs e)
    {
        _drawer.Clear();

        // Check if the object the cursor is above is a highlighted object
        var hovered = Game1.currentLocation.getObjectAtTile((int) Game1.currentCursorTile.X, (int) Game1.currentCursorTile.Y);
        if (hovered != null && _radiusByQualifiedId.TryGetValue(hovered.QualifiedItemId, out var hoveredRadius))
        {
            _drawer.Add(hovered.TileLocation, hoveredRadius);
        }
        
        // Check if the player is holding a highlighted item
        var holding = _radiusByQualifiedId.TryGetValue(Game1.player.CurrentItem.QualifiedItemId, out var holdingRadius);
        if (holding)
        {
            _drawer.Add(Game1.currentCursorTile, holdingRadius);
        }

        // If the highlighting shouldn't be displayed, don't render it
        if (!(hovered is not null || holding || _alwaysDisplayHighlighting)) return;

        foreach (var worldObject in Game1.currentLocation.Objects.Values)
        {
            if (_radiusByQualifiedId.TryGetValue(worldObject.QualifiedItemId, out var radius))
            {
                _drawer.Add(worldObject.TileLocation, radius);
            }
        }

        _drawer.DrawHighlightedObjects(e);
    }

    private void CheckToggleHighlightButton(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button != _config.ToggleHighlightButton) return;

        _alwaysDisplayHighlighting = !_alwaysDisplayHighlighting;
    }
}