using Microsoft.Xna.Framework.Graphics;
using StardewValley.ItemTypeDefinitions;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace RegenerativeAgriculture
{
    public class SoilSurveyorDataDefinition : BaseItemDataDefinition
    {
        public SoilSurveyorDataDefinition()
        {
        }

        internal static SoilSurveyorData GetSpecificData(string id)
        {
            return Game1.content.Load<Dictionary<string, SoilSurveyorData>>("Juicyslew.RegenerativeAgriculture/SoilSurveyor")[id];
        }

        public override string Identifier => "(JS_RA_SS)";

        public override string StandardDescriptor => "JS_RA_SS";


        public override IEnumerable<string> GetAllIds()
        {
            return Game1.content.Load<Dictionary<string, SoilSurveyorData>>("Juicyslew.RegenerativeAgriculture/SoilSurveyor").Keys;
        }

        public override bool Exists(string itemId)
        {
            return Game1.content.Load<Dictionary<string, SoilSurveyorData>>("Juicyslew.RegenerativeAgriculture/SoilSurveyor").ContainsKey(itemId);
        }

        public override ParsedItemData GetData(string itemId)
        {
            var data = GetSpecificData(itemId);
            ModEntry.instance.Monitor.Log(itemId, StardewModdingAPI.LogLevel.Debug);
            return new ParsedItemData(this, itemId, data.TextureIndex, data.Texture, "SoilSurveyor." + itemId, data.DisplayName, data.Description, StardewValley.Object.toolCategory, null, data, false);
        }

        public override Item CreateItem(ParsedItemData data)
        {
            return new SoilSurveyor(data.ItemId);
        }

        public override Rectangle GetSourceRect(ParsedItemData data, Texture2D texture, int spriteIndex)
        {
            int w = texture.Width / 16;
            return new Rectangle(spriteIndex % w * 16, spriteIndex / w * 16, 16, 16);
        }
    }
}
