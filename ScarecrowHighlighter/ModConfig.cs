using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace ScarecrowHighlighter
{
    public class ModConfig
    {
        public class SearchItem
        {
            public string SearchString { get; set; }
            public int Radius { get; set; }
        }
        
        public List<SearchItem> SearchItems = new List<SearchItem>
        {
            new SearchItem{SearchString = "crow",Radius = 8},
            new SearchItem{SearchString = "Deluxe Scarecrow",Radius = 16},
        };
        public string TexturePath { get; set; } = "LooseSprites/buildingPlacementTiles.xnb";
        public Rectangle TextureSourceRectangle { get; set; } = new Rectangle(0, 0, Game1.tileSize, Game1.tileSize);
        public SButton ToggleHighlightButton { get; set; } = SButton.L;
        public bool HighlightOnHold { get; set; } = true;
        public bool HighlightOnHovered { get; set; } = true;
    }
}
