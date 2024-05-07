using Force.DeepCloner;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewMods.Common.Services.Integrations.CustomBush;
using StardewMods.CustomBush;
using StardewMods.CustomBush.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using xTile.Dimensions;
using xTile.Tiles;

namespace RegenerativeAgriculture
{
    enum EatMode
    {
        All,
        OnlyTake,
        OnlyGive
    }
    internal sealed class SharedData
    {
        //static field with the data type of its own class
        private static SharedData instance;
        private ModEntry mod_entry;
        public const int max_health = 40;
        public static int[] max_initial_health = new int[] {24, 16, 12, 9 };
        public const float death_chance_per_missing_nutrient = .05f;

        public Color[] measure_colors;

        public Dictionary<string, int[]> crop_diet_dict;
        public List<string> unknown_diets;

        public Dictionary<GameLocation, Dictionary<Vector2, int[]>> soil_health_data = new();
        public int soil_health_xpad = 2;
        public int soil_health_ypad = 2;
        Dictionary<Vector2, float> death_preview = new();


        public Dictionary<GameLocation, HashSet<Vector2>> surveyed_tiles;  // Acts as a mask for what tiles to show soil health for!

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static SharedData()
        {
        }
        private SharedData()
        {
            ResetData();
            mod_entry = ModEntry.instance;
            mod_entry.Helper.Events.Player.Warped += Player_Warped;
            mod_entry.Helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private void GenerateSoilHealth(GameLocation location)
        {
            soil_health_data[location] = new();
            double[] rarity = new double[]
            {   // Cannot input 0.
                3,
                4,
                6,
                9
            };
            double scale = 7; // Determines the scale of the waves, bigger number = larger slower changing soil health surface.
            double extremity = .7f;  // Based on testing, .65 - .7 is good for getting a 0-1 range, but making it higher will allow for more extreme values.
            double center_offset = .05;
            Perlin[] noises = new Perlin[]
            {
                new Perlin(),
                new Perlin(),
                new Perlin(),
                new Perlin()
            };
            // Added some padding to be safe for things trying to index out of the map.
            int location_width = location.Map.DisplayWidth / 64;
            int location_height = location.Map.DisplayHeight / 64;
            mod_entry.Monitor.Log($"Generating for {location.Name}: {location_width},{location_height}", LogLevel.Debug);

            for (int x = -soil_health_xpad; x < location_width + soil_health_xpad; x++) // tile's x val
            {
                for (int y = -soil_health_ypad; y < location_height + soil_health_ypad; y++) // tile's y val
                {
                    int[] tile_health = new int[4];
                    for (int health_component = 0; health_component < 4; health_component++)
                    {
                        double raw_noise_output = noises[health_component].Noise(x / scale, y / scale);
                        double kinda_centered_val = .5 + center_offset + extremity * raw_noise_output;
                        double remapped_val = (Math.Exp(rarity[health_component] * kinda_centered_val) - 1) / (Math.Exp(rarity[health_component]) - 1);
                        double clamped_val = Math.Min(Math.Max(remapped_val, 0), 1);
                        int final_val = (int)(clamped_val * max_initial_health[health_component]);
                        tile_health[health_component] = final_val;
                    }
                    SetSoilHealth(location, new Vector2(x, y), tile_health);
                }
            }
        }

        //traditional static method to get the singleton instance
        public static SharedData GetInstance()
        {
            //create a singleton, if not created already
            if (instance == null)
            {
                //instancing happens
                instance = new SharedData();
            }
            if (instance.crop_diet_dict == null)
            {
                instance.LoadCropDietData();
            }
            return instance;
        }
        private void Player_Warped(object? sender, WarpedEventArgs e)
        {
            if (e.NewLocation.GetData().CanPlantHere ?? false)
            {
                if (!soil_health_data.ContainsKey(e.NewLocation))
                {
                    death_preview = new();
                    surveyed_tiles[e.NewLocation] = new();
                    GenerateSoilHealth(e.NewLocation);
                }
            }
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Generate initial soil health map, and save it under the farm object mod data so it's part of the save data and multiplayer code.
            // This is going to need to stay updated with every action we take.
            // Each hoeDirt can reference the farm for it's data instead of keeping it locally.  This way there's one ground truth, the farm dataThough if I ever want to add visuals or more features like that, I'll probably want to rethink this structure.
            if (!soil_health_data.ContainsKey(Game1.getFarm()))
            {
                // Fills farm with soil_health_data, since we KNOW we will at least need that.
                surveyed_tiles[Game1.getFarm()] = new();
                GenerateSoilHealth(Game1.getFarm());
            }
        }

        public void ResetData()
        {
            //empty right now
            measure_colors = new Color[]
            {
                new Color(0, 30, 255, 255),
                new Color(144, 0, 255, 255),
                new Color(255, 0, 166, 255),
                new Color(255, 30, 0, 255),
            };
            surveyed_tiles = new();
            soil_health_data = new();
        }

        

        void LoadCropDietData()
        {
            crop_diet_dict = JsonFileReader.Read<Dictionary<string, int[]>>(Path.Combine(mod_entry.Helper.DirectoryPath, "data", "CropDiet.json"));
            unknown_diets = JsonFileReader.Read<List<string>>(Path.Combine(mod_entry.Helper.DirectoryPath, "data", "UnknownDiet.json"));
        }

        public int[] GetCropDiet(string item_id)
        {
            if (!crop_diet_dict.ContainsKey(item_id))
            {
                return new int[]
                {
                    0,0,0,0
                };
            }
            else
            {
                return crop_diet_dict[item_id].DeepClone();
            }
        }

        public float GetDeathPreview(Vector2 tile)
        {
            return death_preview.GetValueOrDefault(tile, 0f);
        }

        public int[] GetSoilHealth(GameLocation location, Vector2 tile)
        {
            if (!soil_health_data.ContainsKey(location))
            {
                // In case special plants allow to grow outside of a farmable location.
                mod_entry.Monitor.Log("GOT HERE.", LogLevel.Debug);
                return new int[] { 1000, 1000, 1000, 1000 };
            }
            return soil_health_data[location][tile].DeepClone();
        }

        public void SetSoilHealth(GameLocation location, Vector2 tile, int[] soil_health)
        {
            if (!soil_health_data.ContainsKey(location))
            {
                // Ignore anything trying to write data to a non-farmable location.
                return;
            }
            soil_health_data[location][tile] = soil_health;
        }

        void ApplyAreaHealth(Dictionary<Vector2, int[]> store, Vector2 tile, int[][] result)
        {
            int ind = 0;
            int diameter = (int)Math.Sqrt(result.Length);
            int radius = diameter / 2; // Integer division so floored.
            for (int i = -radius; i < radius + 1; i++)
            {
                for (int j = -radius; j < radius + 1; j++)
                {
                    store[tile + new Vector2(i, j)] =  result[ind];
                    ind++;
                }
            }
        }

        

        public void UpdateSoilHealth(GameLocation location)
        {

            if (!soil_health_data.ContainsKey(location))
            {
                mod_entry.Monitor.Log($"Couldn't update soil health for {location}", LogLevel.Debug);
                return;
            }
            Dictionary<Vector2, int[]> data_store = soil_health_data[location];

            // Get Terrain Features
            IEnumerable<TerrainFeature> t_features = location.terrainFeatures.Values;

            // Remove irrelevant features.
            t_features = t_features.Where(x =>
                (x is Tree tree && !tree.hasSeed.Value && !tree.stump.Value) ||
                (x is FruitTree fruitTree && !fruitTree.stump.Value && (fruitTree.growthStage.Value != 4 || fruitTree.IsInSeasonHere())) ||
                x is Bush ||
                (x is HoeDirt hoeDirt && hoeDirt.crop != null));

            // Sort so that Trees are first, then bushes, then crops
            t_features = t_features.OrderBy(x => (x is Tree || x is FruitTree) ? 0 : (x is Bush) ? 1 : 2);

            // Calculate Regenerative effects
            Compute_Tomorrow_inplace(data_store, t_features, EatMode.OnlyGive);

            // Calculate Depletive effects
            Compute_Tomorrow_inplace(data_store, t_features, EatMode.OnlyTake);

            // Kill Doomed Plants
            KillDoomedPlants(data_store, t_features);

            // Resolve and clamp outputs, kill things that need to die.
            ClampHealth(data_store);

        }

        public void UpdateDeathPreview(GameLocation location)
        {
            // CLONE the data so that we don't overwrite anything.
            if (!soil_health_data.ContainsKey(location))
            {
                return;
            }
            Dictionary<Vector2, int[]> data_store = soil_health_data[location].DeepClone();

            // Get Terrain Features
            IEnumerable<TerrainFeature> t_features = location.terrainFeatures.Values;

            // Remove irrelevant features.
            t_features = t_features.Where(x =>
                (x is Tree tree && !tree.hasSeed.Value && !tree.stump.Value) ||
                (x is FruitTree fruitTree && !fruitTree.stump.Value && (fruitTree.growthStage.Value != 4 || fruitTree.IsInSeasonHere())) ||
                x is Bush ||
                (x is HoeDirt hoeDirt && hoeDirt.crop != null));

            // Sort so that Trees are first, then bushes, then crops
            t_features = t_features.OrderBy(x => (x is Tree || x is FruitTree) ? 0 : (x is Bush) ? 1 : 2);

            // Calculate Regenerative effects
            Compute_Tomorrow_inplace(data_store, t_features, EatMode.OnlyGive);

            // Calculate Depletive effects
            Compute_Tomorrow_inplace(data_store, t_features, EatMode.OnlyTake);

            // Generate the death preview!
            GenerateDeathPreview(data_store, t_features);
        }

        public bool IsTreeEating(Tree tree)
        {
            return !tree.stump.Value;
        }

        public bool IsFruitTreeEating(FruitTree fruitTree)
        {
            return !fruitTree.stump.Value && (fruitTree.growthStage.Value != 4 || fruitTree.IsInSeasonHere());
        }

        public bool IsBushEating(Bush bush, ICustomBush? customBush)
        {
            // This checks if the bush is currently growing or in season or sheltered.
            Season season = Game1.season;
            int dayOfMonth = Game1.dayOfMonth;
            List<Season> allowed_season = customBush?.Seasons ?? new List<Season> { Season.Spring, Season.Summer, Season.Fall };
            return bush.getAge() < (customBush?.AgeToProduce ?? 20) || (allowed_season.Contains(season) || bush.IsSheltered());
        }

        public bool IsCropEating(HoeDirt hoeDirt, bool ignore_watering = false)
        {
            return !(hoeDirt.crop == null || (!ignore_watering && hoeDirt.needsWatering() && !hoeDirt.isWatered()) || hoeDirt.readyForHarvest());
        }

        void Compute_Tomorrow_inplace(Dictionary<Vector2, int[]> data_store, IEnumerable<TerrainFeature> t_features, EatMode eatMode)
        {
            // RECORD ALL THE EFFECTS OF TREES AND CROPS
            foreach (TerrainFeature t in t_features)
            {
                
                if (t is Tree tree)
                {
                    if (IsTreeEating(tree)) // This is already true at this point.
                    {
                        int[][] next_soil_health = NextTreeSoilHealth(data_store, tree, eatMode);
                        ApplyAreaHealth(data_store, tree.Tile, next_soil_health);
                    }
                }
                else if (t is FruitTree fruitTree)
                {
                    if (IsFruitTreeEating(fruitTree))
                    {// Ensure is in season or not fully grown.
                        int[][] next_soil_health = NextTreeSoilHealth(data_store, fruitTree, eatMode);

                        ApplyAreaHealth(data_store, fruitTree.Tile, next_soil_health);
                    }
                }
                else if (t is Bush bush)
                {
                    bool proceed_flag = false;
                    // If not fully grown or In Bloom
                    ICustomBush customBush;
                    string customBushId = null;
                    bool is_custom_bush = mod_entry.customBushApi.TryGetCustomBush(bush, out customBush, out customBushId);

                    if (IsBushEating(bush, customBush))
                    {
                        int[][] next_soil_health = NextBushSoilHealth(data_store, bush, eatMode);
                        ApplyAreaHealth(data_store, bush.Tile, next_soil_health);
                    }
                }
                else if (t is HoeDirt hoeDirt)
                {
                    if (IsCropEating(hoeDirt))
                    {
                        int[] next_soil_health = NextCropSoilHealth(data_store, hoeDirt, eatMode);
                        data_store[hoeDirt.Tile] = next_soil_health;
                    }
                }
            }
        }

        public void GenerateDeathPreview(Dictionary<Vector2, int[]> health_data, IEnumerable<TerrainFeature> t_features)
        {
            // Dictionary<Vector2, float> result = new();
            foreach (TerrainFeature t in t_features)
            {
                int missing_nutrients = -health_data[t.Tile].Select(x => Math.Min(x, 0)).Sum();
                death_preview[t.Tile] = missing_nutrients * death_chance_per_missing_nutrient;
            }
            // death_preview = result;
        }
        void ClampHealth(Dictionary<Vector2, int[]> store)
        {

            foreach (KeyValuePair<Vector2, int[]> kv in store)
            {
                int[] next_soil_health = kv.Value.Select(x => Math.Min(Math.Max(x, 0), max_health)).ToArray();
                store[kv.Key] = next_soil_health;
            }
        }

        public void KillDoomedPlants(Dictionary<Vector2, int[]> data_store, IEnumerable<TerrainFeature> t_features) {
            // RESOLVE THE EFFECTS (Kill crops that need to die, bound numbers to the range of allowed values again, etc.)
            foreach (TerrainFeature value in t_features)
            {

                // ORDER OF HOW THESE ARE RESOLVED MIGHT MATTER :/


                int[] next_soil_health = data_store[value.Tile];
                int missing_nutrients = -next_soil_health.Select(x => Math.Min(x, 0)).Sum();

                if (value is HoeDirt hoeDirt)
                {
                    if (missing_nutrients * death_chance_per_missing_nutrient > Game1.random.NextDouble())
                    {
                        // TODO: Add global broadcast about dead crops.
                        hoeDirt.crop.Kill();
                    }

                }
                else if (value is Tree tree)
                {
                    if (missing_nutrients * death_chance_per_missing_nutrient > Game1.random.NextDouble())
                    {
                        // TODO: Add global broadcast about dead crops.
                        tree.Location.terrainFeatures.Remove(tree.Tile);
                    }
                }
                else if (value is Bush bush)
                {
                    if (missing_nutrients * death_chance_per_missing_nutrient > Game1.random.NextDouble())
                    {
                        // TODO: Add global broadcast about dead crops.
                        bush.Location.terrainFeatures.Remove(bush.Tile);
                    }
                }
                else if (value is FruitTree fruitTree)
                {
                    if (missing_nutrients * death_chance_per_missing_nutrient > Game1.random.NextDouble())
                    {
                        // TODO: Add global broadcast about dead crops.
                        fruitTree.Location.terrainFeatures.Remove(fruitTree.Tile);
                    }
                }
            }
        }

        public int[] NextCropSoilHealth(Dictionary<Vector2, int[]> data_store, HoeDirt hoeDirt, EatMode eatMode, bool ignore_watering = false)
        {
            // ASSUME THAT THERE IS CROP AND IT'S IN CONDITION TO GROW.
            int[] soil_health = data_store[hoeDirt.Tile];
            int[] crop_diet = GetCropDiet($"(O){hoeDirt.crop.netSeedIndex.Value}");

            for (int i = 0; i < 4; i++)
            {
                if ((eatMode == EatMode.OnlyTake && crop_diet[i] < 0) || (eatMode == EatMode.OnlyGive && crop_diet[i] > 0))
                {
                    continue;
                }
                soil_health[i] = soil_health[i] - crop_diet[i];
            }
            return soil_health;
        }

        public int[][] NextTreeSoilHealth(Dictionary<Vector2, int[]> data_store, TerrainFeature t, EatMode eatMode)
        {
            int radius = 2;
            int[] tree_diet;
            if (t is Tree tree)
            {
                tree_diet = GetCropDiet(tree.GetData().SeedItemId); // Already Qualified ID

                // Tree eats (or gives)

            }
            else if (t is FruitTree fruit_tree)
            {
                tree_diet = GetCropDiet($"(O){fruit_tree.treeId.Value}"); // Convert to Qualified ID
            }
            else
            {
                return null;
            }
            int[][] result = CropEatWithRadius(data_store, t.Tile, tree_diet, radius, eatMode);
            return result;
        }

        public int[][] NextBushSoilHealth(Dictionary<Vector2, int[]> data_store, Bush bush, EatMode eatMode)
        {
            int radius = 1;
            ICustomBush customBush;
            string customBushId;
            bool is_custom_bush = mod_entry.customBushApi.TryGetCustomBush(bush, out customBush, out customBushId);
            int[] crop_diet = new int[] { 0, 0, 0, 0 };  // Default diet.
            if (is_custom_bush)
            {
                if (customBushId is not null)
                {
                    crop_diet = GetCropDiet(customBushId); // Already Qualified
                }
            }
            else
            {
                if (bush.size.Value == 3)
                {
                    // Tea Sapling
                    crop_diet = GetCropDiet("(O)251");
                }
            }

            int[][] result = CropEatWithRadius(data_store, bush.Tile, crop_diet, radius, eatMode);
            return result;

            // If there isn't a crop, the crop doesn't needs watering and isn't watter, or isn't fullyGrown, return soil_health
        }

        public int[][] CropEatWithRadius(Dictionary<Vector2, int[]> data_store, Vector2 tile, int[] diet, int radius, EatMode eatMode)
        {
            List<int[]> result = new();

            for (int i = -radius; i < radius + 1; i++)
            {
                for (int j = -radius; j < radius + 1; j++)
                {
                    result.Add(data_store[tile + new Vector2(i, j)]);
                }
            }
            int center_tile = result.Count / 2; // This should floor
            mod_entry.Monitor.LogOnce($"{center_tile}, {result.Count}", LogLevel.Debug);

            // Tree takes or gives tree_diet to extremeties wherever possible.
            for (int i = 0; i < 4; i++)
            {
                if (diet[i] == 0 || (eatMode == EatMode.OnlyGive && diet[i] > 0) || (eatMode == EatMode.OnlyTake && diet[i] < 0))
                {
                    continue;
                }
                // Let the tree eat first
                result[center_tile][i] -= diet[i];

                // Now let the tile underneath it prepare for next meal:

                // FAIRLY CONFUSING LOGIC WARNING!!!
                // This number is how much to TAKE from extremity tiles.  Negative values will GIVE to extremity tiles
                int diet_take_capacity = Math.Max(Math.Min(result[center_tile][i] + 2 * diet[i], max_health), 0) - result[center_tile][i];  // Determine how far we can go without hitting the max or min value.
                int take_amount = Math.Sign(diet_take_capacity);  // Amount to take / give each step to extremities each step

                // Setup Set of allowed indices to take from (exlude center tile)
                HashSet<int> not_full_indices = new HashSet<int>();
                for (int m = 0; m < result.Count; m++)
                {
                    not_full_indices.Add(m);
                }
                not_full_indices.Remove(center_tile);
                HashSet<int> todo_indices = not_full_indices.DeepClone();
                while (not_full_indices.Count > 0 && diet_take_capacity != 0)
                {
                    if (todo_indices.Count == 0)
                    {
                        // If we've gone through all the tiles, repeat again.
                        todo_indices = not_full_indices.DeepClone();
                    }
                    int target_ind = todo_indices.ElementAt(Game1.random.Next(todo_indices.Count));
                    int new_val = result[target_ind][i] - take_amount;
                    if ((take_amount > 0 && new_val < 0) || (take_amount < 0 && new_val > max_health))
                    {
                        // If we ran out of nutrient to take or space to give to, remove this ind from the not full indices
                        not_full_indices.Remove(target_ind);
                    }
                    else
                    {
                        // Remove diet from target, take to center, and lower the remaining to take.
                        result[target_ind][i] -= take_amount;
                        result[center_tile][i] += take_amount;
                        diet_take_capacity -= take_amount;
                    }
                    todo_indices.Remove(target_ind);
                }
                // Take the result of eating or giving to this trees tile, then ADD to that tile the amount we TRIED TO TAKE from extremeties, and SUBTRACT what we weren't able to take.
            }
            return result.ToArray();
        }
    }

    public static class JsonFileReader
    {
        public static T Read<T>(string filePath)
        {
            var lineCommentsRegex = @"//(.*?)\r?\n";
            string text = File.ReadAllText(filePath);
            string noComments = Regex.Replace(text, lineCommentsRegex, me =>
            {
                return Environment.NewLine;
            });
            return JsonSerializer.Deserialize<T>(noComments);
        }
    }

    
}
