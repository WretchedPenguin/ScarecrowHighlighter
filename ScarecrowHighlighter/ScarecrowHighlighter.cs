using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using Object = StardewValley.Object;

namespace ScarecrowHighlighter
{
    public class ScarecrowHighlighterMod : Mod
    {
        private bool hovering;
        private bool pressed;

        private ModConfig Config;

        private HighlightedDrawer drawer;
        private readonly Dictionary<Object, int> itemsToHighlight = new Dictionary<Object, int>();

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);

            Config = Helper.ReadConfig<ModConfig>();
            drawer = new HighlightedDrawer();

            helper.Events.GameLoop.GameLaunched += RegisterModConfigMenu;
            helper.Events.Input.CursorMoved += InputOnCursorMoved;
            helper.Events.Display.RenderedWorld += DisplayOnRenderedWorld;
            helper.Events.World.ObjectListChanged += WorldOnObjectListChanged;
            helper.Events.Player.Warped += PlayerOnWarped;
            helper.Events.Input.ButtonPressed += CheckButtonPressed;
        }

        private void RegisterModConfigMenu(object? sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_HighlightOnHold_Name,
                tooltip: I18n.Config_HighlightOnHold_Tooltip,
                getValue: () => Config.HighlightOnHold,
                setValue: value => Config.HighlightOnHold = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: I18n.Config_HighlightOnHover_Name,
                tooltip: I18n.Config_HighlightOnHover_Tooltip,
                getValue: () => Config.HighlightOnHovered,
                setValue: value => Config.HighlightOnHovered = value
            );

            configMenu.AddKeybind(
                mod: ModManifest,
                name: I18n.Config_ToggleHighlight_Name,
                getValue: () => Config.ToggleHighlightButton,
                setValue: value => Config.ToggleHighlightButton = value
            );
        }

        private void DisplayOnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            drawer.Clear();

            var holding = CheckHoldingHighlight();
            if (!hovering && !holding && !pressed)
                return;

            foreach (var pair in itemsToHighlight)
            {
                drawer.AddHighlight(pair.Key.TileLocation, pair.Value);
            }

            drawer.DrawHighlightedObjects(sender, e);
        }

        private void CheckButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == Config.ToggleHighlightButton)
                pressed = !pressed;
        }

        private bool CheckHoldingHighlight()
        {
            if (!Context.IsWorldReady)
                return false;
            if (Game1.player.CurrentItem == null || !Config.HighlightOnHold)
                return false;

            var holding = MatchesSearchPattern(Game1.player.CurrentItem.Name, out int radius);
            if (holding)
            {
                drawer.AddHighlight(Game1.currentCursorTile, radius);
            }

            return holding;
        }

        private void PlayerOnWarped(object sender, WarpedEventArgs e)
        {
            foreach (var obj in e.NewLocation.Objects.Values)
            {
                if (MatchesSearchPattern(obj.Name, out int radius))
                {
                    itemsToHighlight.Add(obj, radius);
                }
            }
        }

        private void WorldOnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            if (!e.IsCurrentLocation)
                return;
            foreach (var added in e.Added)
            {
                if (MatchesSearchPattern(added.Value.Name, out int radius))
                {
                    itemsToHighlight.Add(added.Value, radius);
                }
            }

            foreach (var removed in e.Removed)
            {
                if (MatchesSearchPattern(removed.Value.Name, out int radius))
                {
                    itemsToHighlight.Remove(removed.Value);
                }
            }
        }

        private void InputOnCursorMoved(object sender, CursorMovedEventArgs e)
        {
            hovering = false;
            if (e == null || !Context.IsWorldReady)
                return;
            if (!Config.HighlightOnHovered) return;

            Vector2 tile = e.NewPosition.Tile;
            Object hovered = Game1.currentLocation.getObjectAtTile((int)tile.X, (int)tile.Y);
            if (hovered == null)
                return;
            if (!MatchesSearchPattern(hovered.Name, out int radius))
                return;

            hovering = true;
        }

        private bool MatchesSearchPattern(string name, out int radius)
        {
            var searchItems = Config.SearchItems.OrderByDescending(s => s.SearchString.Length);
            foreach (ModConfig.SearchItem searchItem in searchItems)
            {
                if (name.Contains(searchItem.SearchString))
                {
                    radius = searchItem.Radius;
                    return true;
                }
            }

            radius = -1;
            return false;
        }
    }
}