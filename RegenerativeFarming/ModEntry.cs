using System;
using System.CodeDom.Compiler;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Shops;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using HarmonyLib;
using SpaceShared.APIs;
using StardewValley.Tools;
using System.Collections.Generic;
using StardewValley.GameData.WildTrees;
using System.ComponentModel;
using StardewMods.CustomBush.Framework.Models;
using StardewMods.CustomBush.Framework;
using StardewMods.Common.Services.Integrations.CustomBush;
using StardewValley.GameData.Locations;
using xTile.Layers;
using Force.DeepCloner;
using StardewMods.CustomBush;
using StardewValley.GameData.Crops;
using static HarmonyLib.Code;

namespace RegenerativeAgriculture
{


    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {

        public static ModEntry instance;

        private string[] surveyor_tools = new string[]
        {
            "SoilSurveyor.T0",
            "SoilSurveyor.T1",
            "SoilSurveyor.T2",
            "SoilSurveyor.T3",
            "SoilSurveyor.T4"
        };

        private string[] surveyor_tool_names = new string[]
        {
            "Prodding Stick",
            "Copper Stake",
            "Steel Spike",
            "Gold Measuring Rod",
            "Iridium Soil Scanner"
        };

        private string[] surveyor_tool_descriptions = new string[]
        {
            "For poking around at the soil",
            "Measures basic soil health information",
            "Easy to poke deep into the soil for accurate soil health",
            "Slim gold rod with measurement tools snuggly packed inside",
            "Scans soil with complex electronics and sensors.  How did Clint build this..?"
        };

        private string[] hoe_tools = new string[]
        {
            "Hoe",
            "CopperHoe",
            "SteelHoe",
            "GoldHoe",
            "IridiumHoe"
        };

        //private static Dictionary<string, int[]> crop_diet_dict = new()
        //{
        //    { "273", new int[]                          { -1, -1, 0, 0 } },     // Parsnip 
        //    { "299", new int[]                          { -1, -1, 0, 0 } },     // Parsnip 
        //    { "301", new int[]                          { -1, -1, 0, 0 } },     // Parsnip 
        //    { "302", new int[]                          { -1, -1, 0, 0 } },     // Parsnip 
        //    { "347", new int[]                          { -1, -1, 0, 0 } },     // Parsnip 
        //    { "425", new int[]                          { -1, -1, 0, 0 } },     // Parsnip
        //    { "427", new int[]                          { -3, -2, 0, 0 } },     // Tulip
        //    { "429", new int[]                          { -2, 0, -2, 0 } },     // Blue Jazz
        //    { "431", new int[]                          { -1, -1, 0, 0 } },     // Parsnip 
        //    { "433", new int[]                          { -1, -1, 0, 0 } },     // Parsnip 
        //    { "453", new int[]                          { -1, -1, 0, 0 } },     // Parsnip 
        //    { "455", new int[]                          { -1, -1, 0, 0 } },     // Parsnip
        //    { "472", new int[]                          { -1, -1, 0, 0 } },     // Parsnip 

        //    { "477", new int[]                          { 2, 0, 0, 0 } },       // Kale
        //    { "473", new int[]                          { 2, 1, 0, 0 } },       // Green Bean
        //    { "474", new int[]                          { 2, 0, 1, 0 } },       // Cauliflower
        //    { "475", new int[]                          { 3, 1, 1, 0 } },       // Potato
        //    { "Cornucopia_BasilSeeds", new int[]        { 2, 0, 2, 0 } },       // Basil
        //    { "Cornucopia_BellPepperSeeds", new int[]   { 1, 2, 0, 0 } },       // Bell Pepper
        //    { "Cornucopia_TurnipSeeds", new int[]       { 0, 3, 0, 0 } },       // Turnip
        //    { "Cornucopia_CottonBollSeeds", new int[]   { 3, 1, 0, 0 } },       // Cotton
        //    { "Cornucopia_CucumberSeeds", new int[]     { 2, 2, 0, 0 } },       // Cucumber
        //    { "Cornucopia_KiwiSeeds", new int[]         { 0, 0, 2, 0 } },       // Kiwi
        //    { "Cornucopia_LettuceSeeds", new int[]      { 5, 0, 0, 0 } },       // Lettuce
        //    { "Cornucopia_OliveSeeds", new int[]        { 1, 0, 1, 0 } },       // Olive
        //    { "Cornucopia_OnionSeeds", new int[]        { 1, 1, 1, 1 } },       // Onion

        //    // Unmilled Rice
        //    // Strawberry
        //    // Ancient fruit
        //    // Foragables??
        //};

        private readonly PerScreen<OverlayMenu?> CurrentOverlay = new();

        Random random = new Random(Guid.NewGuid().GetHashCode());
        SharedData? shared_data;
        ShopCropDiet shop_crop_diet_ui;
        public Texture2D health_icons;
        public Texture2D aoe_icons;
        public Texture2D caution_icons;
        public Texture2D health_bars;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        ///
        /// Nothing here.
        /// 

        public ICustomBushApi customBushApi;

        public override void Entry(IModHelper helper)
        {
            instance = this;
            helper.Events.GameLoop.DayEnding += OnDayEnd;
            helper.Events.Display.RenderedHud += OnRenderedHud;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.ReturnedToTitle += GameLoop_ReturnedToTitle;
            helper.Events.Content.AssetRequested += Content_AssetRequested;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            shop_crop_diet_ui = new ShopCropDiet(helper);

            shared_data = SharedData.GetInstance();

            health_icons = helper.GameContent.Load<Texture2D>($"{ModManifest.UniqueID}/2x_soil_measure_icons.png");
            aoe_icons = helper.GameContent.Load<Texture2D>($"{ModManifest.UniqueID}/radius_icons_no_support_lines.png");
            caution_icons = helper.GameContent.Load<Texture2D>($"{ModManifest.UniqueID}/caution_icons.png");
            health_bars = helper.GameContent.Load<Texture2D>($"{ModManifest.UniqueID}/health_bars_2.png");
            // Try to use this for determining when held item is crop
            //crop_data = helper.GameContent.Load<string, string>("Data/Crops");
            //fruitTree_data = helper.GameContent.Load<string, string>("Data/"???);


            // Soil Surveyor Setup
            SoilSurveyorDataDefinition def = new();
            ItemRegistry.ItemTypes.Add(def);
            Helper.Reflection.GetField<Dictionary<string, IItemDataDefinition>>(typeof(ItemRegistry), "IdentifierLookup").GetValue()[def.Identifier] = def;

            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            
        }

        bool last_holding_surveyor = false;


        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        ///

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            var sc = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            customBushApi = Helper.ModRegistry.GetApi<ICustomBushApi>("furyx639.CustomBush");
            sc.RegisterSerializerType(typeof(SoilSurveyor));
        }

        private void GameLoop_ReturnedToTitle(object sender, ReturnedToTitleEventArgs e)
        {
            shared_data.ResetData();
        }

        


        private void Content_AssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}/SoilSurveyor"))
            {
                e.LoadFrom(() =>
                {
                    Dictionary<string, SoilSurveyorData> ret = new();
                    for (int i = 0; i < 5; ++i)
                    {
                        ret.Add($"SoilSurveyor.T{i}",
                            new SoilSurveyorData()
                            {
                                TextureIndex = i*7 + 5, // TODO: tmp
                                DisplayName = surveyor_tool_names[i],
                                Description = surveyor_tool_descriptions[i]
                            });
                    }
                    return ret;
                }, StardewModdingAPI.Events.AssetLoadPriority.Exclusive);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}/soil_surveyor_placeholder.png"))
            {
                e.LoadFromModFile<Texture2D>("assets/soil_surveyor_placeholder.png", StardewModdingAPI.Events.AssetLoadPriority.Low);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("spacechase0.SpaceCore/ObjectExtensionData"))
            {
                e.Edit((asset) =>
                {
                    var data = asset.AsDictionary<string, SpaceCore.VanillaAssetExpansion.ObjectExtensionData>().Data;
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit((asset) =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    int i = 0;
                });
            }else if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}/2x_soil_measure_icons.png"))
            {
                e.LoadFromModFile<Texture2D>("assets/2x_soil_measure_icons.png", StardewModdingAPI.Events.AssetLoadPriority.Low);
            }else if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}/radius_icons_no_support_lines.png"))
            {
                e.LoadFromModFile<Texture2D>("assets/radius_icons_no_support_lines.png", StardewModdingAPI.Events.AssetLoadPriority.Low);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}/caution_icons.png"))
            {
                e.LoadFromModFile<Texture2D>("assets/caution_icons.png", StardewModdingAPI.Events.AssetLoadPriority.Low);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo($"{ModManifest.UniqueID}/health_bars_2.png"))
            {
                e.LoadFromModFile<Texture2D>("assets/health_bars_2.png", StardewModdingAPI.Events.AssetLoadPriority.Low);
            }
            //else if (e.NameWithoutLocale.IsEquivalentTo("Data/Shops"))
            //{
            //    e.Edit((asset) =>
            //    {
            //        var data = asset.AsDictionary<string, ShopData>().Data;
            //        //data["Blacksmith"].Items.Add(new()
            //        //{
            //        //    Id = "Soil Surveyor 0",
            //        //    Price = 100,
            //        //    TradeItemId = null,
            //        //    TradeItemAmount = 0,
            //        //    ItemId = "(JS_RA_SS)SoilSurveyor.T0",
            //        //});
            //        //data["Blacksmith"].Items.Add(new()
            //        //{
            //        //    Id = "Soil Surveyor 1",
            //        //    Price = 2000,
            //        //    TradeItemId = "334",
            //        //    TradeItemAmount = 5,
            //        //    ItemId = "(JS_RA_SS)SoilSurveyor.T1",
            //        //});
            //        //data["Blacksmith"].Items.Add(new()
            //        //{
            //        //    Id = "Soil Surveyor 2",
            //        //    Price = 5000,
            //        //    TradeItemId = "335",
            //        //    TradeItemAmount = 5,
            //        //    ItemId = "(JS_RA_SS)SoilSurveyor.T2",
            //        //});
            //        //data["Blacksmith"].Items.Add(new()
            //        //{
            //        //    Id = "Soil Surveyor 3",
            //        //    Price = 10000,
            //        //    TradeItemId = "336",
            //        //    TradeItemAmount = 5,
            //        //    ItemId = "(JS_RA_SS)SoilSurveyor.T3",
            //        //});
            //        //data["Blacksmith"].Items.Add(new()
            //        //{
            //        //    Id = "Soil Surveyor 4",
            //        //    Price = 25000,
            //        //    TradeItemId = "337",
            //        //    TradeItemAmount = 5,
            //        //    ItemId = "(JS_RA_SS)SoilSurveyor.T4",
            //        //});
            //    });
            //}
        }

        private void DisableOverlay()
        {
            this.Monitor.Log("Disabling Overlay!", LogLevel.Debug);
            this.CurrentOverlay.Value?.Dispose();
            this.CurrentOverlay.Value = null;
        }

        private void EnableOverlay()
        {
            this.Monitor.Log("Enabling Overlay.", LogLevel.Debug);
            PerScreen<OverlayMenu> currentOverlay = this.CurrentOverlay;
            if (currentOverlay.Value == null)
            {
                this.Monitor.Log("Creating new overlay since none exists!", LogLevel.Debug);
                OverlayMenu overlayMenu2 = (currentOverlay.Value = new OverlayMenu(base.Helper.Events, base.Helper.Input, base.Helper.Reflection));
            }
        }

        private void ResetOverlayIfShown()
        {
            if (this.CurrentOverlay.Value != null)
            {
                this.DisableOverlay();
                this.EnableOverlay();
            }
        }

        

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            // Write surveyed_tiles to save data

            foreach (GameLocation location in shared_data.soil_health_data.Keys)
            {
                location.modData["juicyslew.regenerative_agriculture_soil_health_data"] = string.Join(";", shared_data.soil_health_data[location].Select(kv => $"{kv.Key.X},{kv.Key.Y}|{string.Join(',', kv.Value)}"));
            }
            foreach (GameLocation location in shared_data.soil_health_data.Keys)
            {
                location.modData["juicyslew.regenerative_agriculture_surveyed_tiles"] = string.Join(";", shared_data.surveyed_tiles[location].Select(v => $"{v.X},{v.Y}"));
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            foreach (GameLocation location in Game1.locations)
            {
                if (location.modData.ContainsKey("juicyslew.regenerative_agriculture_soil_health_data"))
                {
                    shared_data.soil_health_data[location] = new();  // Remove any data that should've been unloaded by now.
                                                           // Deserialize
                    string serialized_soil_health_data = location.modData["juicyslew.regenerative_agriculture_soil_health_data"];
                    string[] soil_health_strings = serialized_soil_health_data.Split(';');
                    foreach (string soil_health_string in soil_health_strings)
                    {
                        string[] key_value_pair = soil_health_string.Split('|');
                        string[] key_parts = key_value_pair[0].Split(',');
                        Vector2 tile = new Vector2(int.Parse(key_parts[0]), int.Parse(key_parts[1]));

                        string[] value_parts = key_value_pair[1].Split(',');
                        int[] soil_health = value_parts.Select(i => int.Parse(i)).ToArray();

                        shared_data.SetSoilHealth(location, tile, soil_health);
                    }
                }

                if (location.modData.ContainsKey("juicyslew.regenerative_agriculture_surveyed_tiles"))
                {
                    string serialized_surveyed_tiles = location.modData["juicyslew.regenerative_agriculture_surveyed_tiles"];
                    string[] surveyed_tile_strings = serialized_surveyed_tiles.Split(';');
                    shared_data.surveyed_tiles[location] = surveyed_tile_strings.Select(x => x.Split(",")).Select(x => new Vector2(int.Parse(x[0]), int.Parse(x[1]))).ToHashSet();
                }
            }
        }

        private void OnDayEnd(object? sender, DayEndingEventArgs e)
        {
            foreach (GameLocation location in Game1.locations)
            {
                if (shared_data.soil_health_data.ContainsKey(location))
                {
                    this.Monitor.Log($"Updating Soil Health for {location.Name}", LogLevel.Debug);
                    shared_data.UpdateSoilHealth(location);
                }
            }
        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (Context.IsPlayerFree && Game1.player.ActiveItem is not null)
            {
                // Crop Diet UI (On holding seed)
                Item held_item = Game1.player.ActiveItem;
                bool holding_seed = shared_data.crop_diet_dict.ContainsKey(held_item.QualifiedItemId);
                bool holding_surveyor = surveyor_tools.Contains(held_item.ItemId);
                bool holding_hoe = hoe_tools.Contains(held_item.ItemId);

                GameLocation current_location = Game1.currentLocation;

                // Enable or Disable Overlay
                if (!last_holding_surveyor && holding_surveyor)
                {
                    // DO THIS ASYNC
                    //var func = () => { shared_data.UpdateDeathPreview(current_location); return; };
                    //Action actionDelegate = new Action(func);
                    //Task task1 = new Task(actionDelegate);
                    //task1.Start();
                    shared_data.UpdateDeathPreview(current_location);
                    EnableOverlay();
                }
                if (last_holding_surveyor && !holding_surveyor)
                {
                    DisableOverlay();
                }

                int[]? held_diet = null;
                if (holding_seed)
                {
                    // Show crop diet in top left corner.
                    int xPosition = 10;
                    int yPosition = 10;
                    held_diet = shared_data.GetCropDiet(held_item.QualifiedItemId);
                    bool has_radius_effect = ItemRegistry.GetData(held_item.QualifiedItemId).ObjectType == "Basic";
                    string title = $"{held_item.DisplayName} Diet";
                    Drawer.DrawInfoBox(e.SpriteBatch, shared_data.measure_colors, xPosition, yPosition, held_diet, title, is_diet: true, width: Math.Max(title.Length * 22, 12 * 22), radius: has_radius_effect ? 1 : 0);
                }

                
                if ((holding_seed || holding_surveyor || holding_hoe) && shared_data.soil_health_data.ContainsKey(current_location))
                {
                    // Soil Surveyor UI (On holding Hoe / Seed / Surveyor, and mousing over surveyed tile)
                    bool double_ui_flag = true;
                    int[]? soil_health = null;
                    int[]? preview_health = null;
                    if (shared_data.surveyed_tiles[current_location].Contains(Game1.currentCursorTile))
                    {
                        soil_health = shared_data.GetSoilHealth(current_location, Game1.currentCursorTile);
                        preview_health = shared_data.GetHealthPreview(Game1.currentCursorTile);
                        // If there isn't health preview use the seed we're holding to generate a quick health preview.
                        if (preview_health is null && held_diet is not null)
                        {  
                            preview_health = new int[4];
                            for (int i = 0; i < 4; i++)
                            {
                                preview_health[i] = soil_health[i] - held_diet[i]; // Why clamp this?? //  Math.Min(Math.Max(soil_health[i] - held_diet[i], -1), SharedData.max_health);
                            }
                        }
                    }


                    string title = "Soil Health";
                    int health_box_width = Math.Max(title.Length * 22, 13 * 22);
                    int health_box_height = 200;
                    Vector2 mouse_world_coordinates = Game1.currentViewportTarget + Game1.getMousePosition().ToVector2();
                    int xPosition = mouse_world_coordinates.X < Game1.getPlayerOrEventFarmer().StandingPixel.X ? Game1.getMouseX() - health_box_width - 42 : Game1.getMouseX() + 42;
                    int yPosition = mouse_world_coordinates.Y < Game1.getPlayerOrEventFarmer().StandingPixel.Y ? Game1.getMouseY() - health_box_height - 42 : Game1.getMouseY() + 42;
                    Drawer.DrawInfoBox(e.SpriteBatch, shared_data.measure_colors, xPosition, yPosition, soil_health, title, width: health_box_width, next_health_data: preview_health);
                    // Moused Over Crop Diet UI (For seeing the diet of the crop you're looking at currently!)
                    if (current_location.terrainFeatures.ContainsKey(Game1.currentCursorTile))
                    {
                        TerrainFeature t = current_location.terrainFeatures[Game1.currentCursorTile];
                        string crop_id = null;
                        bool eating = false;
                        // Update Cached next soil health map.
                        if (t is Tree tree)
                        {
                            WildTreeData tree_data = tree.GetData();
                            crop_id = tree_data.SeedItemId;
                            eating = shared_data.IsTreeEating(tree);
                        }
                        else if (t is FruitTree fruit_tree){
                            crop_id = $"(O){fruit_tree.treeId.Value}";
                            eating = shared_data.IsFruitTreeEating(fruit_tree);
                        }
                        else if (t is HoeDirt hoe_dirt && hoe_dirt.crop != null)
                        {
                            crop_id = $"(O){hoe_dirt.crop.netSeedIndex.Value}";
                            eating = shared_data.IsCropEating(hoe_dirt) == EatMode.OnlyTake;
                        }
                        else if (t is Bush bush)
                        {
                            ICustomBush? customBush = null;
                            string customBushId = null;
                            bool is_custom_bush = customBushApi.TryGetCustomBush(bush, out customBush, out customBushId);
                            if (is_custom_bush)
                            {
                                if (customBushId is not null)
                                {
                                    crop_id = customBushId; // Already Qualified
                                }
                            }
                            else
                            {
                                if (bush.size == Bush.greenTeaBush)
                                { 
                                    crop_id = "(O)251";
                                }
                            }
                            eating = shared_data.IsBushEating(bush);
                        }
                        

                        if (crop_id is not null)
                        {
                            string crop_name = ItemRegistry.GetData(crop_id).DisplayName;
                            int[] crop_diet = shared_data.GetCropDiet(crop_id);
                            string diet_title = $"{crop_name} Diet";

                            int diet_box_width = Math.Max(diet_title.Length * 22, 320);
                            int diet_box_height = 200;
                            xPosition = mouse_world_coordinates.X < Game1.getPlayerOrEventFarmer().StandingPixel.X ? Game1.getMouseX() - diet_box_width - 42 : Game1.getMouseX() + 42;
                            int yExtraOffset = double_ui_flag ? diet_box_height : 0;
                            yPosition = mouse_world_coordinates.Y < Game1.getPlayerOrEventFarmer().StandingPixel.Y ? Game1.getMouseY() - diet_box_height - 42 - yExtraOffset : Game1.getMouseY() + 42 + yExtraOffset;

                            Drawer.DrawInfoBox(e.SpriteBatch, shared_data.measure_colors, xPosition, yPosition, crop_diet, diet_title, is_diet: true, width: diet_box_width);
                        }
                    }
                }
                last_holding_surveyor = holding_surveyor;
            }
        }
    }
    [HarmonyPatch(typeof(FruitTree))]
    [HarmonyPatch("IsGrowthBlocked")]
    class FruitTree_IsGrowthBlocked_Patch
    {
        static bool Prefix(ref bool __result, Vector2 tileLocation, GameLocation environment)
        {
            // THIS FULLY REPLACES THE ISGROWTHBLOCKED function.  It will likely cause compatibility issues.  However I'm going to assume that not many people are trying to touch this particular code.
            // TODO: Make this more compatible, perhaps with a postfix somehow.
            Vector2[] surroundingTileLocationsArray = Utility.getSurroundingTileLocationsArray(tileLocation);
            for (int i = 0; i < surroundingTileLocationsArray.Length; i++)
            {
                Vector2 v = surroundingTileLocationsArray[i];
                TerrainFeature terrainFeature;
                bool isTerrainFeature = environment.terrainFeatures.TryGetValue(v, out terrainFeature);
                bool isHoeDirt = isTerrainFeature && terrainFeature is HoeDirt;
                bool isBush = isTerrainFeature && terrainFeature is Bush;
                if (environment.IsTileOccupiedBy(v, ~(CollisionMask.Characters | CollisionMask.Farmers)) && !isHoeDirt && !isBush)
                {
                    StardewValley.Object o = environment.getObjectAtTile((int)v.X, (int)v.Y);
                    if (o == null)
                    {
                        __result = true;
                        return false;
                    }
                    if (!(o.QualifiedItemId == "(O)590"))
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }
    }
    //[HarmonyPatch(typeof(FruitTree))]
    //[HarmonyPatch("IsTooCloseToAnotherTree")]
    //class FruitTree_IsTooCloseToAnotherTree_Patch
    //{
    //    static bool Prefix(ref bool __result, Vector2 tileLocation, GameLocation environment, bool fruitTreesOnly = false)
    //    {
    //        Vector2 v = default(Vector2);
    //        for (int i = (int)tileLocation.X - 1; i <= (int)tileLocation.X + 1; i++)
    //        {
    //            for (int j = (int)tileLocation.Y - 1; j <= (int)tileLocation.Y + 1; j++)
    //            {
    //                v.X = i;
    //                v.Y = j;
    //                if (environment.terrainFeatures.TryGetValue(v, out var feature) && (feature is FruitTree || (!fruitTreesOnly && feature is Tree)))
    //                {
    //                    __result = true;
    //                    return false;
    //                }
    //            }
    //        }
    //        __result = false;
    //        return false;
    //    }
    //}

    static class Drawer
    {
        public static void DrawInfoBoxShapes(SpriteBatch sprite_batch, Color[] measure_colors, int xPosition, int yPosition, int[]? health_data, string titleToRender, bool is_diet = false, int width = 320, string extra_text = null, int[]? next_health_data = null, int radius = 0)
        {
            // Make 0,0 the top left corner
            IClickableMenu.drawTextureBox(Game1.spriteBatch, xPosition, yPosition, width, 180, Color.White);
            sprite_batch.DrawString(Game1.dialogueFont, titleToRender, new Vector2(xPosition + 20, yPosition + 14), Color.Black * 0.2f);
            sprite_batch.DrawString(Game1.dialogueFont, titleToRender, new Vector2(xPosition + 22, yPosition + 16), Color.Black * 0.8f);
            xPosition += 20;
            yPosition += 60;
            // sprite_batch.Draw(Game1.mouseCursors, new Vector2(xPosition, yPosition), new Rectangle(60, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.85f);
            // sprite_batch.Draw(Game1.debrisSpriteSheet, new Vector2(xPosition + 32, yPosition + 10), Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16), Color.White, 0f, new Vector2(8f, 8f), 5f, SpriteEffects.None, 0.95f);
            if (health_data is null)
            { // If I don't have this info, display questionmarks.
                for (int i = 0; i < 4; i++)
                {
                    Color text_color = measure_colors[i];
                    string text = "??";
                    sprite_batch.DrawString(Game1.dialogueFont, text, new Vector2(xPosition, yPosition + 6), text_color * 0.2f);
                    sprite_batch.DrawString(Game1.dialogueFont, text, new Vector2(xPosition + 2, yPosition + 4), text_color * 1.0f);
                    xPosition += (22 * text.Length) + 8;
                    //yPosition += 28;
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    int measure_bar_x = xPosition;
                    int measure_bar_y = yPosition + 26 * i;
                    Color text_color = measure_colors[i];
                    if (next_health_data is not null && next_health_data[i] < health_data[i])
                    {
                        // Draw the preview (but only for taking... figure out what giving looks like at some point.)

                        DrawMeasureIcons(sprite_batch, text_color, measure_bar_x, measure_bar_y, health_data[i], i, true);
                        if (next_health_data[i] <= -1)
                        {
                            // Display Danger Icon
                            sprite_batch.Draw(
                                ModEntry.instance.caution_icons,
                                new Vector2(measure_bar_x + 216, measure_bar_y),
                                new Rectangle(16, 0, 16, 16),
                                Color.White,
                                0f,
                                Vector2.Zero,
                                1.5f,
                                SpriteEffects.None,
                                0.85f
                            );
                        }
                        else
                        {
                            DrawMeasureIcons(sprite_batch, text_color, measure_bar_x, measure_bar_y, next_health_data[i], i, is_diet);
                        }

                    }
                    else
                    {
                        // Just draw so
                        DrawMeasureIcons(sprite_batch, text_color, measure_bar_x, measure_bar_y, health_data[i], i, is_diet);
                    }
                }
                if (is_diet && radius > 0)
                {

                    int spr_width;
                    int spr_height;
                    int spr_x;
                    int spr_y;
                    if (radius == 1)
                    {
                        spr_width = 48;
                        spr_height = 48;
                        spr_x = 0;
                        spr_y = 0;
                    }
                    else
                    {
                        // Radius == 2
                        spr_width = 80;
                        spr_height = 80;
                        spr_x = 48;
                        spr_y = 0;
                    }
                    sprite_batch.Draw(
                        ModEntry.instance.aoe_icons,
                        new Vector2(xPosition + 234, yPosition),
                        new Rectangle(spr_x, spr_y, spr_width, spr_height),
                        Color.Blue * .5f,
                        0f,
                        Vector2.Zero,
                        2f,
                        SpriteEffects.None,
                        0.85f
                    );
                }
            }
        }

        public static void DrawInfoBox(SpriteBatch sprite_batch, Color[] measure_colors, int xPosition, int yPosition, int[]? health_data, string titleToRender, bool is_diet = false, int width = 320, string extra_text = null, int[]? next_health_data = null, int radius = 0)
        {
            // Make 0,0 the top left corner
            if (health_data is null)
            {
                return;
            }
            IClickableMenu.drawTextureBox(Game1.spriteBatch, xPosition, yPosition, width, 200, Color.White);
            sprite_batch.DrawString(Game1.dialogueFont, titleToRender, new Vector2(xPosition + 20, yPosition + 14), Color.Black * 0.2f);
            sprite_batch.DrawString(Game1.dialogueFont, titleToRender, new Vector2(xPosition + 22, yPosition + 16), Color.Black * 0.8f);
            xPosition += 20;
            yPosition += 60;
            // sprite_batch.Draw(Game1.mouseCursors, new Vector2(xPosition, yPosition), new Rectangle(60, 428, 10, 10), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.85f);
            // sprite_batch.Draw(Game1.debrisSpriteSheet, new Vector2(xPosition + 32, yPosition + 10), Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16), Color.White, 0f, new Vector2(8f, 8f), 5f, SpriteEffects.None, 0.95f);
            //if (health_data is null)
            //{ // If I don't have this info, display questionmarks.
            //    for (int i = 0; i < 4; i++)
            //    {
            //        Color text_color = measure_colors[i];
            //        string text = "??";
            //        sprite_batch.DrawString(Game1.dialogueFont, text, new Vector2(xPosition, yPosition + 6), text_color * 0.2f);
            //        sprite_batch.DrawString(Game1.dialogueFont, text, new Vector2(xPosition + 2, yPosition + 4), text_color * 1.0f);
            //        xPosition += (22 * text.Length) + 8;
            //        //yPosition += 28;
            //    }
            //}
            //else
            //{
            for (int i = 0; i < 4; i++)
            {
                int measure_bar_x = xPosition;
                int measure_bar_y = yPosition + 28 * i;
                Color text_color = measure_colors[i];
                if (next_health_data is not null)
                {
                    if (health_data[i] < next_health_data[i])
                    { // If we are gaining health
                        // Draw NEXT health in preview giving format, then draw CURRENT health in giving format over the first.
                        DrawMeasureBars(sprite_batch, text_color, measure_bar_x, measure_bar_y, next_health_data[i], i, false, preview_mode: true);
                        DrawMeasureBars(sprite_batch, text_color, measure_bar_x, measure_bar_y, health_data[i], i, is_diet);
                    }
                    else if (health_data[i] > next_health_data[i])
                    { // If we are losing health.
                        // Draw CURRENT health in taking crop diet format, then draw NEW health in giving format over the first.
                        DrawMeasureBars(sprite_batch, text_color, measure_bar_x, measure_bar_y, health_data[i], i, true, preview_mode: true);
                        if (next_health_data[i] < 0)
                        {
                            // Display Danger Icon
                            sprite_batch.Draw(
                                ModEntry.instance.caution_icons,
                                new Vector2(measure_bar_x + 216, measure_bar_y),
                                new Rectangle(16, 0, 16, 16),
                                Color.White,
                                0f,
                                Vector2.Zero,
                                1.5f,
                                SpriteEffects.None,
                                0.85f
                            );
                        }
                        else
                        {
                            DrawMeasureBars(sprite_batch, text_color, measure_bar_x, measure_bar_y, next_health_data[i], i, is_diet);
                        }
                    }
                    else
                    {
                        // Just draw soil health
                        DrawMeasureBars(sprite_batch, text_color, measure_bar_x, measure_bar_y, health_data[i], i, is_diet);
                    }
                }
                else
                {
                    // Just draw soil health
                    DrawMeasureBars(sprite_batch, text_color, measure_bar_x, measure_bar_y, health_data[i], i, is_diet);
                }
            }
            if (is_diet && radius > 0)
            {

                int spr_width;
                int spr_height;
                int spr_x;
                int spr_y;
                if (radius == 1)
                {
                    spr_width = 48;
                    spr_height = 48;
                    spr_x = 0;
                    spr_y = 0;
                }
                else
                {
                    // Radius == 2
                    spr_width = 80;
                    spr_height = 80;
                    spr_x = 48;
                    spr_y = 0;
                }
                sprite_batch.Draw(
                    ModEntry.instance.aoe_icons,
                    new Vector2(xPosition + 234, yPosition),
                    new Rectangle(spr_x, spr_y, spr_width, spr_height),
                    Color.Blue * .5f,
                    0f,
                    Vector2.Zero,
                    2f,
                    SpriteEffects.None,
                    0.85f
                );
            }
        }

        public static void DrawMeasureIcons(SpriteBatch sprite_batch, Color color, int xpos, int ypos, int amount, int measure, bool is_diet)
        {
            // Render in 4x2 square, Eventually Center align it too.
            bool negative = false;
            if (amount < 0 || !is_diet)
            {
                negative = true;
            }
            amount = Math.Abs(amount);

            int xind = 0;
            int spr_width = 32;
            int spr_height = 32;
            int spr_indx;
            int spr_indy;
            float scale = .75f;
            while (amount > 4)
            {
                // Get correct tilesheet point and draw it based on measure and negative (amount is 4 in all of these cases)

                spr_indx = negative ? 6 : 7;
                spr_indy = measure;
                sprite_batch.Draw(
                    ModEntry.instance.health_icons,
                    new Vector2(xpos + xind * 26, ypos),
                    new Rectangle(spr_width * spr_indx , spr_height * spr_indy, spr_width, spr_height),
                    color * .8f,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0.85f
                );
                xind++;
                amount -= 4;
            }

            // Get correct tilesheet point and draw it based on amount, measure, and negative
            if (amount == 0)
            {
                return;
            }
            spr_indx = (amount-1) * 2 + (negative ? 0 : 1);
            spr_indy = measure;
            sprite_batch.Draw(
                ModEntry.instance.health_icons,
                new Vector2(xpos + xind * 26, ypos),
                new Rectangle(spr_width * spr_indx, spr_height * spr_indy, spr_width, spr_height),
                color * .8f,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0.85f
            );
        }

        public static void DrawMeasureBars(SpriteBatch sprite_batch, Color color, int xpos, int ypos, int amount, int measure, bool is_diet, bool preview_mode = false)
        {
            bool negative = false;
            if (amount < 0 || !is_diet)
            {
                negative = true;
            }
            amount = Math.Abs(amount);
            float fill_amount = (float)amount / SharedData.max_health;

            int spr_width = 208;
            int spr_height = 32;
            int inner_bar_width = 20;
            int inner_bar_length = 196;
            float scale = 1f;

            Color bar_color = new Color(.25f + (color.R / 255f * .75f), .25f + (color.G / 255f * .75f), .25f + (color.B / 255f * .75f), 1f);
            Color fill_color = new Color(.5f + (color.R / 255f / 2f), .5f + (color.G / 255f / 2f), .5f + (color.B / 255f / 2f), 1f);
            fill_color.A = 255;


            int spr_indy = measure;
            // Draw bar content first, then draw bar outline on top.

            // Get correct tilesheet point and draw it based on amount, measure, and negative
            if (amount == 0)
            {
            }
            else {
                // Draw Bar Content (Shaded)

                if (preview_mode && negative)
                { // Draw small bar for preview negatives
                    sprite_batch.Draw(
                        ModEntry.instance.health_bars,
                        new Rectangle(xpos + 6, ypos + 12, (int)(inner_bar_length * fill_amount), inner_bar_width - 14),
                        new Rectangle(4, 11 + spr_indy * spr_height, 1, inner_bar_width - 12),
                        fill_color
                    );
                }
                else
                {  // Draw large bar for everything else
                    sprite_batch.Draw(
                        ModEntry.instance.health_bars,
                        new Rectangle(xpos + 6, ypos + 6, (int)(inner_bar_length * fill_amount), inner_bar_width),
                        new Rectangle(4, 4 + spr_indy * spr_height, 1, inner_bar_width),
                        fill_color
                    );
                }
                if (!negative)
                {  // Draw bg colored bar inside large bar if not negative.
                    // Draw Second Bar on top of the first that makes it look hollow
                    DrawHelper.DrawLine(sprite_batch, xpos + 6, ypos + 13, new Vector2((inner_bar_length * fill_amount), inner_bar_width - 14), new Color(249, 186, 102));
                }
            }

            // Bar outline (Shaded)
            sprite_batch.Draw(
                ModEntry.instance.health_bars,
                new Vector2(xpos, ypos),
                new Rectangle(0, spr_height * spr_indy, spr_width, spr_height),
                Color.Gray,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0.85f
            );
        }
    }



    // Game1.getFarm().getObjectAtTile
    // Game1.getFarm().makeHoeDirt
    // Game1.getFarm().Map
    // Game1.getFarm().modData
    // Game1.getFarm().hoe

    // Tile vectors are by tile size, so increasing a vector by 1 moves the object exactly 1 tile over.
    // Tile right in front of the normal farm house is X:64, Y:18
    // Lowest Y is probably 0 since you can leave the top of the map
    // Highest Y is 64, bottom path out of map
    // Lowest X is probably 3
    // Highest X is probably 77 (tillable) or 79 
    // Top left tile is vaguely x:5 y:9 and X:4, y:10 (the corner isn't tillable)
    // Bottom Left: X:3, Y:61
    // Top Right, can't hoe there, vaguely X:77,Y:23 as top right most hoeable, and Y:13 ish for top right in general
    // Bottom Right isn't really a corner, but vaguely between x:75 y:54 and x:68, y:61
}
