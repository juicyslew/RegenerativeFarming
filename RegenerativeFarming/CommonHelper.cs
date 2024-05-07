using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;

namespace RegenerativeAgriculture
{
    // Code taken from Automate mod by Pathoschild
    internal static class CommonHelper
    {
        private static readonly Lazy<Texture2D> LazyPixel = new Lazy<Texture2D>(delegate
        {
            Texture2D texture2D = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            texture2D.SetData(new Color[1] { Color.White });
            return texture2D;
        });

        public const int ButtonBorderWidth = 16;

        // public static readonly Vector2 ScrollEdgeSize = new Vector2(CommonSprites.Scroll.TopLeft.Width * 4, CommonSprites.Scroll.TopLeft.Height * 4);

        public static Texture2D Pixel => CommonHelper.LazyPixel.Value;

        public static IEnumerable<TValue> GetEnumValues<TValue>() where TValue : struct
        {
            return Enum.GetValues(typeof(TValue)).Cast<TValue>();
        }

        public static IEnumerable<GameLocation> GetLocations(bool includeTempLevels = false)
        {
            IEnumerable<GameLocation> locations = Game1.locations.Concat(from location in Game1.locations
                                                                         from indoors in location.GetInstancedBuildingInteriors()
                                                                         select indoors);
            if (includeTempLevels)
            {
                locations = locations.Concat(MineShaft.activeMines).Concat(VolcanoDungeon.activeLevels);
            }
            return locations;
        }

        public static Vector2 GetPlayerTile(Farmer? player)
        {
            Vector2 position = player?.Position ?? Vector2.Zero;
            return new Vector2((int)(position.X / 64f), (int)(position.Y / 64f));
        }

        public static bool IsItemId(string itemId, bool allowZero = true)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
            {
                if (int.TryParse(itemId, out var id))
                {
                    return id >= ((!allowZero) ? 1 : 0);
                }
                return true;
            }
            return false;
        }

        public static float GetSpaceWidth(SpriteFont font)
        {
            return font.MeasureString("A B").X - font.MeasureString("AB").X;
        }

        public static void Draw(this SpriteBatch batch, Texture2D sheet, Rectangle sprite, int x, int y, int width, int height, Color? color = null)
        {
            batch.Draw(sheet, new Rectangle(x, y, width, height), sprite, color ?? Color.White);
        }

        public static Vector2 DrawHoverBox(SpriteBatch spriteBatch, string label, in Vector2 position, float wrapWidth)
        {
            SpriteFont smallFont = Game1.smallFont;
            Vector2 position2 = position + new Vector2(20f);
            Color? color = null;
            Vector2 labelSize = spriteBatch.DrawTextBlock(smallFont, label, in position2, wrapWidth, in color);
            IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), (int)position.X, (int)position.Y, (int)labelSize.X + 27 + 20, (int)labelSize.Y + 27, Color.White);
            SpriteFont smallFont2 = Game1.smallFont;
            position2 = position + new Vector2(20f);
            color = null;
            spriteBatch.DrawTextBlock(smallFont2, label, in position2, wrapWidth, in color);
            return labelSize + new Vector2(27f);
        }

        public static void DrawTab(SpriteBatch spriteBatch, int x, int y, int innerWidth, int innerHeight, out Vector2 innerDrawPosition, int align = 0, float alpha = 1f, bool forIcon = false, bool drawShadow = true)
        {
            int outerWidth = innerWidth + 32;
            int outerHeight = innerHeight + 21;
            int offsetX = align switch
            {
                1 => -outerWidth / 2,
                2 => -outerWidth,
                _ => 0,
            };
            int iconOffsetX = (forIcon ? (-4) : 0);
            int iconOffsetY = (forIcon ? (-8) : 0);
            innerDrawPosition = new Vector2(x + 16 + offsetX + iconOffsetX, y + 16 + iconOffsetY);
            IClickableMenu.drawTextureBox(spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x + offsetX, y, outerWidth, outerHeight + 4, Color.White * alpha, 1f, drawShadow);
        }

        /*public static void DrawButton(SpriteBatch spriteBatch, in Vector2 position, in Vector2 contentSize, out Vector2 contentPos, out Rectangle bounds, int padding = 0)
        {
            CommonHelper.DrawContentBox(spriteBatch, CommonSprites.Button.Sheet, in CommonSprites.Button.Background, in CommonSprites.Button.Top, in CommonSprites.Button.Right, in CommonSprites.Button.Bottom, in CommonSprites.Button.Left, in CommonSprites.Button.TopLeft, in CommonSprites.Button.TopRight, in CommonSprites.Button.BottomRight, in CommonSprites.Button.BottomLeft, in position, in contentSize, out contentPos, out bounds, padding);
        }*/

        /*public static void DrawScroll(SpriteBatch spriteBatch, in Vector2 position, in Vector2 contentSize, out Vector2 contentPos, out Rectangle bounds, int padding = 5)
        {
            CommonHelper.DrawContentBox(spriteBatch, CommonSprites.Scroll.Sheet, in CommonSprites.Scroll.Background, in CommonSprites.Scroll.Top, in CommonSprites.Scroll.Right, in CommonSprites.Scroll.Bottom, in CommonSprites.Scroll.Left, in CommonSprites.Scroll.TopLeft, in CommonSprites.Scroll.TopRight, in CommonSprites.Scroll.BottomRight, in CommonSprites.Scroll.BottomLeft, in position, in contentSize, out contentPos, out bounds, padding);
        }*/

        public static void DrawContentBox(SpriteBatch spriteBatch, Texture2D texture, in Rectangle background, in Rectangle top, in Rectangle right, in Rectangle bottom, in Rectangle left, in Rectangle topLeft, in Rectangle topRight, in Rectangle bottomRight, in Rectangle bottomLeft, in Vector2 position, in Vector2 contentSize, out Vector2 contentPos, out Rectangle bounds, int padding)
        {
            CommonHelper.GetContentBoxDimensions(topLeft, contentSize, padding, out var innerWidth, out var innerHeight, out var outerWidth, out var outerHeight, out var cornerWidth, out var cornerHeight);
            int x = (int)position.X;
            int y = (int)position.Y;
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth, y + cornerHeight, innerWidth, innerHeight), background, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth, y, innerWidth, cornerHeight), top, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth, y + cornerHeight + innerHeight, innerWidth, cornerHeight), bottom, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x, y + cornerHeight, cornerWidth, innerHeight), left, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth + innerWidth, y + cornerHeight, cornerWidth, innerHeight), right, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x, y, cornerWidth, cornerHeight), topLeft, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x, y + cornerHeight + innerHeight, cornerWidth, cornerHeight), bottomLeft, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth + innerWidth, y, cornerWidth, cornerHeight), topRight, Color.White);
            spriteBatch.Draw(texture, new Rectangle(x + cornerWidth + innerWidth, y + cornerHeight + innerHeight, cornerWidth, cornerHeight), bottomRight, Color.White);
            contentPos = new Vector2(x + cornerWidth + padding, y + cornerHeight + padding);
            bounds = new Rectangle(x, y, outerWidth, outerHeight);
        }

        public static void ShowInfoMessage(string message, int? duration = null)
        {
            Game1.addHUDMessage(new HUDMessage(message, 3)
            {
                noIcon = true,
                timeLeft = (((float?)duration) ?? 3500f)
            });
        }

        public static void ShowErrorMessage(string message)
        {
            Game1.addHUDMessage(new HUDMessage(message, 3));
        }

        /*public static void GetScrollDimensions(Vector2 contentSize, int padding, out int innerWidth, out int innerHeight, out int labelOuterWidth, out int outerHeight, out int borderWidth, out int borderHeight)
        {
            CommonHelper.GetContentBoxDimensions(CommonSprites.Scroll.TopLeft, contentSize, padding, out innerWidth, out innerHeight, out labelOuterWidth, out outerHeight, out borderWidth, out borderHeight);
        }*/

        public static void GetContentBoxDimensions(Rectangle topLeft, Vector2 contentSize, int padding, out int innerWidth, out int innerHeight, out int outerWidth, out int outerHeight, out int borderWidth, out int borderHeight)
        {
            borderWidth = topLeft.Width * 4;
            borderHeight = topLeft.Height * 4;
            innerWidth = (int)(contentSize.X + (float)(padding * 2));
            innerHeight = (int)(contentSize.Y + (float)(padding * 2));
            outerWidth = innerWidth + borderWidth * 2;
            outerHeight = innerHeight + borderHeight * 2;
        }

        public static void DrawLine(this SpriteBatch batch, float x, float y, in Vector2 size, in Color? color = null)
        {
            batch.Draw(CommonHelper.Pixel, new Rectangle((int)x, (int)y, (int)size.X, (int)size.Y), color ?? Color.White);
        }

        public static Vector2 DrawTextBlock(this SpriteBatch batch, SpriteFont font, string? text, in Vector2 position, float wrapWidth, in Color? color = null, bool bold = false, float scale = 1f)
        {
            if (text == null)
            {
                return new Vector2(0f, 0f);
            }
            List<string> words = new List<string>();
            string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (string word2 in array)
            {
                string wordPart = word2;
                int newlineIndex;
                while ((newlineIndex = wordPart.IndexOf(Environment.NewLine, StringComparison.Ordinal)) >= 0)
                {
                    if (newlineIndex == 0)
                    {
                        words.Add(Environment.NewLine);
                        wordPart = wordPart.Substring(Environment.NewLine.Length);
                    }
                    else if (newlineIndex > 0)
                    {
                        words.Add(wordPart.Substring(0, newlineIndex));
                        words.Add(Environment.NewLine);
                        wordPart = wordPart.Substring(newlineIndex + Environment.NewLine.Length);
                    }
                }
                if (wordPart.Length > 0)
                {
                    words.Add(wordPart);
                }
            }
            float xOffset = 0f;
            float yOffset = 0f;
            float lineHeight = font.MeasureString("ABC").Y * scale;
            float spaceWidth = CommonHelper.GetSpaceWidth(font) * scale;
            float blockWidth = 0f;
            float blockHeight = lineHeight;
            foreach (string word in words)
            {
                float wordWidth = font.MeasureString(word).X * scale;
                if (word == Environment.NewLine || (wordWidth + xOffset > wrapWidth && (int)xOffset != 0))
                {
                    xOffset = 0f;
                    yOffset += lineHeight;
                    blockHeight += lineHeight;
                }
                if (!(word == Environment.NewLine))
                {
                    Vector2 wordPosition = new Vector2(position.X + xOffset, position.Y + yOffset);
                    if (bold)
                    {
                        Utility.drawBoldText(batch, word, font, wordPosition, color ?? Color.Black, scale);
                    }
                    else
                    {
                        batch.DrawString(font, word, wordPosition, color ?? Color.Black, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
                    }
                    if (xOffset + wordWidth > blockWidth)
                    {
                        blockWidth = xOffset + wordWidth;
                    }
                    xOffset += wordWidth + spaceWidth;
                }
            }
            return new Vector2(blockWidth, blockHeight);
        }

        public static void InterceptErrors(this IMonitor monitor, string verb, Action action, Action<Exception>? onError = null)
        {
            monitor.InterceptErrors(verb, null, action, onError);
        }

        public static void InterceptErrors(this IMonitor monitor, string verb, string? detailedVerb, Action action, Action<Exception>? onError = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                monitor.InterceptError(ex, verb, detailedVerb);
                onError?.Invoke(ex);
            }
        }

        public static void InterceptError(this IMonitor monitor, Exception ex, string verb, string? detailedVerb = null)
        {
            if (detailedVerb == null)
            {
                detailedVerb = verb;
            }
            monitor.Log($"Something went wrong {detailedVerb}:\n{ex}", LogLevel.Error);
            CommonHelper.ShowErrorMessage("Huh. Something went wrong " + verb + ". The error log has the technical details.");
        }

        public static void RemoveObsoleteFiles(IMod mod, params string[] relativePaths)
        {
            string basePath = mod.Helper.DirectoryPath;
            foreach (string relativePath in relativePaths)
            {
                string fullPath = Path.Combine(basePath, relativePath);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Delete(fullPath);
                        mod.Monitor.Log("Removed obsolete file '" + relativePath + "'.");
                    }
                    catch (Exception ex)
                    {
                        mod.Monitor.Log($"Failed deleting obsolete file '{relativePath}':\n{ex}");
                    }
                }
            }
        }

        public static string GetFileHash(string absolutePath)
        {
            using FileStream stream = File.OpenRead(absolutePath);
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
