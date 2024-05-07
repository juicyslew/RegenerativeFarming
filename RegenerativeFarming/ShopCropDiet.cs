using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI;
using StardewValley.Menus;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RegenerativeAgriculture
{
    // Code taken from UIInfoSuite2: https://github.com/Annosz/UIInfoSuite2
    // Edited by me (Juicyslew)
    internal class ShopCropDiet : IDisposable
    {
        private readonly IModHelper _helper;

        SharedData shared_data;
        ModEntry mod_entry;

        public ShopCropDiet(IModHelper helper)
        {
            this._helper = helper;
            shared_data = SharedData.GetInstance();
            mod_entry = ModEntry.instance;
            this.ToggleOption(shopCropDiets: true);
        }

        public void Dispose()
        {
            this.ToggleOption(shopCropDiets: false);
        }

        public void ToggleOption(bool shopCropDiets)
        {
            this._helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
            if (shopCropDiets)
            {
                this._helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
            }
        }

        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            if (Game1.activeClickableMenu is ShopMenu { hoveredItem: Item hoverItem } menu)
            {
                if (shared_data.crop_diet_dict.ContainsKey(hoverItem.QualifiedItemId)){
                    int[] crop_diet = shared_data.GetCropDiet(hoverItem.QualifiedItemId);
                    string crop_name = ItemRegistry.GetData(hoverItem.QualifiedItemId).DisplayName;
                    string title = $"{crop_name} Diet";
                    int width = Math.Max(title.Length * 22, 22*12);
                    int xPosition = menu.xPositionOnScreen + 230 - width;
                    int yPosition = menu.yPositionOnScreen + 548;
                    Drawer.DrawInfoBox(Game1.spriteBatch, shared_data.measure_colors, xPosition, yPosition, crop_diet, title, width);
                }
            }
        }
    }
}
