using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;

namespace ScarecrowHighlighter
{
    public class HighlightedDrawer
    {
        private readonly Dictionary<Vector2, int> _highlights = new();

        public void Add(Vector2 location, int radius)
        {
            _highlights[location] = radius;
        }

        public void Clear()
        {
            _highlights.Clear();
        }

        public void DrawHighlightedObjects(RenderedWorldEventArgs e)
        {
            foreach (var toDraw in _highlights)
            {
                DrawObject(e.SpriteBatch, toDraw.Key, toDraw.Value);
            }
        }

        private static void DrawObject(SpriteBatch spriteBatch, Vector2 tileLocation, int radius)
        {
            var locations = GetLocationsInRadius(tileLocation, radius);

            const int cursorSize = 16;

            foreach (var location in locations)
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

        private static Vector2 TileToScreen(Vector2 location)
        {
            return new Vector2(
                location.X * Game1.tileSize - Game1.viewport.X,
                location.Y * Game1.tileSize - Game1.viewport.Y
            );
        }

        private static IEnumerable<Vector2> GetLocationsInRadius(Vector2 location, int radius)
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
    }
}