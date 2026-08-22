using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Web7.Core.Managers;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.WebAPI.GraphQL
{
    /// <summary>Root Query type for WEB7 Collective Consciousness GraphQL API.</summary>
    public class Query
    {
        /// <summary>
        /// Returns the aggregate intention state for a collective consciousness space.
        /// avatarId is used to authorise the lookup; spaceId identifies the space.
        /// </summary>
        public async Task<IntentionState> GetIntentionStateAsync(string avatarId, string spaceId)
        {
            if (!Guid.TryParse(avatarId, out Guid parsedAvatarId))
                throw new ArgumentException("avatarId must be a valid GUID.", nameof(avatarId));

            if (!Guid.TryParse(spaceId, out Guid parsedSpaceId))
                throw new ArgumentException("spaceId must be a valid GUID.", nameof(spaceId));

            var manager = new CollectiveConsciousnessManager(parsedAvatarId);
            var result = await manager.GetAggregateFieldAsync(parsedSpaceId);

            if (result.IsError)
                throw new Exception(result.Message ?? "Failed to retrieve intention state.");

            return result.Result?.AggregateState;
        }

        /// <summary>
        /// Returns the collective consciousness space for the given avatarId and spaceId,
        /// including participant session IDs and the aggregate intention state.
        /// </summary>
        public async Task<CollectiveConsciousnessSpace> GetCollectiveSpaceAsync(string avatarId, string spaceId)
        {
            if (!Guid.TryParse(avatarId, out Guid parsedAvatarId))
                throw new ArgumentException("avatarId must be a valid GUID.", nameof(avatarId));

            if (!Guid.TryParse(spaceId, out Guid parsedSpaceId))
                throw new ArgumentException("spaceId must be a valid GUID.", nameof(spaceId));

            var manager = new CollectiveConsciousnessManager(parsedAvatarId);
            var result = await manager.GetAggregateFieldAsync(parsedSpaceId);

            if (result.IsError)
                throw new Exception(result.Message ?? "Failed to retrieve collective space.");

            return result.Result;
        }

        /// <summary>
        /// Returns the symbiosis session for the given avatarId and sessionId.
        /// </summary>
        public async Task<SymbiosisSession> GetSymbiosisSessionAsync(string avatarId, string sessionId)
        {
            if (!Guid.TryParse(avatarId, out Guid parsedAvatarId))
                throw new ArgumentException("avatarId must be a valid GUID.", nameof(avatarId));

            if (!Guid.TryParse(sessionId, out Guid parsedSessionId))
                throw new ArgumentException("sessionId must be a valid GUID.", nameof(sessionId));

            var manager = new SymbiosisSessionManager(parsedAvatarId);
            var result = await manager.GetSessionAsync(parsedSessionId);

            if (result.IsError)
                throw new Exception(result.Message ?? "Failed to retrieve symbiosis session.");

            return result.Result;
        }
    }
}
