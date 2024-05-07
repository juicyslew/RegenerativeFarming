using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RegenerativeAgriculture
{
    internal class SoilSurveyor : Tool
    {
        private SharedData shared_data;

        public SoilSurveyor()
            : base("SoilSurveyor", 0, 0, 0, stackable: false)
        {
            shared_data = SharedData.GetInstance();
        }

        /// <inheritdoc />
        protected override void initNetFields()
        {
            base.initNetFields();
        }

        /// <inheritdoc />
        protected override void MigrateLegacyItemId()
        {
            switch (base.UpgradeLevel)
            {
                case 0:
                    base.ItemId = "ProddingStick";
                    break;
                case 1:
                    base.ItemId = "CopperStake";
                    break;
                case 2:
                    base.ItemId = "SteelSpike";
                    break;
                case 3:
                    base.ItemId = "GoldMeasuringRod";
                    break;
                case 4:
                    base.ItemId = "IridiumSoilSensor";
                    break;
                default:
                    base.ItemId = "ProddingStick";
                    break;
            }
        }

        /// <inheritdoc />
        protected override Item GetOneNew()
        {
            return new SoilSurveyor();
        }

        public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
        {
            this.Update(who.FacingDirection, 0, who);
            who.EndUsingTool();
            return true;
        }

        public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
        {
            base.DoFunction(location, x, y, power, who);
            int base_energy_cost = 4 - (int)((upgradeLevel + 1) / 2);
            if (!base.isEfficient)
            {
                who.Stamina -= (float)(base_energy_cost * power) - (float)who.FarmingLevel * 0.1f;
            }
            int tileX = x / 64;
            int tileY = y / 64;
            Rectangle tileRect = new Rectangle(tileX * 64, tileY * 64, 64, 64);
            Vector2 tile = new Vector2(tileX, tileY);
            if (location != Game1.getFarm())
            {
                Game1.drawObjectDialogue("This soil doesn't know you well enough to let you survey it.");
                return;
            }
            location.performToolAction(this, tileX, tileY);

            // Set Surveyed tile here.
            shared_data.surveyed_tiles.Add(tile);

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
    }
}
