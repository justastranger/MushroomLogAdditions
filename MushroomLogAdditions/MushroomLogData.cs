namespace MushroomLogAdditions
{
    internal class MushroomLogData : Dictionary<string, List<OutputWithChance>>
    {

    }

    internal class OutputWithChance : Tuple<string, float>
    {
        public OutputWithChance(string outputQualifiedItemId, float chance) : base(outputQualifiedItemId, chance)
        {
        }
    }
}
