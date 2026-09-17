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
    /// Manages the lifecycle of WEB7 symbiosis sessions. Enforces the Borg-Free pledge as code, not just policy:
    /// no signal is ever processed without explicit consent, every session can be ended instantly by the human
    /// with zero residue (the default retention mode is Ephemeral), and channel names are screened against
    /// invasive/implant terminology as a defence-in-depth check on top of the BioSignalType enum already only
    /// containing non-invasive, externally-read modalities.
    /// </summary>
    public class SymbiosisSessionManager : OASISManager
    {
        private static readonly string[] InvasiveChannelTerms = { "implant", "subdermal", "cranial", "neural-lace", "electrode-array-internal" };

        private readonly IntentionEngine _intentionEngine = new IntentionEngine();

        public SymbiosisSessionManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }

        public SymbiosisSessionManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null)
            : base(OASISStorageProvider, avatarId, OASISDNA) { }

        /// <summary>Starts a new symbiosis session. ConsentGranted must be explicitly true - the default is always private/disconnected.</summary>
        public async Task<OASISResult<SymbiosisSession>> StartSessionAsync(Guid avatarId, bool consentGranted, Enums.RetentionMode retention = Enums.RetentionMode.Ephemeral)
        {
            OASISResult<SymbiosisSession> result = new OASISResult<SymbiosisSession>();

            if (!consentGranted)
            {
                OASISErrorHandling.HandleError(ref result, "Consent is required to start a WEB7 symbiosis session. The connection is always voluntary - nothing connects without explicit human consent.");
                return result;
            }

            SymbiosisSession session = new SymbiosisSession { Id = Guid.NewGuid(), AvatarId = avatarId, ConsentGranted = true, Retention = retention };
            session.AuditLog.Add($"{DateTime.UtcNow:o} - session started with explicit consent.");

            Holon holon = new Holon(HolonType.SymbiosisSession) { Name = $"Symbiosis-{session.Id}", Description = "WEB7 symbiosis session." };
            WriteSessionToMetaData(holon, session);

            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(holon, AvatarId, false);

            if (saveResult.IsError || saveResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error starting symbiosis session. Reason: {saveResult.Message}");
                return result;
            }

            session.Id = saveResult.Result.Id;
            result.Result = session;
            return result;
        }

        /// <summary>Submits a batch of raw bio-signal samples for an active, consenting session and returns the freshly computed intention state.</summary>
        public async Task<OASISResult<IntentionState>> SubmitSignalsAsync(Guid sessionId, List<BioSignalSample> samples)
        {
            OASISResult<IntentionState> result = new OASISResult<IntentionState>();

            string complianceError = ValidateBorgFreeCompliance(samples);
            if (complianceError != null)
            {
                OASISErrorHandling.HandleError(ref result, complianceError);
                return result;
            }

            OASISResult<SymbiosisSession> sessionResult = await GetSessionAsync(sessionId);

            if (sessionResult.IsError || sessionResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Session {sessionId} not found.");
                return result;
            }

            SymbiosisSession session = sessionResult.Result;

            if (!session.ConsentGranted || !session.IsActive)
            {
                OASISErrorHandling.HandleError(ref result, "Session is not active or consent has been withdrawn - no signal can be processed.");
                return result;
            }

            IntentionState state = _intentionEngine.ComputeIntentionState(samples);
            session.LastIntentionState = state;
            session.AuditLog.Add($"{DateTime.UtcNow:o} - processed {samples.Count} signal sample(s).");

            await PersistSessionAsync(sessionId, session);

            result.Result = state;
            return result;
        }

        /// <summary>Ends a session instantly - if retention is Ephemeral, signal-derived data is wiped immediately, leaving no trace.</summary>
        public async Task<OASISResult<bool>> EndSessionAsync(Guid sessionId)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<SymbiosisSession> sessionResult = await GetSessionAsync(sessionId);

            if (sessionResult.IsError || sessionResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Session {sessionId} not found.");
                return result;
            }

            SymbiosisSession session = sessionResult.Result;
            session.IsActive = false;
            session.EndedUtc = DateTime.UtcNow;
            session.AuditLog.Add($"{DateTime.UtcNow:o} - session ended by human. Reversible by design - no trace left behind.");

            if (session.Retention == Enums.RetentionMode.Ephemeral)
            {
                session.LastIntentionState = null;
                session.AuditLog.Add($"{DateTime.UtcNow:o} - ephemeral retention: all signal-derived data wiped on session end.");
            }

            result.Result = await PersistSessionAsync(sessionId, session);
            return result;
        }

        public async Task<OASISResult<SymbiosisSession>> GetSessionAsync(Guid sessionId)
        {
            OASISResult<SymbiosisSession> result = new OASISResult<SymbiosisSession>();
            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(sessionId, false);

            if (loadResult.IsError || loadResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Session {sessionId} not found.");
                return result;
            }

            result.Result = ReadSessionFromMetaData(loadResult.Result);
            return result;
        }

        /// <summary>
        /// Defence-in-depth Borg-Free check: BioSignalType already only contains non-invasive modalities at the type
        /// level, so this also screens free-text channel names against invasive/implant terminology in case a future
        /// signal type or device profile is added without due care.
        /// </summary>
        private static string ValidateBorgFreeCompliance(List<BioSignalSample> samples)
        {
            if (samples == null || samples.Count == 0)
                return "At least one bio-signal sample is required.";

            foreach (BioSignalSample sample in samples)
            {
                if (string.IsNullOrEmpty(sample.Channel))
                    continue;

                string channelLower = sample.Channel.ToLowerInvariant();

                if (InvasiveChannelTerms.Any(term => channelLower.Contains(term)))
                    return $"Rejected: channel '{sample.Channel}' implies an invasive/implanted signal source. WEB7 is Borg-Free by architecture - only externally-read, non-invasive signals are accepted.";
            }

            return null;
        }

        private async Task<bool> PersistSessionAsync(Guid sessionId, SymbiosisSession session)
        {
            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(sessionId, false);

            if (loadResult.IsError || loadResult.Result == null)
                return false;

            WriteSessionToMetaData(loadResult.Result, session);
            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(loadResult.Result, AvatarId, false);
            return !saveResult.IsError;
        }

        private static void WriteSessionToMetaData(IHolon holon, SymbiosisSession session)
        {
            holon.MetaData["AvatarId"] = session.AvatarId.ToString();
            holon.MetaData["ConsentGranted"] = session.ConsentGranted;
            holon.MetaData["IsActive"] = session.IsActive;
            holon.MetaData["Retention"] = session.Retention.ToString();
            holon.MetaData["StartedUtc"] = session.StartedUtc.ToString("o");
            holon.MetaData["EndedUtc"] = session.EndedUtc?.ToString("o");
            holon.MetaData["LastIntentionState"] = session.LastIntentionState != null ? JsonSerializer.Serialize(session.LastIntentionState) : null;
            holon.MetaData["AuditLog"] = JsonSerializer.Serialize(session.AuditLog);
        }

        private static SymbiosisSession ReadSessionFromMetaData(IHolon holon)
        {
            SymbiosisSession session = new SymbiosisSession
            {
                Id = holon.Id,
                AvatarId = holon.MetaData.TryGetValue("AvatarId", out object a) && Guid.TryParse(a?.ToString(), out Guid aid) ? aid : Guid.Empty,
                ConsentGranted = holon.MetaData.TryGetValue("ConsentGranted", out object c) && c != null && Convert.ToBoolean(c),
                IsActive = holon.MetaData.TryGetValue("IsActive", out object i) && i != null && Convert.ToBoolean(i),
                StartedUtc = holon.MetaData.TryGetValue("StartedUtc", out object s) && s != null && DateTime.TryParse(s.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime st) ? st : DateTime.UtcNow
            };

            if (holon.MetaData.TryGetValue("Retention", out object r) && Enum.TryParse(r?.ToString(), true, out Enums.RetentionMode retention))
                session.Retention = retention;

            if (holon.MetaData.TryGetValue("EndedUtc", out object e) && e != null && DateTime.TryParse(e.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime ed))
                session.EndedUtc = ed;

            if (holon.MetaData.TryGetValue("LastIntentionState", out object lis) && lis != null)
                session.LastIntentionState = JsonSerializer.Deserialize<IntentionState>(lis.ToString());

            if (holon.MetaData.TryGetValue("AuditLog", out object log) && log != null)
                session.AuditLog = JsonSerializer.Deserialize<List<string>>(log.ToString()) ?? new List<string>();

            return session;
        }
    }
}
