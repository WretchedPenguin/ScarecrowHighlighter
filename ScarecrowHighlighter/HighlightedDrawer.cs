using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace ScarecrowHighlighter
{
    public class HighlightedDrawer
    {
        public readonly Dictionary<Vector2, int> highlights = new Dictionary<Vector2, int>();

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

            var cursorSize = 16;

            foreach (Vector2 location in locations)
            {
                spriteBatch.Draw(
                    Game1.mouseCursors,
                    TileToScreen(location),
                    new Rectangle(194, 388, cursorSize, cursorSize),
                    Color.White,
                    0,
                    Vector2.Zero,
                    new Vector2(Game1.tileSize / (float) cursorSize),
                    SpriteEffects.None,
                    0
                );
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