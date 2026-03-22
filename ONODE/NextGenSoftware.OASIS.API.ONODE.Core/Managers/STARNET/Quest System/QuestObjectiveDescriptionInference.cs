using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    /// <summary>
    /// When clients create objectives with only Description/ItemRequired (no explicit dictionaries), infer Option-B need dicts
    /// so progress APIs and STAR client formatting (count-first lists) work.
    /// </summary>
    public static class QuestObjectiveDescriptionInference
    {
        /// <summary>Populate requirement dictionaries from human-readable text. Uses objective <paramref name="gameSource"/> as the game key (e.g. ODOOM, OQUAKE).</summary>
        public static void ApplyInferredNeeds(Objective objective, string? description, string? itemRequired, string gameSource)
        {
            if (objective == null) return;
            var game = string.IsNullOrWhiteSpace(gameSource) ? "ODOOM" : gameSource.Trim();
            var primary = description?.Trim() ?? "";
            var item = itemRequired?.Trim() ?? "";
            var combined = string.IsNullOrEmpty(item) ? primary : $"{primary} {item}".Trim();
            if (string.IsNullOrEmpty(combined))
                combined = "Complete";

            var lower = combined.ToLowerInvariant();

            string FirstIntOr(string fallback)
            {
                var m = Regex.Match(combined, @"(\d+)", RegexOptions.CultureInvariant);
                return m.Success ? m.Groups[1].Value : fallback;
            }

            if (Regex.IsMatch(lower, @"\bkill\b") || lower.Contains("monster", StringComparison.Ordinal) ||
                Regex.IsMatch(lower, @"\benemy\b") || lower.Contains("enemies", StringComparison.Ordinal) ||
                lower.Contains("zombie", StringComparison.Ordinal) || lower.Contains("imp", StringComparison.Ordinal))
            {
                objective.NeedToKillMonsters[game] = new List<string> { FirstIntOr("1") };
                return;
            }

            if (lower.Contains("earn", StringComparison.Ordinal) && lower.Contains("xp", StringComparison.Ordinal))
            {
                objective.NeedToEarnXP[game] = new List<string> { FirstIntOr("1") };
                return;
            }

            if (lower.Contains("mega health", StringComparison.Ordinal) || lower.Contains("stimpack", StringComparison.Ordinal) ||
                lower.Contains("medikit", StringComparison.Ordinal) ||
                (lower.Contains("health", StringComparison.Ordinal) && !lower.Contains("armor", StringComparison.Ordinal) &&
                 !lower.Contains("ammo", StringComparison.Ordinal)))
            {
                objective.NeedToCollectHealth[game] = new List<string> { "1" };
                return;
            }

            if (lower.Contains("keycard", StringComparison.Ordinal) || Regex.IsMatch(lower, @"\bkeys?\b"))
            {
                objective.NeedToCollectKeys[game] = new List<string> { "1" };
                return;
            }

            if (lower.Contains("armor", StringComparison.Ordinal))
            {
                objective.NeedToCollectArmor[game] = new List<string> { "1" };
                return;
            }

            if (lower.Contains("ammo", StringComparison.Ordinal) || lower.Contains("shell", StringComparison.Ordinal) ||
                lower.Contains("clip", StringComparison.Ordinal) || lower.Contains("bullet", StringComparison.Ordinal))
            {
                objective.NeedToCollectAmmo[game] = new List<string> { "1" };
                return;
            }

            if (lower.Contains("weapon", StringComparison.Ordinal))
            {
                objective.NeedToCollectWeapons[game] = new List<string> { "1" };
                return;
            }

            if (lower.Contains("powerup", StringComparison.Ordinal) || lower.Contains("artifact", StringComparison.Ordinal))
            {
                objective.NeedToCollectPowerups[game] = new List<string> { "1" };
                return;
            }

            if ((lower.Contains("level", StringComparison.Ordinal) && (lower.Contains("complete", StringComparison.Ordinal) ||
                                                                       lower.Contains("clear", StringComparison.Ordinal))) ||
                lower.Contains("complete level", StringComparison.Ordinal))
            {
                objective.NeedToCompleteLevel[game] = new List<string> { "1" };
                return;
            }

            if (lower.Contains("rune", StringComparison.Ordinal))
            {
                objective.NeedToCollectItems[game] = new List<string> { "1" };
                return;
            }

            objective.NeedToCollectItems[game] = new List<string> { "1" };
        }
    }
}
