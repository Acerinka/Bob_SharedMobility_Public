using System;

namespace Bob.SharedMobility
{
    public static class BobTargetIds
    {
        public const string Map = "Map";
        public const string MapFull = "Mapfull";

        public static bool IsMap(string targetId)
        {
            return Equals(targetId, Map);
        }

        public static bool IsMapFull(string targetId)
        {
            return Equals(targetId, MapFull);
        }

        public static bool Equals(string targetId, string expectedTargetId)
        {
            return string.Equals(targetId, expectedTargetId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
