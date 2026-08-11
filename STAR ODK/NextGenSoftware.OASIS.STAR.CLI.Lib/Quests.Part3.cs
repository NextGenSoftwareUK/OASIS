using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class Quests : STARNETUIBase<Quest, DownloadedQuest, InstalledQuest, STARNETDNA>
    {

        private static object BuildQuestListJsonObject(IQuest q, bool showDetailed)
        {
            var objectives = new List<object>();
            if (q.Objectives != null)
            {
                foreach (var o in q.Objectives.OrderBy(x => x.Order))
                {
                    if (o == null) continue;
                    objectives.Add(new
                    {
                        id = o.Id,
                        order = o.Order,
                        title = o.Title ?? string.Empty,
                        description = o.Description ?? string.Empty,
                        linkedGeoHotSpotId = o is Objective obj && obj.LinkedGeoHotSpotId.HasValue ? obj.LinkedGeoHotSpotId.Value.ToString() : (string?)null,
                        externalHandoffUri = o is Objective obj2 && !string.IsNullOrWhiteSpace(obj2.ExternalHandoffUri) ? obj2.ExternalHandoffUri : (string?)null,
                        status = FormatObjectiveUiStatus(q, o),
                        progressPercent = GetObjectiveProgressPercent(o),
                        progressSummary = o.ProgressSummary ?? string.Empty
                    });
                }
            }

            List<string>? prereq = null;
            if (showDetailed && q is Quest qq && qq.PrerequisiteQuestIds != null && qq.PrerequisiteQuestIds.Count > 0)
                prereq = qq.PrerequisiteQuestIds.ToList();

            var row = new
            {
                id = q.Id,
                name = q.Name ?? string.Empty,
                description = q.Description ?? string.Empty,
                questType = GetRuntimeQuestTypeLabel(q),
                status = q.Status.ToString(),
                progressPercent = GetQuestProgressPercent(q),
                gameSource = q.GameSource ?? string.Empty,
                parentQuestId = q.ParentQuestId == Guid.Empty ? (string?)null : q.ParentQuestId.ToString(),
                parentMissionId = q.ParentMissionId == Guid.Empty ? (string?)null : q.ParentMissionId.ToString(),
                linkedGeoHotSpotId = q is Quest ql && ql.LinkedGeoHotSpotId.HasValue ? ql.LinkedGeoHotSpotId.Value.ToString() : (string?)null,
                externalHandoffUri = q is Quest qh && !string.IsNullOrWhiteSpace(qh.ExternalHandoffUri) ? qh.ExternalHandoffUri : (string?)null,
                rewardKarma = showDetailed ? q.RewardKarma : (long?)null,
                rewardXP = showDetailed ? q.RewardXP : (long?)null,
                prerequisiteQuestIds = prereq,
                objectives,
                starnetDNA = showDetailed && q.STARNETDNA != null ? new
                {
                    id = q.STARNETDNA.Id,
                    name = q.STARNETDNA.Name,
                    description = q.STARNETDNA.Description,
                    starnetHolonType = q.STARNETDNA.STARNETHolonType,
                    starnetCategory = FormatStarnetDnaJsonValue(q.STARNETDNA.STARNETCategory),
                    starnetSubCategory = FormatStarnetDnaJsonValue(q.STARNETDNA.STARNETSubCategory),
                    version = q.STARNETDNA.Version,
                    versionSequence = q.STARNETDNA.VersionSequence,
                    createdOn = q.STARNETDNA.CreatedOn,
                    createdByAvatarUsername = q.STARNETDNA.CreatedByAvatarUsername,
                    publishedOn = q.STARNETDNA.PublishedOn,
                    publishedProviderType = q.STARNETDNA.PublishedProviderType,
                    launchTarget = q.STARNETDNA.LaunchTarget,
                    dependencies = q.STARNETDNA.Dependencies,
                    metaTagMappings = q.STARNETDNA.MetaTagMappings
                } : null
            };
            return row;
        }

        private static void WriteRuntimeQuestToConsole(IQuest q, bool includeExtraPaddingInFooter = true)
        {
            string typeLabel = GetRuntimeQuestTypeLabel(q);
            string parentNote = q.ParentQuestId != Guid.Empty
                ? $"  (sub-quest of {q.ParentQuestId})"
                : string.Empty;

            CLIEngine.ShowMessage(
                $"  {q.Name ?? "(unnamed)"}  [{typeLabel}]  {q.Status}  {GetQuestProgressPercent(q)}%{parentNote}",
                ConsoleColor.Green,
                false);

            if (!string.IsNullOrWhiteSpace(q.Description))
                CLIEngine.ShowMessage($"      {q.Description}", ConsoleColor.Green, false);

            if (!string.IsNullOrWhiteSpace(q.GameSource))
                CLIEngine.ShowMessage($"      GameSource: {q.GameSource}", ConsoleColor.Green, false);

            if (q.Objectives == null || q.Objectives.Count == 0)
            {
                CLIEngine.ShowMessage("      Objectives: (none)", ConsoleColor.DarkGray, false);
                Console.WriteLine("");
                return;
            }

            CLIEngine.ShowMessage("      Objectives:", ConsoleColor.Green, false);
            foreach (IObjective o in q.Objectives.OrderBy(x => x.Order))
            {
                if (o == null) continue;
                string st = FormatObjectiveUiStatus(q, o);
                int pct = GetObjectiveProgressPercent(o);
                string line = o.ProgressSummary ?? string.Empty;
                CLIEngine.ShowMessage(
                    $"        [{o.Order}] {o.Title}  |  {st}  |  {pct}%",
                    ConsoleColor.Green,
                    false);
                if (!string.IsNullOrWhiteSpace(line))
                    CLIEngine.ShowMessage($"            {line}", ConsoleColor.DarkGray, false);

                // Spacing between objective "items" (matches the inventory detailed UX).
                Console.WriteLine("");
            }

            if (includeExtraPaddingInFooter)
                Console.WriteLine("");
        }

        private static string GetRuntimeQuestTypeLabel(IQuest q)
        {
            if (q.QuestType != default(QuestType))
                return q.QuestType.ToString();
            return q.Type.ToString();
        }

        private static int GetQuestProgressPercent(IQuest q)
        {
            if (q is Quest qq)
                return qq.ProgressPercent;
            return 0;
        }

        private static int GetObjectiveProgressPercent(IObjective o)
        {
            if (o is Objective oj)
                return oj.ProgressPercent;
            return o.IsCompleted ? 100 : 0;
        }

        private static string FormatObjectiveUiStatus(IQuest quest, IObjective obj)
        {
            if (obj.IsCompleted) return "Completed";
            if (quest.Status == QuestStatus.NotStarted)
                return "NotStarted";
            return "InProgress";
        }

        private static string FormatStarnetDnaJsonValue(object? raw)
        {
            if (raw == null)
                return "None";

            if (raw is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.String:
                        return je.GetString() ?? "None";
                    case JsonValueKind.Number:
                        if (je.TryGetInt32(out var i32))
                            return i32.ToString(CultureInfo.InvariantCulture);
                        if (je.TryGetInt64(out var i64))
                            return i64.ToString(CultureInfo.InvariantCulture);
                        return je.GetRawText();
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return "None";
                    default:
                        return je.GetRawText();
                }
            }

            try
            {
                string s = raw.ToString();
                if (string.IsNullOrWhiteSpace(s))
                    return "None";

                if (s.Contains("\"ValueKind\"", StringComparison.Ordinal))
                {
                    using var doc = JsonDocument.Parse(s);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("ValueKind", out JsonElement vk))
                    {
                        if (vk.ValueKind == JsonValueKind.Number)
                            return vk.GetRawText();
                        if (vk.ValueKind == JsonValueKind.String)
                            return vk.GetString() ?? "None";
                    }
                }

                return s;
            }
            catch
            {
                return raw.ToString() ?? "None";
            }
        }
    }
}
