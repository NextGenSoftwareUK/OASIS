using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public static class LevelManager
    {
        private static Dictionary<int, long> _levelLookup = null;

        /// <summary>
        /// Get the karma thresholds needed for each level. Key: Level, Value: Min Karma Needed.
        /// </summary>
        public static Dictionary<int, long> LevelLookup
        {
            get
            {
                if (_levelLookup == null)
                    GenerateLevelLookup();

                return _levelLookup;
            }
        }

        /// <summary>
        /// Increase to make it easier to level up, decrease to make it harder (lowest is can be is 1).
        /// </summary>
        public static int LevelThresholdWeighting { get; set; } = 4;

        /// <summary>
        /// The current max level supported, this can be increased if needed, max karma supported is 9 Quintillion but current max level is 99 (1255125945858 karma, around 1.2 trillion).
        /// </summary>
        public static int MaxLevel { get; set; } = 99;

        /// <summary>
        /// Get the level for the karma passed in.
        /// </summary>
        /// <param name="karma"></param>
        /// <returns></returns>
        public static int GetLevelFromKarma(long karma)
        {
            if (karma < LevelLookup[2])
                return 1;

            foreach (int level in LevelLookup.Keys)
            {
                if (level < LevelLookup.Keys.Count + 1)
                {
                    if (karma >= LevelLookup[level] && karma < LevelLookup[level + 1])
                        return level;
                }
                else
                    return level; //max level.
            }

            return 1;
        }

        public static void GenerateLevelLookup(bool showKarmaThreshholds = false)
        {
            _levelLookup = new Dictionary<int, long>();

            if (LevelThresholdWeighting < 1)
                LevelThresholdWeighting = 1;

            if (showKarmaThreshholds)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n Karma Level Thresholds:\n");
            }

            long currentKarma = 1;
            for (int i = 2; i < (MaxLevel + 1); i++)
            {
                currentKarma = Math.Abs(currentKarma + 100 + currentKarma / LevelThresholdWeighting);
                _levelLookup[i] = currentKarma;

                if (showKarmaThreshholds)
                    Console.WriteLine($" Level {i} = {currentKarma} karma.");
            }

            if (showKarmaThreshholds)
                Console.WriteLine("");
        }
    }
}