using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace RegenerativeAgriculture
{
    // Taken from the Automate mod and edited.
    internal abstract class BaseOverlay : IDisposable
    {
        private readonly IModEvents Events;

        protected readonly IInputHelper InputHelper;

        protected readonly IReflectionHelper Reflection;

        private readonly int ScreenId;

        private xTile.Dimensions.Rectangle LastViewport;

        private readonly Func<bool>? KeepAliveCheck;

        private readonly bool? AssumeUiMode;

        public virtual void Dispose()
        {
            this.Events.Display.RenderedActiveMenu -= OnRendered;
            this.Events.Display.RenderedWorld -= OnRenderedWorld;
            this.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            this.Events.Input.ButtonPressed -= OnButtonPressed;
            this.Events.Input.ButtonsChanged -= OnButtonsChanged;
            this.Events.Input.CursorMoved -= OnCursorMoved;
            this.Events.Input.MouseWheelScrolled -= OnMouseWheelScrolled;
        }

        protected BaseOverlay(IModEvents events, IInputHelper inputHelper, IReflectionHelper reflection, Func<bool>? keepAlive = null, bool? assumeUiMode = null)
        {
            this.Events = events;
            this.InputHelper = inputHelper;
            this.Reflection = reflection;
            this.KeepAliveCheck = keepAlive;
            this.LastViewport = new xTile.Dimensions.Rectangle(Game1.uiViewport.X, Game1.uiViewport.Y, Game1.uiViewport.Width, Game1.uiViewport.Height);
            this.ScreenId = Context.ScreenId;
            this.AssumeUiMode = assumeUiMode;
            events.GameLoop.UpdateTicked += OnUpdateTicked;
            if (this.IsMethodOverridden("DrawUi"))
            {
                events.Display.RenderedActiveMenu += OnRendered;
            }
            if (this.IsMethodOverridden("DrawWorld"))
            {
                events.Display.RenderedWorld += OnRenderedWorld;
            }
            if (this.IsMethodOverridden("ReceiveLeftClick"))
            {
                events.Input.ButtonPressed += OnButtonPressed;
            }
            if (this.IsMethodOverridden("ReceiveButtonsChanged"))
            {
                events.Input.ButtonsChanged += OnButtonsChanged;
            }
            if (this.IsMethodOverridden("ReceiveCursorHover"))
            {
                events.Input.CursorMoved += OnCursorMoved;
            }
            if (this.IsMethodOverridden("ReceiveScrollWheelAction"))
            {
                events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
            }
        }

        protected virtual void DrawUi(SpriteBatch batch)
        {
        }

        protected virtual void DrawWorld(SpriteBatch batch)
        {
        }

        protected virtual bool ReceiveLeftClick(int x, int y)
        {
            return false;
        }

        protected virtual void ReceiveButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
        }

        protected virtual bool ReceiveScrollWheelAction(int amount)
        {
            return false;
        }

        protected virtual bool ReceiveCursorHover(int x, int y)
        {
            return false;
        }

        protected virtual void ReceiveGameWindowResized()
        {
        }

        protected void DrawCursor()
        {
            if (!Game1.options.hardwareCursor)
            {
                Vector2 cursorPos = new Vector2(Game1.getMouseX(), Game1.getMouseY());
                if (Constants.TargetPlatform == GamePlatform.Android)
                {
                    cursorPos *= Game1.options.zoomLevel / this.Reflection.GetProperty<float>(typeof(Game1), "NativeZoomLevel").GetValue();
                }
                Game1.spriteBatch.Draw(Game1.mouseCursors, cursorPos, Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, Game1.options.SnappyMenus ? 44 : 0, 16, 16), Color.White * Game1.mouseCursorTransparency, 0f, Vector2.Zero, 4f + Game1.dialogueButtonScale / 150f, SpriteEffects.None, 1f);
            }
        }

        private void OnRendered(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (Context.ScreenId == this.ScreenId)
            {
                this.DrawUi(Game1.spriteBatch);
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (Context.ScreenId == this.ScreenId)
            {
                this.DrawWorld(e.SpriteBatch);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (Context.ScreenId == this.ScreenId)
            {
                if (this.KeepAliveCheck != null && !this.KeepAliveCheck())
                {
                    this.Dispose();
                    return;
                }
                xTile.Dimensions.Rectangle newViewport = Game1.uiViewport;
                if (this.LastViewport.Width != newViewport.Width || this.LastViewport.Height != newViewport.Height)
                {
                    newViewport = new xTile.Dimensions.Rectangle(newViewport.X, newViewport.Y, newViewport.Width, newViewport.Height);
                    this.ReceiveGameWindowResized();
                    this.LastViewport = newViewport;
                }
            }
            else if (!Context.HasScreenId(this.ScreenId))
            {
                this.Dispose();
            }
        }

        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (Context.ScreenId == this.ScreenId)
            {
                this.ReceiveButtonsChanged(sender, e);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (Context.ScreenId == this.ScreenId && (e.Button == SButton.MouseLeft || e.Button.IsUseToolButton()))
            {
                bool uiMode = this.AssumeUiMode ?? Game1.uiMode;
                bool handled;
                if (Constants.TargetPlatform == GamePlatform.Android)
                {
                    float nativeZoomLevel = this.Reflection.GetProperty<float>(typeof(Game1), "NativeZoomLevel").GetValue();
                    handled = this.ReceiveLeftClick((int)((float)Game1.getMouseX() * Game1.options.zoomLevel / nativeZoomLevel), (int)((float)Game1.getMouseY() * Game1.options.zoomLevel / nativeZoomLevel));
                }
                else
                {
                    handled = this.ReceiveLeftClick(Game1.getMouseX(uiMode), Game1.getMouseY(uiMode));
                }
                if (handled)
                {
                    this.InputHelper.Suppress(e.Button);
                }
            }
        }

        private void OnMouseWheelScrolled(object? sender, MouseWheelScrolledEventArgs e)
        {
            if (Context.ScreenId == this.ScreenId && this.ReceiveScrollWheelAction(e.Delta))
            {
                MouseState cur = Game1.oldMouseState;
                Game1.oldMouseState = new MouseState(cur.X, cur.Y, e.NewValue, cur.LeftButton, cur.MiddleButton, cur.RightButton, cur.XButton1, cur.XButton2);
            }
        }

        private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
        {
            if (Context.ScreenId == this.ScreenId)
            {
                bool uiMode = this.AssumeUiMode ?? Game1.uiMode;
                if (this.ReceiveCursorHover(Game1.getMouseX(uiMode), Game1.getMouseY(uiMode)))
                {
                    Game1.InvalidateOldMouseMovement();
                }
            }
        }

        private bool IsMethodOverridden(string name)
        {
            MethodInfo method = base.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException($"Can't find method {base.GetType().FullName}.{name}.");
            }
            return method.DeclaringType != typeof(BaseOverlay);
        }
    }
}
