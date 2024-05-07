using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using xTile.Layers;

namespace RegenerativeAgriculture
{
    // Taken from the Automate mod made by Pathoschild
    internal static class TileHelper
    {
        public static IEnumerable<Vector2> GetTiles(this GameLocation? location)
        {
            if (location?.Map?.Layers == null)
            {
                return Enumerable.Empty<Vector2>();
            }
            Layer layer = location.Map.Layers[0];
            return TileHelper.GetTiles(0, 0, layer.LayerWidth, layer.LayerHeight);
        }

        public static IEnumerable<Vector2> GetTiles(this Rectangle area)
        {
            return TileHelper.GetTiles(area.X, area.Y, area.Width, area.Height);
        }

        public static Rectangle Expand(this Rectangle area, int distance)
        {
            return new Rectangle(area.X - distance, area.Y - distance, area.Width + distance * 2, area.Height + distance * 2);
        }

        public static IEnumerable<Vector2> GetSurroundingTiles(this Vector2 tile)
        {
            return Utility.getSurroundingTileLocationsArray(tile);
        }

        public static IEnumerable<Vector2> GetSurroundingTiles(this Rectangle area)
        {
            for (int x = area.X - 1; x <= area.X + area.Width; x++)
            {
                for (int y = area.Y - 1; y <= area.Y + area.Height; y++)
                {
                    if (!area.Contains(x, y))
                    {
                        yield return new Vector2(x, y);
                    }
                }
            }
        }

        public static IEnumerable<Vector2> GetAdjacentTiles(this Vector2 tile)
        {
            return Utility.getAdjacentTileLocationsArray(tile);
        }

        public static IEnumerable<Vector2> GetTiles(int x, int y, int width, int height)
        {
            int curX = x;
            for (int maxX = x + width - 1; curX <= maxX; curX++)
            {
                int curY = y;
                for (int maxY = y + height - 1; curY <= maxY; curY++)
                {
                    yield return new Vector2(curX, curY);
                }
            }
        }

        public static IEnumerable<Vector2> GetVisibleTiles(int expand = 0)
        {
            return TileHelper.GetVisibleArea(expand).GetTiles();
        }

        public static Rectangle GetVisibleArea(int expand = 0)
        {
            return new Rectangle(Game1.viewport.X / 64 - expand, Game1.viewport.Y / 64 - expand, (int)Math.Ceiling((decimal)Game1.viewport.Width / 64m) + expand * 2, (int)Math.Ceiling((decimal)Game1.viewport.Height / 64m) + expand * 2);
        }

        public static Vector2 GetTileFromCursor()
        {
            return TileHelper.GetTileFromScreenPosition(Game1.getMouseX(), Game1.getMouseY());
        }

        public static Vector2 GetTileFromScreenPosition(float x, float y)
        {
            return new Vector2((int)(((float)Game1.viewport.X + x) / 64f), (int)(((float)Game1.viewport.Y + y) / 64f));
        }
    }

}
