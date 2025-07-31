using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ScarecrowHighlighter;

public class HighlightedDrawer
{
    private readonly ModConfig _config;

    public HighlightedDrawer(ModConfig config)
    {
        _config = config;
    }

    public void DrawHighlightedItems(SpriteBatch batch, List<(Vector2 location, string qualifiedItemId)> items)
    {
        // Do a lookup to collect all the items that affect a location
        var tiles = items.ToLookup(x => x.location, x => x.qualifiedItemId);

        foreach (var toDraw in tiles)
        {
            DrawTile(batch, toDraw.Key, toDraw.ToList());
        }
    }

    private void DrawTile(SpriteBatch spriteBatch, Vector2 tileLocation, List<string> qualifiedItemIds)
    {
        const int cursorSize = 16;

        var position = TileToScreen(tileLocation);

        spriteBatch.Draw(
            texture: Game1.mouseCursors,
            position: position,
            sourceRectangle: new Rectangle(194, 388, cursorSize, cursorSize),
            color: Color.White,
            rotation: 0,
            origin: Vector2.Zero,
            scale: new Vector2(Game1.tileSize / (float) cursorSize),
            effects: SpriteEffects.None,
            layerDepth: 0
        );

        // Adds icons for the sources of the highlighting
        if (!_config.HighlightSource) return;

        // Make sure the icons are always displayed in the same order
        var orderedItemIds = qualifiedItemIds.Distinct().OrderBy(x => x).ToList();
        var columnAmount = (float) Math.Ceiling(Math.Sqrt(orderedItemIds.Count));
        var iconSize = Game1.tileSize / columnAmount;
        const int padding = 4;

        // For each unique source, add an icon
        for (var i = 0; i < orderedItemIds.Count; i++)
        {
            var x = i % columnAmount;
            var y = (float) Math.Floor(i / columnAmount);
            var iconPosition = position + new Vector2(x * iconSize + padding, y * iconSize + padding);

            var qualifiedItemId = qualifiedItemIds[i];
            var data = ItemRegistry.GetData(qualifiedItemId);
            var texture = data.GetTexture();
            var sourceRectangle = data.GetSourceRect();

            spriteBatch.Draw(
                texture: texture,
                position: iconPosition,
                sourceRectangle: sourceRectangle,
                color: Color.White * 0.3f,
                rotation: 0,
                origin: Vector2.Zero,
                scale: Vector2.One,
                effects: SpriteEffects.None,
                layerDepth: 1
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
}