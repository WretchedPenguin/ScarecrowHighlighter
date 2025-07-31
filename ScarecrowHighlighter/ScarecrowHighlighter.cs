using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Object = StardewValley.Object;

namespace ScarecrowHighlighter;

public class ScarecrowHighlighterMod : Mod
{
    private bool _alwaysDisplayHighlighting;

    private ModConfig _config = null!;
    private HighlightedDrawer _drawer = null!;

    private readonly Dictionary<string, int> _radiusByQualifiedItemId = new();

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);

        _config = Helper.ReadConfig<ModConfig>();
        _drawer = new HighlightedDrawer(_config);

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
                _radiusByQualifiedItemId.Add(data.QualifiedItemId, radius);
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

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.Config_HighlightSource_Name,
            tooltip: I18n.Config_HighlightSource_Tooltip,
            getValue: () => _config.HighlightSource,
            setValue: value => _config.HighlightSource = value
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
        List<(Vector2 location, string qualifiedItemId)> highlightedLocations = new();

        // Check if the object the cursor is above is a highlighted object
        var hovered = Game1.currentLocation.getObjectAtTile((int) Game1.currentCursorTile.X, (int) Game1.currentCursorTile.Y);
        if (hovered != null && _radiusByQualifiedItemId.ContainsKey(hovered.QualifiedItemId))
        {
            highlightedLocations.Add((hovered.TileLocation, hovered.QualifiedItemId));
        }

        // Check if the player is holding a highlighted item
        var holding = _radiusByQualifiedItemId.ContainsKey(Game1.player.CurrentItem.QualifiedItemId);
        if (holding)
        {
            highlightedLocations.Add((Game1.currentCursorTile, Game1.player.CurrentItem.QualifiedItemId));
        }

        // If the highlighting shouldn't be displayed, don't render it
        if (!(hovered is not null || holding || _alwaysDisplayHighlighting)) return;

        foreach (var worldObject in Game1.currentLocation.Objects.Values)
        {
            if (_radiusByQualifiedItemId.ContainsKey(worldObject.QualifiedItemId))
            {
                highlightedLocations.Add((worldObject.TileLocation, worldObject.QualifiedItemId));
            }
        }

        _drawer.DrawHighlightedItems(e.SpriteBatch, _radiusByQualifiedItemId, highlightedLocations);
    }

    private void CheckToggleHighlightButton(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button != _config.ToggleHighlightButton) return;

        _alwaysDisplayHighlighting = !_alwaysDisplayHighlighting;
    }
}