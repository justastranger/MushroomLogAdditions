using StardewModdingAPI;

namespace MushroomLogAdditions
{
    internal class Config
    {
        public LogLevel loggingLevel { get; set; }
        public bool loadInternal { get; set; }

        public Config()
        {
            loggingLevel = LogLevel.Trace;
            loadInternal = true;
        }
    }
}
