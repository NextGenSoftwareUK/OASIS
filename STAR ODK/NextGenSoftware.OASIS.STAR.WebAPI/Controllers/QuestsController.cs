using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{    [ApiController]
    [Route("api/[controller]")]
    public partial class QuestsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());
        private readonly ILogger<QuestsController> _logger;

        public QuestsController(ILogger<QuestsController> logger)
        {
            _logger = logger;
        }

        protected override STARAPI GetStarAPI() => _starAPI;

        /// <summary>Fallback: set quest.Status from MetaData when the load path did not go through HolonManager.MapMetaData (e.g. fallback LoadAllForAvatarAsync). Prefer "Status" (key used by HolonManager); support "QuestStatus" for backwards compatibility.</summary>
        private static void NormalizeQuestStatusFromMetaData(Quest q)
        {
            if (q?.MetaData == null) return;
            var key = q.MetaData.ContainsKey("Status") ? "Status" : (q.MetaData.ContainsKey("QuestStatus") ? "QuestStatus" : null);
            if (key == null) return;
            var val = q.MetaData[key];
            if (val == null) return;
            var s = val.ToString();
            if (string.IsNullOrEmpty(s)) return;
            if (System.Enum.TryParse<QuestStatus>(s, true, out var status))
                q.Status = status;
        }

        /// <summary>
        /// Filters out soft-deleted quest rows. <paramref name="avatarId"/> is reserved for optional per-row reload verify.
        /// </summary>
        private Task<List<Quest>> FilterToLoadableActiveQuestsAsync(Guid avatarId, IEnumerable<IQuest> source)
        {
            _ = avatarId;
            var filtered = new List<Quest>();
            if (source == null)
                return Task.FromResult(filtered);

            foreach (var item in source)
            {
                if (item is not Quest quest)
                    continue;
                if (quest.Id == Guid.Empty)
                    continue;
                if (quest.DeletedDate != DateTime.MinValue || quest.IsDeleted)
                    continue;

                filtered.Add(quest);
            }

            return Task.FromResult(filtered);
        }
    }
}