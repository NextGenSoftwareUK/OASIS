using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.Web7.Core.Enums;
using NextGenSoftware.OASIS.Web7.Core.Managers;
using NextGenSoftware.OASIS.Web7.Core.Models;

namespace NextGenSoftware.OASIS.MCP.Server.Tools
{
    /// <summary>
    /// WEB7 - the non-invasive human/AI symbiosis layer. In-process MCP tools wrapping every public method of
    /// SymbiosisSessionManager and CollectiveConsciousnessManager.
    /// </summary>
    [McpServerToolType]
    public static class Web7Tools
    {
        private static Guid ParseAvatarId(string? avatarId) => Guid.TryParse(avatarId, out Guid id) ? id : Guid.Empty;

        [McpServerTool(Name = "web7_start_session"), Description("WEB7: starts a new symbiosis session. consentGranted must be explicitly true - the connection is always voluntary.")]
        public static async Task<string> StartSession(string avatarId, bool consentGranted, string retention = "Ephemeral")
        {
            SymbiosisSessionManager manager = new SymbiosisSessionManager(ParseAvatarId(avatarId));
            var result = await manager.StartSessionAsync(ParseAvatarId(avatarId), consentGranted, Enum.Parse<RetentionMode>(retention, true));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web7_submit_signals"), Description("WEB7: submits a batch of raw bio-signal samples (EEG/HRV/GSR/EyeTracking/VocalHarmonics) for an active, consenting session and returns the freshly computed intention state (focus, arousal, emotional valence, cognitive load) via real FFT/HRV/GSR DSP. Rejects any channel name implying an invasive/implanted source (Borg-Free pledge).")]
        public static async Task<string> SubmitSignals(string sessionId, List<BioSignalSample> samples, string? avatarId = null)
        {
            SymbiosisSessionManager manager = new SymbiosisSessionManager(ParseAvatarId(avatarId));
            var result = await manager.SubmitSignalsAsync(Guid.Parse(sessionId), samples);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web7_end_session"), Description("WEB7: ends a session instantly - with Ephemeral retention (the default), all signal-derived data is wiped immediately, leaving no trace.")]
        public static async Task<string> EndSession(string sessionId, string? avatarId = null)
        {
            SymbiosisSessionManager manager = new SymbiosisSessionManager(ParseAvatarId(avatarId));
            var result = await manager.EndSessionAsync(Guid.Parse(sessionId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web7_get_session"), Description("WEB7: gets a symbiosis session's current state, including its last computed intention state.")]
        public static async Task<string> GetSession(string sessionId, string? avatarId = null)
        {
            SymbiosisSessionManager manager = new SymbiosisSessionManager(ParseAvatarId(avatarId));
            var result = await manager.GetSessionAsync(Guid.Parse(sessionId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web7_create_space"), Description("WEB7: creates a collective consciousness space - a shared intention field where multiple consenting sessions co-create.")]
        public static async Task<string> CreateSpace(string name, string? avatarId = null)
        {
            CollectiveConsciousnessManager manager = new CollectiveConsciousnessManager(ParseAvatarId(avatarId));
            var result = await manager.CreateSpaceAsync(name);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web7_join_space"), Description("WEB7: joins a consenting symbiosis session to a collective consciousness space.")]
        public static async Task<string> JoinSpace(string spaceId, string sessionId, string? avatarId = null)
        {
            CollectiveConsciousnessManager manager = new CollectiveConsciousnessManager(ParseAvatarId(avatarId));
            var result = await manager.JoinSpaceAsync(Guid.Parse(spaceId), Guid.Parse(sessionId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web7_get_aggregate_field"), Description("WEB7: recomputes and returns the aggregate (mean) intention field across every participating session in a collective consciousness space - never any individual's raw signal.")]
        public static async Task<string> GetAggregateField(string spaceId, string? avatarId = null)
        {
            CollectiveConsciousnessManager manager = new CollectiveConsciousnessManager(ParseAvatarId(avatarId));
            var result = await manager.GetAggregateFieldAsync(Guid.Parse(spaceId));
            return JsonSerializer.Serialize(result);
        }
    }
}
