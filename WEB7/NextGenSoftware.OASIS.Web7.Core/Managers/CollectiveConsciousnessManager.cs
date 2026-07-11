using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.Core.Managers
{
    /// <summary>
    /// Manages collective consciousness spaces - shared intention fields where multiple consenting symbiosis
    /// sessions co-create. Only the aggregate (mean) intention state across participants is ever exposed; no
    /// individual's raw signal or per-person state is shared with the group, preserving sovereignty within unity.
    /// </summary>
    public class CollectiveConsciousnessManager : OASISManager
    {
        private readonly SymbiosisSessionManager _sessionManager;

        public CollectiveConsciousnessManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
            _sessionManager = new SymbiosisSessionManager(avatarId, OASISDNA);
        }

        public CollectiveConsciousnessManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null)
            : base(OASISStorageProvider, avatarId, OASISDNA)
        {
            _sessionManager = new SymbiosisSessionManager(OASISStorageProvider, avatarId, OASISDNA);
        }

        public async Task<OASISResult<CollectiveConsciousnessSpace>> CreateSpaceAsync(string name)
        {
            OASISResult<CollectiveConsciousnessSpace> result = new OASISResult<CollectiveConsciousnessSpace>();

            CollectiveConsciousnessSpace space = new CollectiveConsciousnessSpace { Name = name };
            Holon holon = new Holon(HolonType.CollectiveConsciousnessSpace) { Name = name, Description = "WEB7 collective consciousness space." };
            WriteSpaceToMetaData(holon, space);

            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(holon, AvatarId, false);

            if (saveResult.IsError || saveResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating collective consciousness space. Reason: {saveResult.Message}");
                return result;
            }

            space.Id = saveResult.Result.Id;
            result.Result = space;
            return result;
        }

        /// <summary>Joins a consenting symbiosis session to a space - the session's owner must already have granted consent for the session itself.</summary>
        public async Task<OASISResult<bool>> JoinSpaceAsync(Guid spaceId, Guid sessionId)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<SymbiosisSession> sessionResult = await _sessionManager.GetSessionAsync(sessionId);

            if (sessionResult.IsError || sessionResult.Result == null || !sessionResult.Result.ConsentGranted)
            {
                OASISErrorHandling.HandleError(ref result, "Only sessions with explicit consent already granted may join a collective consciousness space.");
                return result;
            }

            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(spaceId, false);

            if (loadResult.IsError || loadResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Space {spaceId} not found.");
                return result;
            }

            CollectiveConsciousnessSpace space = ReadSpaceFromMetaData(loadResult.Result);

            if (!space.ParticipantSessionIds.Contains(sessionId))
                space.ParticipantSessionIds.Add(sessionId);

            WriteSpaceToMetaData(loadResult.Result, space);
            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(loadResult.Result, AvatarId, false);
            result.Result = !saveResult.IsError;
            return result;
        }

        /// <summary>Recomputes the aggregate (mean) intention field across every participating session's last known state.</summary>
        public async Task<OASISResult<CollectiveConsciousnessSpace>> GetAggregateFieldAsync(Guid spaceId)
        {
            OASISResult<CollectiveConsciousnessSpace> result = new OASISResult<CollectiveConsciousnessSpace>();
            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(spaceId, false);

            if (loadResult.IsError || loadResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Space {spaceId} not found.");
                return result;
            }

            CollectiveConsciousnessSpace space = ReadSpaceFromMetaData(loadResult.Result);
            List<IntentionState> states = new List<IntentionState>();

            foreach (Guid sessionId in space.ParticipantSessionIds)
            {
                OASISResult<SymbiosisSession> sessionResult = await _sessionManager.GetSessionAsync(sessionId);

                if (!sessionResult.IsError && sessionResult.Result?.LastIntentionState != null)
                    states.Add(sessionResult.Result.LastIntentionState);
            }

            if (states.Count > 0)
            {
                space.AggregateState = new IntentionState
                {
                    Focus = states.Average(s => s.Focus),
                    Arousal = states.Average(s => s.Arousal),
                    EmotionalValence = states.Average(s => s.EmotionalValence),
                    CognitiveLoad = states.Average(s => s.CognitiveLoad)
                };
            }

            result.Result = space;
            return result;
        }

        private static void WriteSpaceToMetaData(IHolon holon, CollectiveConsciousnessSpace space)
        {
            holon.MetaData["Name"] = space.Name;
            holon.MetaData["ParticipantSessionIds"] = JsonSerializer.Serialize(space.ParticipantSessionIds);
            holon.MetaData["CreatedUtc"] = space.CreatedUtc.ToString("o");
        }

        private static CollectiveConsciousnessSpace ReadSpaceFromMetaData(IHolon holon)
        {
            CollectiveConsciousnessSpace space = new CollectiveConsciousnessSpace
            {
                Id = holon.Id,
                Name = holon.MetaData.TryGetValue("Name", out object n) ? n?.ToString() : holon.Name,
                CreatedUtc = holon.MetaData.TryGetValue("CreatedUtc", out object c) && c != null && DateTime.TryParse(c.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt) ? dt : DateTime.UtcNow
            };

            if (holon.MetaData.TryGetValue("ParticipantSessionIds", out object p) && p != null)
                space.ParticipantSessionIds = JsonSerializer.Deserialize<List<Guid>>(p.ToString()) ?? new List<Guid>();

            return space;
        }
    }
}
