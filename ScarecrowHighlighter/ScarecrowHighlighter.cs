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

        private readonly List<Object> scarecrows = new List<Object>();

        private ModConfig config;

        public override void Entry(IModHelper helper)
        {
            config = Helper.ReadConfig<ModConfig>();

            helper.Events.Input.CursorMoved += InputOnCursorMoved;
            helper.Events.Display.RenderedWorld += DisplayOnRenderedWorld;
            helper.Events.World.ObjectListChanged += WorldOnObjectListChanged;
            helper.Events.Player.Warped += PlayerOnWarped;
            helper.Events.GameLoop.UpdateTicked += CheckHoldingScarecrow;
            helper.Events.Input.ButtonPressed += CheckButtonPressed;

            tileTexture =
                Helper.Content.Load<Texture2D>(config.TexturePath, ContentSource.GameContent);
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
        }

        private void PlayerOnWarped(object sender, WarpedEventArgs e)
        {
            scarecrows.Clear();
            foreach (var obj in e.NewLocation.Objects.Values)
            {
                if (obj.Name.Contains(config.SearchString))
                {
                    scarecrows.Add(obj);
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
                    scarecrows.Add(added.Value);
            }

            foreach (var removed in e.Removed)
            {
                if (removed.Value.Name.Contains(config.SearchString))
                    scarecrows.Remove(removed.Value);
            }
        }

        private void DisplayOnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            if (!hovering && !holding && !pressed)
                return;

            if (holding)
                DrawScarecrow(e.SpriteBatch, Game1.currentCursorTile);

            foreach (Object scarecrow in scarecrows)
            {
                DrawScarecrow(e.SpriteBatch, scarecrow.TileLocation);
            }
        }

        private void DrawScarecrow(SpriteBatch spriteBatch, Vector2 tileLocation)
        {
            var locations = GetLocationsInRadius(tileLocation);
            foreach (Vector2 location in locations)
            {
                spriteBatch.Draw(tileTexture, TileToScreen(location), config.TextureSourceRectangle, Color.White);
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

        private Vector2 TileToScreen(Vector2 location)
        {
            return new Vector2(location.X * Game1.tileSize - Game1.viewport.X,
                (location.Y * Game1.tileSize) - Game1.viewport.Y);
        }

        private List<Vector2> GetLocationsInRadius(Vector2 location)
        {
            List<Vector2> locations = new List<Vector2>();
            int radius = config.Radius;
            for (float x = -radius; x <= radius; x++)
            {
                float tilesRemoved = Math.Abs(x) - radius / 2f;
                for (float y = Math.Max(-radius, -radius + tilesRemoved);
                    y <= Math.Min(radius, radius - tilesRemoved);
                    y++)
                {
                    locations.Add(new Vector2(location.X + x, location.Y + y));
                }
            }

            return locations;
        }
    }
}