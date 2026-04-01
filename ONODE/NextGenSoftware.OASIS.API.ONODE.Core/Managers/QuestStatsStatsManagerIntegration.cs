using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using OasisBootStatic = NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers;

/// <summary>
/// Connects <see cref="StatsManager.GetQuestStatsAsync"/> to <see cref="QuestManager.GetQuestStatsAsync"/> so avatar/dashboard stats read quest holons, not settings holons.
/// Call <see cref="RegisterWithStatsManager"/> once after <c>OASISBootLoader.BootOASIS</c>.
/// </summary>
public static class QuestStatsStatsManagerIntegration
{
    private static readonly object Gate = new();
    private static bool _registered;

    public static void RegisterWithStatsManager()
    {
        lock (Gate)
        {
            if (_registered)
                return;
            StatsManager.ResolveQuestStatsAsync = LoadQuestStatsViaQuestManagerAsync;
            _registered = true;
        }
    }

    private static async Task<OASISResult<Dictionary<string, object>>> LoadQuestStatsViaQuestManagerAsync(Guid avatarId)
    {
        var oasis = OasisBootStatic.OASISDNA;
        if (oasis == null)
        {
            var err = new OASISResult<Dictionary<string, object>>();
            OASISErrorHandling.HandleError(ref err, "OASISDNA is not loaded; boot OASIS before resolving quest statistics.");
            return err;
        }

        var provider = ProviderManager.Instance.CurrentStorageProvider;
        if (provider == null)
        {
            var err = new OASISResult<Dictionary<string, object>>();
            OASISErrorHandling.HandleError(ref err, "No active storage provider; cannot load quest holons for statistics.");
            return err;
        }

        var qm = new QuestManager(provider, avatarId, new STARDNA(), oasis);
        return await qm.GetQuestStatsAsync(avatarId).ConfigureAwait(false);
    }
}
