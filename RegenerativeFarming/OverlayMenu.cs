using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

//using Pathoschild.Stardew.Automate.Framework;
//using Pathoschild.Stardew.Common;
//using Pathoschild.Stardew.Common.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewMods.Common.Services.Integrations.CustomBush;
using StardewValley;
using StardewValley.TerrainFeatures;
using xTile.Tiles;
using StardewMods.CustomBush;
using StardewMods.CustomBush.Framework;
using Force.DeepCloner;
using xTile.Dimensions;

namespace RegenerativeAgriculture
{
    internal sealed class OverlaySettings
    {
        public static OverlaySettings instance;
        public float overlay_opacity = .25f;
        public int displayed_measure = 0;
        public OverlaySettings()
        {
            instance = this;
        }

        public static OverlaySettings GetInstance()
        {
            //create a singleton, if not created already
            if (instance == null)
            {
                //instancing happens
                instance = new OverlaySettings();
            }
            return instance;
        }
    }

    // Taken from Automate mod by Pathoschild
    internal class OverlayMenu : BaseOverlay
    {
        private readonly int TileGap = 0;

        private SharedData shared_data;
        OverlaySettings settings;

        public OverlayMenu(IModEvents events, IInputHelper inputHelper, IReflectionHelper reflection)
            : base(events, inputHelper, reflection)
        {
            shared_data = SharedData.GetInstance();
            settings = OverlaySettings.GetInstance();
        }

        override protected void ReceiveButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            // Opacity control
            if (e.Pressed.Contains(SButton.Up))
            {
                settings.overlay_opacity += .05f;
                if (settings.overlay_opacity > 1f)
                {
                    settings.overlay_opacity = 1f;
                }
            }
            if (e.Pressed.Contains(SButton.Down))
            {
                settings.overlay_opacity -= .05f;
                if (settings.overlay_opacity < .05f) 
                {
                    settings.overlay_opacity = .05f;
                }

            }
            // Overlay selecting
            if (e.Pressed.Contains(SButton.Right))
            {
                settings.displayed_measure += 1;
                if (settings.displayed_measure == 4)
                {
                    settings.displayed_measure = 0;
                }
            }
            if (e.Pressed.Contains(SButton.Left))
            {
                settings.displayed_measure -= 1;
                if (settings.displayed_measure == -1)
                {
                    settings.displayed_measure = 3;
                }
            }
        }

        protected override void DrawWorld(SpriteBatch spriteBatch)
        {
            // Some of this information can be cached until player plants something, updates surveyed tiles, etc.
            GameLocation current_location = Game1.currentLocation;
            if (!Context.IsPlayerFree || !shared_data.soil_health_data.ContainsKey(current_location))
            {
                return;
            }


            Color[] measure_colors = shared_data.measure_colors;
            Color unknown_color = new Color(0, 0, 0, (int)(255 * settings.overlay_opacity));
            Color min_color = Color.Black;
            min_color.A = 0;
            Color max_color = measure_colors[settings.displayed_measure];
            max_color.A = (byte)(int) MathF.Min(255 * settings.overlay_opacity + 150, 255);

            HashSet<Vector2> danger_tiles = new(); // tiles that might die.
            foreach (Vector2 tile in TileHelper.GetVisibleTiles(1))
            {
                float screenX = tile.X * 64f - (float)Game1.viewport.X;
                float screenY = tile.Y * 64f - (float)Game1.viewport.Y;
                int tileSize = 64;

                Color disp_color;
                if (shared_data.surveyed_tiles[current_location].Contains(tile))
                {
                    int[] soil_health = shared_data.GetSoilHealth(current_location, tile);

                    float lerp_amount = (float)soil_health[settings.displayed_measure] / SharedData.max_health;
                    disp_color = Color.Lerp(min_color, max_color, lerp_amount * MathF.Min(1f, .6f + settings.overlay_opacity));
                    int[]? health_preview = shared_data.GetHealthPreview(tile);
                    if (health_preview is not null && HealthHelper.GetMissingNutrients(health_preview) > 0)
                    {
                        danger_tiles.Add(tile);
                    }
                }
                // This code makes only diggable tiles displays unknown color.  Unfortunately farm mods often make more things diggable and I can't correctly detect that yet.
                else if (current_location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back") != null)
                {
                    disp_color = unknown_color;
                }
                else
                {
                    continue;
                }
                DrawHelper.DrawLine(spriteBatch, screenX + (float)this.TileGap, screenY + (float)this.TileGap, new Vector2(tileSize - this.TileGap * 2, tileSize - this.TileGap * 2), disp_color);
            }
            Color danger_color = Color.Black;

            foreach (Vector2 tile in danger_tiles)
            {
                DrawEdgeBorders(spriteBatch, tile, danger_color, danger_tiles, 4);
            }

            // Draw Effect Radius
            Vector2 mouse_tile = Game1.currentCursorTile;
            TerrainFeature mouse_terrain;
            current_location.terrainFeatures.TryGetValue(mouse_tile, out mouse_terrain);
            int radius = -1;
            if (mouse_terrain is not null)
            {
                if (mouse_terrain is FruitTree)  // mouse_terrain is Tree || mouse_terrain is FruitTree)
                {
                    radius = 2;
                }
                else if (mouse_terrain is Bush bush)
                {
                    radius = 1;
                }
            }
            DrawRadius(spriteBatch, mouse_tile, Color.White, radius, 4);

            // Draw Cursor Tile Box.
            // Still draw radius even if no terrain.
            DrawEdgeBorders(spriteBatch, mouse_tile, Color.White, new HashSet<Vector2>() { mouse_tile }, 4, shaveSide: 16);
            base.DrawCursor();

            // DRAW HUD UI FOR WHAT COLOR WE'RE LOOKING AT
            Color[] colors = new Color[4];
            for (int i = 0; i < 4; i++)
            {
                if (i == settings.displayed_measure)
                {
                    colors[i] = measure_colors[i];
                }
                else
                {
                    colors[i] = Color.Black;
                }
            }
            string extra_text = string.Format("{0:0.00}", settings.overlay_opacity);
            Drawer.DrawInfoBoxShapes(spriteBatch, colors, 10, 10, new int[] { 4, 4, 4, 4 }, "Overlay", width: 200, extra_text: extra_text);
        }

        private void DrawRadius(SpriteBatch spriteBatch, Vector2 tile, Color color, int radius, int borderSize = 2, int shaveSide = 0)
        {
            // There's a much more efficient way to do this.
            HashSet<Vector2> tiles = new HashSet<Vector2>();
            for (int i = -radius; i < radius+1; i++)
            {
                for (int j = -radius; j < radius+1; j++)
                {
                    tiles.Add(tile + new Vector2(i, j));
                }
            }
            for (int i = -radius; i < radius + 1; i++)
            {
                for (int j = -radius; j < radius + 1; j++)
                {
                    DrawEdgeBorders(spriteBatch, tile + new Vector2(i, j), color, tiles, borderSize, shaveSide);
                }
            }
        }

        private void DrawEdgeBorders(SpriteBatch spriteBatch, Vector2 tile, Color color, HashSet<Vector2> tiles, int borderSize, int shaveSide = 0)
        {
            float screenX = tile.X * 64f - (float)Game1.viewport.X;
            float screenY = tile.Y * 64f - (float)Game1.viewport.Y;
            float tileSize = 64f;
            //DrawHelper.DrawLine(spriteBatch, screenX, screenY + borderSize / 2, new Vector2(tileSize, borderSize), color);
            //DrawHelper.DrawLine(spriteBatch, screenX, screenY + tileSize - +borderSize / 2, new Vector2(tileSize, borderSize), color);
            //DrawHelper.DrawLine(spriteBatch, screenX + borderSize / 2, screenY, new Vector2(borderSize, tileSize), color);
            //DrawHelper.DrawLine(spriteBatch, screenX + tileSize - borderSize / 2, screenY, new Vector2(borderSize, tileSize), color);
            
            if (!tiles.Contains(new Vector2(tile.X, tile.Y - 1f)))
            {
                DrawHelper.DrawLine(spriteBatch, screenX + shaveSide/2, screenY, new Vector2(tileSize - shaveSide, borderSize), color);
            }
            if (!tiles.Contains(new Vector2(tile.X, tile.Y + 1f)))
            {
                DrawHelper.DrawLine(spriteBatch, screenX + shaveSide / 2, screenY + tileSize, new Vector2(tileSize - shaveSide, borderSize), color);
            }
            if (!tiles.Contains(new Vector2(tile.X - 1f, tile.Y)))
            {
                DrawHelper.DrawLine(spriteBatch, screenX, screenY + shaveSide / 2, new Vector2(borderSize, tileSize - shaveSide), color);
            }
            if (!tiles.Contains(new Vector2(tile.X + 1f, tile.Y)))
            {
                DrawHelper.DrawLine(spriteBatch, screenX + tileSize, screenY + shaveSide / 2, new Vector2(borderSize, tileSize - shaveSide), color);
            }
        }
    }
}
