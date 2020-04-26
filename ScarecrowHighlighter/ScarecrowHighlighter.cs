using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private Texture2D tileTexture;

        private ModConfig config;

        private HighlightedDrawer drawer;
        private readonly Dictionary<Object, int> itemsToHighlight = new Dictionary<Object, int>();

        public override void Entry(IModHelper helper)
        {
            config = Helper.ReadConfig<ModConfig>();
            tileTexture =
                Helper.Content.Load<Texture2D>(config.TexturePath, ContentSource.GameContent);
            drawer = new HighlightedDrawer(tileTexture, config.TextureSourceRectangle);

            helper.Events.Input.CursorMoved += InputOnCursorMoved;
            helper.Events.Display.RenderedWorld += DisplayOnRenderedWorld;
            helper.Events.World.ObjectListChanged += WorldOnObjectListChanged;
            helper.Events.Player.Warped += PlayerOnWarped;
            helper.Events.Input.ButtonPressed += CheckButtonPressed;
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
            if (e.Button == config.ToggleHighlightButton)
                pressed = !pressed;
        }

        private bool CheckHoldingHighlight()
        {
            if (!Context.IsWorldReady)
                return false;
            if (Game1.player.CurrentItem == null || !config.HighlightOnHold)
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
            Vector2 tile = e.NewPosition.Tile;
            Object hovered = Game1.currentLocation.getObjectAtTile((int) tile.X, (int) tile.Y);
            if (hovered == null)
                return;
            if (!MatchesSearchPattern(hovered.Name, out int radius) || !config.HighlightOnHovered)
                return;

            hovering = true;
        }

        private bool MatchesSearchPattern(string name, out int radius)
        {
            var searchItems = config.SearchItems.OrderByDescending(s => s.SearchString.Length);
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