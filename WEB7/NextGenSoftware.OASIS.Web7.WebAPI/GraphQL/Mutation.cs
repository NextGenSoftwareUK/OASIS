using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Web7.Core.Enums;
using NextGenSoftware.OASIS.Web7.Core.Managers;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.Web7.WebAPI.GraphQL
{
    /// <summary>Root Mutation type for WEB7 Collective Consciousness GraphQL API.</summary>
    public class Mutation
    {
        /// <summary>
        /// Creates a new collective consciousness space for the given avatar.
        /// </summary>
        public async Task<CollectiveConsciousnessSpace> CreateCollectiveSpaceAsync(string avatarId, string name)
        {
            if (!Guid.TryParse(avatarId, out Guid parsedAvatarId))
                throw new ArgumentException("avatarId must be a valid GUID.", nameof(avatarId));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("name must not be empty.", nameof(name));

            var manager = new CollectiveConsciousnessManager(parsedAvatarId);
            var result = await manager.CreateSpaceAsync(name);

            if (result.IsError)
                throw new Exception(result.Message ?? "Failed to create collective space.");

            return result.Result;
        }

        /// <summary>
        /// Joins an existing collective consciousness space with the given session.
        /// </summary>
        public async Task<bool> JoinCollectiveSpaceAsync(string avatarId, string spaceId, string sessionId)
        {
            if (!Guid.TryParse(avatarId, out Guid parsedAvatarId) ||
                !Guid.TryParse(spaceId, out Guid parsedSpaceId) ||
                !Guid.TryParse(sessionId, out Guid parsedSessionId))
                throw new ArgumentException("avatarId, spaceId, and sessionId must all be valid GUIDs.");

            var manager = new CollectiveConsciousnessManager(parsedAvatarId);
            var result = await manager.JoinSpaceAsync(parsedSpaceId, parsedSessionId);

            if (result.IsError)
                throw new Exception(result.Message ?? "Failed to join collective space.");

            return result.Result;
        }

        /// <summary>
        /// Starts a new consent-gated symbiosis session for the given avatar.
        /// retention defaults to Ephemeral per the Borg-Free Pledge sovereignty principle.
        /// </summary>
        public async Task<SymbiosisSession> StartSymbiosisSessionAsync(
            string avatarId,
            bool consentGranted,
            string retention = "Ephemeral")
        {
            if (!Guid.TryParse(avatarId, out Guid parsedAvatarId))
                throw new ArgumentException("avatarId must be a valid GUID.", nameof(avatarId));

            if (!consentGranted)
                throw new ArgumentException("Consent must be granted before a symbiosis session can be started (Borg-Free Pledge).");

            RetentionMode retentionMode = RetentionMode.Ephemeral;
            if (!string.IsNullOrWhiteSpace(retention))
                Enum.TryParse<RetentionMode>(retention, true, out retentionMode);

            var manager = new SymbiosisSessionManager(parsedAvatarId);
            var result = await manager.StartSessionAsync(parsedAvatarId, consentGranted, retentionMode);

            if (result.IsError)
                throw new Exception(result.Message ?? "Failed to start symbiosis session.");

            return result.Result;
        }

        /// <summary>
        /// Submits a batch of raw bio-signal samples for the given session and returns the freshly computed intention state.
        /// </summary>
        public async Task<IntentionState> SubmitSymbiosisSignalsAsync(string avatarId, string sessionId, List<BioSignalSample> samples)
        {
            if (!Guid.TryParse(avatarId, out Guid parsedAvatarId) ||
                !Guid.TryParse(sessionId, out Guid parsedSessionId))
                throw new ArgumentException("avatarId and sessionId must be valid GUIDs.");

            var manager = new SymbiosisSessionManager(parsedAvatarId);
            var result = await manager.SubmitSignalsAsync(parsedSessionId, samples);

            if (result.IsError)
                throw new Exception(result.Message ?? "Failed to submit signals.");

            return result.Result;
        }

        /// <summary>
        /// Ends an active symbiosis session.
        /// </summary>
        public async Task<bool> EndSymbiosisSessionAsync(string avatarId, string sessionId)
        {
            if (!Guid.TryParse(avatarId, out Guid parsedAvatarId) ||
                !Guid.TryParse(sessionId, out Guid parsedSessionId))
                throw new ArgumentException("avatarId and sessionId must be valid GUIDs.");

            var manager = new SymbiosisSessionManager(parsedAvatarId);
            var result = await manager.EndSessionAsync(parsedSessionId);

            if (result.IsError)
                throw new Exception(result.Message ?? "Failed to end symbiosis session.");

            return result.Result;
        }
    }
}
