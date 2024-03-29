using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using StardewValley.GameData.Machines;
using Newtonsoft;
using System.Reflection.PortableExecutable;
using Microsoft.Xna.Framework;

namespace MushroomLogAdditions
{
    public class ModEntry : Mod
    {
        internal Config config;
        internal ITranslationHelper i18n => Helper.Translation;

        internal static Dictionary<string, string> treeToOutputDict = new();

        internal static ModEntry instance;
        internal static Harmony harmony;

        public override void Entry(IModHelper helper)
        {
            string startingMessage = i18n.Get("MushroomLogAdditions.start");
            Monitor.Log(startingMessage, LogLevel.Trace);

            config = helper.ReadConfig<Config>();
            helper.Events.GameLoop.SaveLoaded += CollectOutputs;
            instance = this;
            harmony = new Harmony(ModManifest.UniqueID);
            // harmony.PatchAll();
            // harmony.Patch(AccessTools.Method(typeof(StardewValley.Object), nameof(StardewValley.Object.OutputMushroomLog)), new(typeof(ObjectPatch), nameof(ObjectPatch.OutputMushroomLogPrefix)), null, null, null);
            
        }

        private void CollectOutputs(object? sender, SaveLoadedEventArgs e)
        {
            // This framework comes with one addition
            // It was the entire point of writing this and doubles as an example of the format
            IContentPack internalContentPack = Helper.ContentPacks.CreateTemporary(
                directoryPath: Path.Combine(Helper.DirectoryPath, "internal"),
                id: "JAS.MushroomLogAdditions.Internal",
                name: "Mushroom Log Additions Internal Pack",
                description: "Adds mushroom trees->mushroom seeds to the Mushroom Log results.",
                author: instance.ModManifest.Author,
                version: instance.ModManifest.Version
            );
            // initialize the local variable and load the internal datapack in one line
            Dictionary<string, string>? data = internalContentPack.ReadJsonFile<Dictionary<string, string>>("MushroomLogData.json");
            if (data != null && data.Count > 0)
            {
                // this should never fail the check
                data.ToList().ForEach(x => { treeToOutputDict[x.Key] = x.Value; });
                Monitor.Log("Loaded internal content pack.");
            }
            else Monitor.Log("Internal content pack failed to load.", LogLevel.Error); // *cough*

            foreach (IContentPack contentPack in Helper.ContentPacks.GetOwned())
            {
                Monitor.Log($"Reading content pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} from {contentPack.DirectoryPath}", LogLevel.Trace);
                if (contentPack.HasFile("MushroomLogData.json"))
                {
                    data = contentPack.ReadJsonFile<Dictionary<string, string>>("MushroomLogData.json");
                    if (data != null && data.Count > 0)
                    {
                        // merge the two dictionaries, overwriting values
                        // TODO log duplicates
                        Monitor.Log($"Content pack loaded: {contentPack.Manifest.Name} {contentPack.Manifest.Version} from {contentPack.DirectoryPath}", LogLevel.Trace);
                        data.ToList().ForEach(x => {treeToOutputDict[x.Key] = x.Value;});
                    }
                }
            }

            instance.Monitor.Log("Content packs loaded, current additions: ");
            instance.Monitor.Log(Newtonsoft.Json.JsonConvert.SerializeObject(treeToOutputDict));
        }

        public static Item OutputMushroomLog(StardewValley.Object machine, Item inputItem, bool probe, MachineItemOutput outputData, out int? overrideMinutesUntilReady)
        {
            overrideMinutesUntilReady = null;
            instance.Monitor.Log("Mushroom Log Postfix");

            // we have to clone the vanilla code since we can't access any of the original method's local variables
            // otherwise this would've been a simple postfix...
            List<Tree> nearbyTrees = new();
            for (int x = (int)machine.TileLocation.X - 3; x < (int)machine.TileLocation.X + 4; x++)
            {
                for (int y = (int)machine.TileLocation.Y - 3; y < (int)machine.TileLocation.Y + 4; y++)
                {
                    Vector2 v = new((float)x, (float)y);
                    if (machine.Location.terrainFeatures.ContainsKey(v) && machine.Location.terrainFeatures[v] is Tree tree)
                    {
                        nearbyTrees.Add(tree);
                    }
                }
            }
            int treeCount = nearbyTrees.Count;
            List<string> mushroomPossibilities = new();
            int mossyCount = 0;
            foreach (Tree tree in nearbyTrees)
            {
                if (tree.growthStage.Value >= 5)
                {
                    string mushroomType = (Game1.random.NextBool(0.05) ? "(O)422" : (Game1.random.NextBool(0.15) ? "(O)420" : "(O)404"));
                    string treeType = tree.treeType.Value;
                    instance.Monitor.Log($"Testing treeType {treeType}.");
                    if (!(treeType == "2"))
                    {
                        if (!(treeType == "1"))
                        {
                            if (!(treeType == "3"))
                            {
                                if (treeType == "13")
                                {
                                    mushroomType = "(O)422";
                                }
                            }
                            else
                            {
                                mushroomType = "(O)281";
                            }
                        }
                        else
                        {
                            mushroomType = "(O)257";
                        }
                    }
                    else
                    {
                        mushroomType = (Game1.random.NextBool(0.1) ? "(O)422" : "(O)420");

                        // this small bit here is the only addition to the original function
                        // check to see if the scanned tree is registered as having an output
                        if (treeToOutputDict.ContainsKey(treeType))
                        {
                            mushroomType = treeToOutputDict[treeType];
                            instance.Monitor.Log($"TreeType {treeType} recognized, injecting {mushroomType} into mushroomPossibilities.");
                        }
                    }
                    mushroomPossibilities.Add(mushroomType);
                    if (tree.hasMoss.Value)
                    {
                        mossyCount++;
                    }
                }
            }

            for (int i = 0; i < Math.Max(1, (int)(nearbyTrees.Count * 0.75f)); i++)
            {
                mushroomPossibilities.Add(Game1.random.NextBool(0.05) ? "(O)422" : (Game1.random.NextBool(0.15) ? "(O)420" : "(O)404"));
            }
            int amount = Math.Max(1, Math.Min(5, Game1.random.Next(1, 3) * (nearbyTrees.Count / 2)));
            int quality = 0;
            float qualityBoostChance = mossyCount * 0.025f + treeCount * 0.025f;
            while (Game1.random.NextDouble() < (double)qualityBoostChance)
            {
                quality++;
                if (quality == 3)
                {
                    quality = 4;
                    break;
                }
            }
            // confirmed we have an appropriate vanilla mushroomPossibilities, though our added entries are missing
            // instance.Monitor.Log(Newtonsoft.Json.JsonConvert.SerializeObject(mushroomPossibilities));
            // re-roll the output using the new pool with the old stack amount and quality values
            return ItemRegistry.Create(Game1.random.ChooseFrom(mushroomPossibilities), amount, quality, false);
        }
    }

    //[HarmonyPatch(typeof(StardewValley.Object), nameof(StardewValley.Object.OutputMushroomLog))]
    //public static class ObjectPatch
    //{
    //    [HarmonyPrefix]
    //    public static bool OutputMushroomLogPrefix(StardewValley.Object machine, Item inputItem, bool probe, MachineItemOutput outputData, out int? overrideMinutesUntilReady, Item __result)
    //    {
    //        overrideMinutesUntilReady = null;
    //        ModEntry.instance.Monitor.Log("Mushroom Log Postfix");

    //        // we have to clone the vanilla code since we can't access any of the original method's local variables
    //        List<Tree> nearbyTrees = new();
    //        for (int x = (int)machine.TileLocation.X - 3; x < (int)machine.TileLocation.X + 4; x++)
    //        {
    //            for (int y = (int)machine.TileLocation.Y - 3; y < (int)machine.TileLocation.Y + 4; y++)
    //            {
    //                Vector2 v = new((float)x, (float)y);
    //                if (machine.Location.terrainFeatures.ContainsKey(v) && machine.Location.terrainFeatures[v] is Tree tree)
    //                {
    //                    nearbyTrees.Add(tree);
    //                }
    //            }
    //        }
    //        int treeCount = nearbyTrees.Count;
    //        List<string> mushroomPossibilities = new();
    //        int mossyCount = 0;
    //        foreach (Tree tree in nearbyTrees)
    //        {
    //            if (tree.growthStage.Value >= 5)
    //            {
    //                string mushroomType = (Game1.random.NextBool(0.05) ? "(O)422" : (Game1.random.NextBool(0.15) ? "(O)420" : "(O)404"));
    //                string treeType = tree.treeType.Value;
    //                if (!(treeType == "2"))
    //                {
    //                    if (!(treeType == "1"))
    //                    {
    //                        if (!(treeType == "3"))
    //                        {
    //                            if (treeType == "13")
    //                            {
    //                                mushroomType = "(O)422";
    //                            }
    //                        }
    //                        else
    //                        {
    //                            mushroomType = "(O)281";
    //                        }
    //                    }
    //                    else
    //                    {
    //                        mushroomType = "(O)257";
    //                    }
    //                }
    //                else
    //                {
    //                    mushroomType = (Game1.random.NextBool(0.1) ? "(O)422" : "(O)420");

    //                    // this small bit here is the only addition to the original function
    //                    // check to see if the scanned tree is registered as having an output
    //                    if (ModEntry.treeToOutputDict.ContainsKey(treeType))
    //                    {
    //                        mushroomType = ModEntry.treeToOutputDict[treeType];
    //                    }
    //                }
    //                mushroomPossibilities.Add(mushroomType);
    //                if (tree.hasMoss.Value)
    //                {
    //                    mossyCount++;
    //                }
    //            }
    //        }

    //        for (int i = 0; i < Math.Max(1, (int)(nearbyTrees.Count * 0.75f)); i++)
    //        {
    //            mushroomPossibilities.Add(Game1.random.NextBool(0.05) ? "(O)422" : (Game1.random.NextBool(0.15) ? "(O)420" : "(O)404"));
    //        }
    //        int amount = Math.Max(1, Math.Min(5, Game1.random.Next(1, 3) * (nearbyTrees.Count / 2)));
    //        int quality = 0;
    //        float qualityBoostChance = mossyCount * 0.025f + treeCount * 0.025f;
    //        while (Game1.random.NextDouble() < (double)qualityBoostChance)
    //        {
    //            quality++;
    //            if (quality == 3)
    //            {
    //                quality = 4;
    //                break;
    //            }
    //        }

    //        ModEntry.instance.Monitor.Log(Newtonsoft.Json.JsonConvert.SerializeObject(mushroomPossibilities));
    //        // re-roll the output using the new pool with the old stack amount and quality values
    //        __result = ItemRegistry.Create(Game1.random.ChooseFrom(mushroomPossibilities), amount, quality, false);
    //        return false;
    //    }
    //}
}
