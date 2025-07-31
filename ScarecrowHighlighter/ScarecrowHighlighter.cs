using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Object = StardewValley.Object;

namespace ScarecrowHighlighter;

public class ScarecrowHighlighterMod : Mod
{
    private bool _hovering;
    private bool _pressed;

    private ModConfig _config = null!;

    private readonly HighlightedDrawer _drawer = new();
    private readonly Dictionary<Object, int> _itemsToHighlight = new();

    private readonly Dictionary<string, int> _radiusByQualifiedId = new();

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);

        _config = Helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.GameLaunched += BuildHighlightingList;
        helper.Events.GameLoop.GameLaunched += RegisterModConfigMenu;

        helper.Events.Input.CursorMoved += InputOnCursorMoved;
        helper.Events.Display.RenderedWorld += DisplayOnRenderedWorld;
        helper.Events.World.ObjectListChanged += WorldOnObjectListChanged;
        helper.Events.Player.Warped += PlayerOnWarped;
        helper.Events.Input.ButtonPressed += CheckButtonPressed;
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

    private void DisplayOnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        _drawer.Clear();

        var holding = CheckHoldingHighlight();
        if (!_hovering && !holding && !_pressed)
            return;

        foreach (var pair in _itemsToHighlight)
        {
            _drawer.Add(pair.Key.TileLocation, pair.Value);
        }

        _drawer.DrawHighlightedObjects(e);
    }

    private void CheckButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button != _config.ToggleHighlightButton) return;

        _pressed = !_pressed;
    }

    private bool CheckHoldingHighlight()
    {
        if (!Context.IsWorldReady) return false;
        if (Game1.player.CurrentItem == null) return false;
        if (!_config.HighlightOnHold) return false;

        var holding = _radiusByQualifiedId.TryGetValue(Game1.player.CurrentItem.QualifiedItemId, out var radius);

        if (holding)
        {
            _drawer.Add(Game1.currentCursorTile, radius);
        }

        return holding;
    }

    private void PlayerOnWarped(object? sender, WarpedEventArgs e)
    {
        foreach (var obj in e.NewLocation.Objects.Values)
        {
            if (_radiusByQualifiedId.TryGetValue(obj.QualifiedItemId, out var radius))
            {
                _itemsToHighlight.Add(obj, radius);
            }
        }
    }

    private void WorldOnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
    {
        if (!e.IsCurrentLocation) return;

        foreach (var added in e.Added)
        {
            if (_radiusByQualifiedId.TryGetValue(added.Value.QualifiedItemId, out var radius))
            {
                _itemsToHighlight.Add(added.Value, radius);
            }
        }

        foreach (var removed in e.Removed)
        {
            if (_radiusByQualifiedId.ContainsKey(removed.Value.QualifiedItemId))
            {
                _itemsToHighlight.Remove(removed.Value);
            }
        }
    }

    private void InputOnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
        _hovering = false;
        if (!Context.IsWorldReady) return;
        if (!_config.HighlightOnHovered) return;

        var tile = e.NewPosition.Tile;
        var hovered = Game1.currentLocation.getObjectAtTile((int) tile.X, (int) tile.Y);
        if (hovered == null) return;
        if (!_radiusByQualifiedId.ContainsKey(hovered.QualifiedItemId)) return;

        _hovering = true;
    }
}