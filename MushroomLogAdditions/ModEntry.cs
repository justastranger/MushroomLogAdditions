using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.TerrainFeatures;
using StardewValley.GameData.Machines;

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
            instance = this;
            harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();
        }

        public void CollectOutputs()
        {
            IContentPack internalContentPack = Helper.ContentPacks.CreateTemporary(
                directoryPath: Path.Combine(Helper.DirectoryPath, "content-pack"),
                id: Guid.NewGuid().ToString("N"),
                name: "Mushroom Log Additions Internal Pack",
                description: "Adds mushroom trees->mushroom seeds to the Mushroom Log results.",
                author: "justastranger",
                version: new SemanticVersion(1, 0, 0)
            );
            Dictionary<string, string>? data = internalContentPack.ReadJsonFile<Dictionary<string, string>>("MushroomLogData.json");
            if (data != null && data.Count > 0)
            {
                data.ToList().ForEach(x => { treeToOutputDict[x.Key] = x.Value; });
            }

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
        }
    }

    [HarmonyPatch(typeof(StardewValley.Object))]
    public static class ObjectPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(StardewValley.Object.OutputMushroomLog))]
        public static Item OutputMushroomLogPostfix(StardewValley.Object machine, Item inputItem, bool probe, MachineItemOutput outputData, out int? overrideMinutesUntilReady, Item __result, List<Tree> ___nearbyTrees, List<string> ___mushroomPossibilities)
        {
            // output value that is normally null
            overrideMinutesUntilReady = null;
            // The list of trees is already compiled, we just need to loop through it again to check for new trees
            foreach (Tree tree in ___nearbyTrees)
            {
                // vanilla check for old enough trees
                if (tree.growthStage.Value >= 5)
                {
                    // check to see if the scanned tree is registered as having an output
                    if (ModEntry.treeToOutputDict.TryGetValue(tree.treeType.Value, out string mushroomType))
                    {
                        // add them to the existing pool of mushrooms
                        ___mushroomPossibilities.Add(mushroomType);
                    }
                }
            }
            // re-roll the output using the new pool with the old stack amount and quality values
            return ItemRegistry.Create(Game1.random.ChooseFrom(___mushroomPossibilities), __result.stack.Value, __result.quality.Value, false);
        }
    }
}
