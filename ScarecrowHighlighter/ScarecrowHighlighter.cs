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

    private readonly Dictionary<string, HighlightingConfig> _configByQualifiedItemId = new();

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);

        _config = Helper.ReadConfig<ModConfig>();
        _drawer = new HighlightedDrawer(_config);

        helper.Events.GameLoop.GameLaunched += (_, _) => BuildHighlightingList();
        helper.Events.GameLoop.GameLaunched += RegisterModConfigMenu;

        helper.Events.Display.RenderedWorld += DisplayHighlighting;
        helper.Events.Input.ButtonPressed += CheckToggleHighlightButton;
    }

    /// <summary>
    /// Builds a list of objects to highlight based on the name of registered items.
    /// The radius from the scarecrow is taken from the official method <see cref="Object.GetRadiusForScarecrow"/>
    /// </summary>
    private void BuildHighlightingList()
    {
        // Clear for when the config changes
        _configByQualifiedItemId.Clear();

        foreach (var itemType in ItemRegistry.ItemTypes.Where(x => x.Identifier == "(BC)"))
        {
            foreach (var id in itemType.GetAllIds())
            {
                var data = itemType.GetData(id);

                if (!data.InternalName.Contains("arecrow", StringComparison.OrdinalIgnoreCase)) continue;

                var radius = data.InternalName.Contains("deluxe", StringComparison.OrdinalIgnoreCase) ? 17 : 9;
                _configByQualifiedItemId.Add(data.QualifiedItemId, new HighlightingConfig(radius, HighlightingType.Circle));
            }
        }

        if (_config.HighlightSprinklers)
        {
            // Radius can be overriden when placed with an upgrade, this is handled using the GetSprinklerTiles method later
            _configByQualifiedItemId["(O)599"] = new HighlightingConfig(0, HighlightingType.Square);
            _configByQualifiedItemId["(O)621"] = new HighlightingConfig(1, HighlightingType.Square);
            _configByQualifiedItemId["(O)645"] = new HighlightingConfig(2, HighlightingType.Square);
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

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: I18n.Config_HighlightSprinklers_Name,
            tooltip: I18n.Config_HighlightSprinklers_Tooltip,
            getValue: () => _config.HighlightSprinklers,
            setValue: value =>
            {
                _config.HighlightSprinklers = value;
                // Rebuild the list of highlighting objects
                BuildHighlightingList();
            });

        configMenu.AddKeybind(
            mod: ModManifest,
            name: I18n.Config_ToggleHighlight_Name,
            getValue: () => _config.ToggleHighlightButton,
            setValue: value => _config.ToggleHighlightButton = value
        );
    }

    private void DisplayHighlighting(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsPlayerFree) return;

        List<(Vector2 location, string qualifiedItemId)> highlightedLocations = new();

        // Check if the object the cursor is above is a highlighted object
        var hoveredObject = Game1.currentLocation.getObjectAtTile((int) Game1.currentCursorTile.X, (int) Game1.currentCursorTile.Y);
        var hovered = _config.HighlightOnHovered && hoveredObject is not null && _configByQualifiedItemId.ContainsKey(hoveredObject.QualifiedItemId);

        // Check if the player is holding a highlighted item
        var holding = false;
        if (Game1.player.CurrentItem != null && _config.HighlightOnHold)
        {
            holding = _configByQualifiedItemId.ContainsKey(Game1.player.CurrentItem.QualifiedItemId);
            if (holding)
            {
                AddTilesInRange(ref highlightedLocations, Game1.currentCursorTile, Game1.player.CurrentItem.QualifiedItemId);
            }
        }

        // If the highlighting shouldn't be displayed, don't render it
        if (!(hovered || holding || _alwaysDisplayHighlighting)) return;

        foreach (var worldObject in Game1.currentLocation.Objects.Values)
        {
            if (_config.HighlightSprinklers && worldObject.GetSprinklerTiles().Any())
            {
                highlightedLocations.AddRange(worldObject.GetSprinklerTiles().Select(tile => (tile, worldObject.QualifiedItemId)));
            }

            AddTilesInRange(ref highlightedLocations, worldObject.TileLocation, worldObject.QualifiedItemId);
        }

        _drawer.DrawHighlightedItems(e.SpriteBatch, highlightedLocations);
    }

    private void AddTilesInRange(ref List<(Vector2 location, string qualifiedItemId)> highlightedLocations, Vector2 location, string qualifiedItemId)
    {
        IEnumerable<Vector2> tiles = new List<Vector2>();
        if (_configByQualifiedItemId.TryGetValue(qualifiedItemId, out var config))
        {
            tiles = config.Type is HighlightingType.Circle
                ? GetLocationsInCircleRadius(location, config.Radius)
                : GetLocationsInSquareRadius(location, config.Radius);
        }

        var tilesWithItemId = tiles.Select(tile => (tile, qualifiedItemId));
        highlightedLocations.AddRange(tilesWithItemId);
    }

    private static IEnumerable<Vector2> GetLocationsInCircleRadius(Vector2 location, int radius)
    {
        for (float x = -radius; x < radius; x++)
        {
            for (var y = -radius; y < radius; y++)
            {
                var tileLocation = new Vector2(location.X + x, location.Y + y);
                if (Vector2.Distance(location, tileLocation) < radius)
                {
                    yield return tileLocation;
                }
            }
        }
    }

    /// <summary>
    /// Code ported from <see cref="Object.GetSprinklerTiles"/> to be supported outside of Object
    /// </summary>
    private static IEnumerable<Vector2> GetLocationsInSquareRadius(Vector2 location, int radius)
    {
        if (radius == 0)
            return Utility.getAdjacentTileLocations(location);
        if (radius <= 0)
            return new List<Vector2>();

        var tiles = new List<Vector2>();
        for (var x = (int) location.X - radius; x <= location.X + (double) radius; ++x)
        {
            for (var y = (int) location.Y - radius; y <= location.Y + (double) radius; ++y)
                tiles.Add(new Vector2(x, y));
        }

        return tiles;
    }

    private void CheckToggleHighlightButton(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button != _config.ToggleHighlightButton) return;

        _alwaysDisplayHighlighting = !_alwaysDisplayHighlighting;
    }
}

public record HighlightingConfig(int Radius, HighlightingType Type);

public enum HighlightingType
{
    Circle,
    Square,
}