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

    public void DrawHighlightedItems(SpriteBatch batch, Dictionary<Vector2, HashSet<string>> tiles)
    {
        foreach (var toDraw in tiles)
        {
            DrawTile(batch, toDraw.Key, toDraw.Value);
        }
    }

    private void DrawTile(SpriteBatch spriteBatch, Vector2 tileLocation, HashSet<string> qualifiedItemIds)
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
        const int padding = 8;
        var iconSize = (Game1.tileSize - padding * 2) / columnAmount;

        // For each unique source, add an icon
        for (var i = 0; i < orderedItemIds.Count; i++)
        {
            var qualifiedItemId = orderedItemIds[i];
            var data = ItemRegistry.GetData(qualifiedItemId);
            var texture = data.GetTexture();
            var sourceRectangle = data.GetSourceRect();

            // Scale the icon to fix the size it's supposed to be, to fix exactly in the grid, but a little smaller
            var scale = new Vector2(iconSize / sourceRectangle.Height * 0.75f);
            var actualWidth = scale.X * sourceRectangle.Width;
            var actualHeight = scale.Y * sourceRectangle.Height;
            
            var x = i % columnAmount;
            var y = (float) Math.Floor(i / columnAmount);
            var iconPositionTopLeft = position + new Vector2(x * iconSize + padding, y * iconSize + padding);
            var centered = new Vector2(iconPositionTopLeft.X + (iconSize - actualWidth) / 2, iconPositionTopLeft.Y + (iconSize - actualHeight) / 2);

            spriteBatch.Draw(
                texture: texture,
                position: centered,
                sourceRectangle: sourceRectangle,
                color: Color.White * 0.5f,
                rotation: 0,
                origin: Vector2.Zero,
                scale: scale,
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