using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley.Delegates;
using StardewValley.Enchantments;
using StardewValley.Internal;
using StardewValley.Inventories;
using StardewValley.Tools;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;

namespace RegenerativeAgriculture
{
    [XmlType("Mods_Juicyslew_RegenerativeAgriculture_SoilSurveyor")]
    public class SoilSurveyor : GenericTool
    {
        private SharedData shared_data;
        public override string DisplayName => GetDisplayName();
        public string Description { get; set; }

        public override string TypeDefinitionId => "(JS_RA_SS)";

        //public Inventory Inventory => netInventory.Value;

        //public readonly NetRef<Inventory> netInventory = new(new());

        public readonly NetString soilSurveyorName = new();

        [XmlIgnore]
        public readonly NetBool isOpen = new(false);
        string[] denied_messages = new string[]
        {
            "The ground is too hard.",
            "This ground is not for your crops.",
            "This ground doesn't like you.",
            "The ground looks annoyed at your prodding.",
            "The ground doesn't want to share it's secrets.",
            "You think you hear the ground say 'Ow'.",
            "Bruh, this ain't farmable."
        };

        public SoilSurveyor()
        {
            NetFields.AddField(soilSurveyorName)
                .AddField(isOpen);

            InstantUse = false;
            shared_data = SharedData.GetInstance();
        }
        public SoilSurveyor(string type)
        : this()
        {
            ItemId = type;
            ReloadData();
        }

        private string GetDisplayName()
        {
            try
            {
                if (!string.IsNullOrEmpty(soilSurveyorName.Value))
                    return soilSurveyorName.Value;

                var data = Game1.content.Load<Dictionary<string, SoilSurveyorData>>("Juicyslew.RegenerativeAgriculture/SoilSurveyor");
                return data[ItemId].DisplayName;
            }
            catch (Exception e)
            {
                return "Error Item";
            }
        }

        public void ReloadData()
        {
            var data = Game1.content.Load<Dictionary<string, SoilSurveyorData>>("Juicyslew.RegenerativeAgriculture/SoilSurveyor");
            Name = ItemId;
            Description = data[ItemId].Description;
        }

        public override bool canThisBeAttached(StardewValley.Object o)
        {
            return false;
        }

        public override string getDescription()
        {
            Description = loadDescription();
            return Game1.parseText(Description.Replace('^', '\n'), Game1.smallFont, getDescriptionWidth());
        }

        public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
        {
            base.DoFunction(location, x, y, power, who);
            int base_energy_cost = 6 - upgradeLevel.Value;
            if (!base.isEfficient)
            {
                who.Stamina -= (float)(base_energy_cost * power) - (float)who.FarmingLevel * 0.1f;
            }
            int tileX = x / 64;
            int tileY = y / 64;
            //Rectangle tileRect = new Rectangle(tileX * 64, tileY * 64, 64, 64);
            Vector2 tile = new Vector2(tileX, tileY);
            if (!shared_data.soil_health_data.ContainsKey(location))
            {
                string denied_message = Game1.random.Choose(denied_messages);
                Game1.drawObjectDialogue(denied_message);
                return;
            }
            location.performToolAction(this, tileX, tileY);

            // Set Surveyed tile here.
            shared_data.surveyed_tiles[location].Add(tile);

            // TODO: Check that there's no building in the way of your swing.

            //if (location.terrainFeatures.TryGetValue(tile, out var terrainFeature) && terrainFeature.performToolAction(this, 0, tile))
            //{
            //    location.terrainFeatures.Remove(tile);
            //}
            //if (location.largeTerrainFeatures != null)
            //{
            //    for (int i = location.largeTerrainFeatures.Count - 1; i >= 0; i--)
            //    {
            //        LargeTerrainFeature largeFeature = location.largeTerrainFeatures[i];
            //        if (largeFeature.getBoundingBox().Intersects(tileRect) && largeFeature.performToolAction(this, 0, tile))
            //        {
            //            location.largeTerrainFeatures.RemoveAt(i);
            //        }
            //    }
            //}
            //Vector2 toolTilePosition = new Vector2(tileX, tileY);
            //if (location.Objects.TryGetValue(toolTilePosition, out var obj) && obj.Type != null && obj.performToolAction(this))
            //{
            //    if (obj.Type == "Crafting" && (int)obj.fragility != 2)
            //    {
            //        location.debris.Add(new Debris(obj.QualifiedItemId, who.GetToolLocation(), Utility.PointToVector2(who.StandingPixel)));
            //    }
            //    obj.performRemoveAction();
            //    location.Objects.Remove(toolTilePosition);
            //}
        }

        protected override Item GetOneNew()
        {
            return new SoilSurveyor();
        }

        protected override void GetOneCopyFrom(Item source)
        {
            var soil_surveyor = source as SoilSurveyor;
            base.GetOneCopyFrom(source);

            ItemId = source.ItemId;
            ReloadData();
        }

        public override bool canBeTrashed()
        {
            return false;
        }

        public override bool canBeGivenAsGift()
        {
            return false;
        }

        public override bool canBeDropped()
        {
            return false;
        }

        public override bool CanAddEnchantment(BaseEnchantment enchantment)
        {
            return false;
        }

        public override bool CanUseOnStandingTile()
        {
            return true;
        }
    }
}
