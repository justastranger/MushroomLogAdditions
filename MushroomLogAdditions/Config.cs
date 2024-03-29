using StardewModdingAPI;

namespace MushroomLogAdditions
{
    internal class Config
    {
        public LogLevel loggingLevel { get; set; }

        public Config()
        {
            loggingLevel = LogLevel.Trace;
        }
    }
}
