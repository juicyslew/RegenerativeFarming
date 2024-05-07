using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace RegenerativeAgriculture
{
    // Code Taken from Automate mod by Pathoschild
    internal static class DrawHelper
    {
        public static float GetSpaceWidth(SpriteFont font)
        {
            return CommonHelper.GetSpaceWidth(font);
        }

        public static void DrawSprite(this SpriteBatch spriteBatch, Texture2D sheet, Rectangle sprite, float x, float y, Vector2 errorSize, Color? color = null, float scale = 1f)
        {
            try
            {
                spriteBatch.Draw(sheet, new Vector2(x, y), sprite, color ?? Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            catch
            {
                Utility.DrawErrorTexture(spriteBatch, new Rectangle((int)x, (int)y, (int)errorSize.X, (int)errorSize.Y), 0f);
            }
        }

        public static void DrawSprite(this SpriteBatch spriteBatch, Texture2D sheet, Rectangle sprite, float x, float y, Point errorSize, Color? color = null, float scale = 1f)
        {
            try
            {
                spriteBatch.Draw(sheet, new Vector2(x, y), sprite, color ?? Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
            catch
            {
                Utility.DrawErrorTexture(spriteBatch, new Rectangle((int)x, (int)y, errorSize.X, errorSize.Y), 0f);
            }
        }

        //public static void DrawSpriteWithin(this SpriteBatch spriteBatch, SpriteInfo? sprite, float x, float y, Vector2 size, Color? color = null)
        //{
        //    try
        //    {
        //        sprite?.Draw(spriteBatch, (int)x, (int)y, size, color);
        //    }
        //    catch
        //    {
        //        Utility.DrawErrorTexture(spriteBatch, new Rectangle((int)x, (int)y, (int)size.X, (int)size.Y), 0f);
        //    }
        //}

        public static void DrawSpriteWithin(this SpriteBatch spriteBatch, Texture2D sheet, Rectangle sprite, float x, float y, Vector2 size, Color? color = null)
        {
            float largestDimension = Math.Max(sprite.Width, sprite.Height);
            float scale = size.X / largestDimension;
            float leftOffset = Math.Max((size.X - (float)sprite.Width * scale) / 2f, 0f);
            float topOffset = Math.Max((size.Y - (float)sprite.Height * scale) / 2f, 0f);
            spriteBatch.DrawSprite(sheet, sprite, x + leftOffset, y + topOffset, size, color ?? Color.White, scale);
        }

        public static void DrawLine(this SpriteBatch batch, float x, float y, Vector2 size, Color? color = null)
        {
            batch.Draw(CommonHelper.Pixel, new Rectangle((int)x, (int)y, (int)size.X, (int)size.Y), color ?? Color.White);
        }
    }
}
