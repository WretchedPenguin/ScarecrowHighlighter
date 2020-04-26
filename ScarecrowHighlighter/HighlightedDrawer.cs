using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using Object = StardewValley.Object;

namespace ScarecrowHighlighter
{
    public class HighlightedDrawer
    {
        public readonly Dictionary<Vector2, int> highlights = new Dictionary<Vector2, int>();
        private readonly Texture2D tileTexture;
        private readonly Rectangle textureSourceRectangle;

        public HighlightedDrawer(Texture2D tileTexture, Rectangle textureSourceRectangle)
        {
            this.tileTexture = tileTexture;
            this.textureSourceRectangle = textureSourceRectangle;
        }

        public void AddHighlight(Vector2 location, int radius)
        {
            highlights[location] = radius;
        }
        public void Remove(Vector2 location)
        {
            highlights.Remove(location);
        }

        public void Clear()
        {
            highlights.Clear();
        }

        public void DrawHighlightedObjects(object sender, RenderedWorldEventArgs e)
        {
            foreach (var toDraw in highlights)
            {
                DrawObject(e.SpriteBatch, toDraw.Key, toDraw.Value);
            }
        }
        
        private void DrawObject(SpriteBatch spriteBatch, Vector2 tileLocation, int radius)
        {
            var locations = GetLocationsInRadius(tileLocation, radius);
            foreach (Vector2 location in locations)
            {
                spriteBatch.Draw(tileTexture, TileToScreen(location), textureSourceRectangle, Color.White);
            }
        }
        
        private Vector2 TileToScreen(Vector2 location)
        {
            return new Vector2(location.X * Game1.tileSize - Game1.viewport.X,
                (location.Y * Game1.tileSize) - Game1.viewport.Y);
        }
        
        private List<Vector2> GetLocationsInRadius(Vector2 location, int radius)
        {
            List<Vector2> locations = new List<Vector2>();
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