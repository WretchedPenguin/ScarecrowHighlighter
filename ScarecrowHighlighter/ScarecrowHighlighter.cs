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
        private bool holding;
        private bool pressed;

        private Texture2D tileTexture;

        private ModConfig config;

        private HighlightedDrawer drawer;

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
            helper.Events.GameLoop.UpdateTicked += CheckHoldingScarecrow;
            helper.Events.Input.ButtonPressed += CheckButtonPressed;
        }
        
        private void DisplayOnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (!hovering && !holding && !pressed)
                return;

            drawer.DrawHighlightedObjects(sender, e);
            
            if(holding)
                drawer.Remove(Game1.currentCursorTile);
        }

        private void CheckButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == config.ToggleHighlightButton)
                pressed = !pressed;
        }

        private void CheckHoldingScarecrow(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;
            holding = false;
            if (Game1.player.CurrentItem == null || !config.HighlightOnHold)
                return;
            
            holding = Game1.player.CurrentItem.Name.Contains(config.SearchString);
            if (holding)
                drawer.AddHighlight(Game1.currentCursorTile, config.Radius);
        }

        private void PlayerOnWarped(object sender, WarpedEventArgs e)
        {
            drawer.Clear();
            foreach (var obj in e.NewLocation.Objects.Values)
            {
                if (obj.Name.Contains(config.SearchString))
                {
                    drawer.AddHighlight(obj.TileLocation, config.Radius);
                }
            }
        }

        private void WorldOnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            if (!e.IsCurrentLocation)
                return;
            foreach (var added in e.Added)
            {
                if (added.Value.Name.Contains(config.SearchString))
                {
                    drawer.AddHighlight(added.Value.TileLocation, config.Radius);
                }
            }

            foreach (var removed in e.Removed)
            {
                if (removed.Value.Name.Contains(config.SearchString))
                {
                    drawer.Remove(removed.Key);
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
            if (!hovered.Name.Contains(config.SearchString) || !config.HighlightOnHovered)
                return;

            hovering = true;
        }
    }
}