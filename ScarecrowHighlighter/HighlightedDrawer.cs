using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ScarecrowHighlighter;

public class HighlightedDrawer
{
    public void DrawHighlightedItems(SpriteBatch batch, Dictionary<string, int> radiusByQualifiedItemId, List<(Vector2 location, string qualifiedItemId)> items)
    {
        var tiles = items
            // Turn the list of objects' locations to a list of tiles to highlight
            .SelectMany(item =>
            {
                var radius = radiusByQualifiedItemId[item.qualifiedItemId];
                return GetLocationsInRadius(item.location, radius)
                    .Select(location => item with { location = location });
            })
            // Do a lookup to collect all the items that affect this location
            .ToLookup(x => x.location, x => x.qualifiedItemId);

        foreach (var toDraw in tiles)
        {
            DrawTile(batch, toDraw.Key);
        }
    }

    private static void DrawTile(SpriteBatch spriteBatch, Vector2 tileLocation)
    {
        const int cursorSize = 16;

        spriteBatch.Draw(
            Game1.mouseCursors,
            TileToScreen(tileLocation),
            new Rectangle(194, 388, cursorSize, cursorSize),
            Color.White,
            0,
            Vector2.Zero,
            new Vector2(Game1.tileSize / (float) cursorSize),
            SpriteEffects.None,
            0
        );
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