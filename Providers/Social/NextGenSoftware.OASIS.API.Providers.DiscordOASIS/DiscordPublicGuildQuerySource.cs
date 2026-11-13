using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.DiscordOASIS;

/// <summary>Exact public guild observations verified against Discord's public invite API.</summary>
public sealed class DiscordPublicGuildQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "discord-public-guild";
    private static readonly Dictionary<string, string> Invites = new(StringComparer.Ordinal)
    {
        ["267624335836053506"] = "python",
        ["1235157165589794909"] = "godotengine",
        ["212250894228652034"] = "4JBkykG"
    };

    private readonly GuildBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public static DiscordPublicGuildQuerySource ForReviewedTeachingCatalogue(Guid owner) =>
        new(owner, Invites.Keys, "267624335836053506");

    public DiscordPublicGuildQuerySource(Guid owner, IEnumerable<string> guildIds, string probe)
        : this(owner, guildIds, probe, handler: null) { }

    internal DiscordPublicGuildQuerySource(Guid owner, IEnumerable<string> guildIds, string probe, HttpMessageHandler? handler)
        : this(new GuildBackend(owner, guildIds, probe, handler), LogicalId(owner, probe)) { }

    private DiscordPublicGuildQuerySource(GuildBackend backend, Guid probeId) : base(backend, "discord-public-guild:")
    {
        this.backend = backend;
        this.probeId = probeId;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed public Discord guild metadata";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Network);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static Guid LogicalId(Guid owner, string guildId)
    {
        if (owner == Guid.Empty || guildId is null || !Invites.ContainsKey(guildId))
            throw new ArgumentException("A teaching source owner and reviewed Discord guild are required.");

        return NativeRecordHolonMapper.StableHolonId(owner, PType.DiscordOASIS, Source, guildId);
    }

    public PType QueryProviderType => PType.DiscordOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(probeId, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose()
    {
        IsProviderActivated = false;
        backend.Dispose();
    }

    private sealed class GuildBackend : IKeyValueBackend, IDisposable
    {
        private static readonly SemaphoreSlim RequestSlot = new(1);
        private static DateTimeOffset blockedUntil;
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> guilds;
        private readonly HttpClient client;

        public GuildBackend(Guid owner, IEnumerable<string> guildIds, string probe, HttpMessageHandler? handler)
        {
            var catalogue = guildIds?.Take(Invites.Count + 1).ToArray() ?? Array.Empty<string>();
            if (owner == Guid.Empty || catalogue.Length == 0 || catalogue.Length > Invites.Count ||
                catalogue.Any(guild => guild is null || !Invites.ContainsKey(guild)) ||
                catalogue.Distinct(StringComparer.Ordinal).Count() != catalogue.Length ||
                !catalogue.Contains(probe, StringComparer.Ordinal))
                throw new ArgumentException("Distinct reviewed Discord guild IDs and a catalogue probe are required.");

            this.owner = owner;
            guilds = catalogue.ToDictionary(guild => LogicalId(owner, guild));
            if (handler is null)
            {
                var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                transport.UseCookies = false;
                transport.UseProxy = false;
                handler = transport;
            }
            client = new HttpClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!guilds.TryGetValue(id, out var guildId))
                throw new InvalidDataException("The selected ID is outside the reviewed Discord guild catalogue.");

            var invite = Invites[guildId];
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://discord.com/api/v10/invites/{invite}?with_counts=false");
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("DiscordBot (https://oasisomniverse.one, 1.0)");

            using var response = await SendAsync(request, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("Unexpected Discord invite content type.");

            await response.Content.LoadIntoBufferAsync(16384, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 12 });
            var result = document.RootElement;
            RequireUniqueObject(result);
            var guild = result.GetProperty("guild");
            RequireUniqueObject(guild);
            var name = guild.GetProperty("name").GetString();
            if (result.GetProperty("code").GetString() != invite || result.GetProperty("type").GetInt32() != 0 ||
                guild.GetProperty("id").GetString() != guildId || name is not { Length: >= 1 and <= 100 } ||
                string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl))
                throw new InvalidDataException("Discord must return the exact reviewed guild and a valid public name.");

            var record = new NativeRecord(Source, guildId, new Dictionary<string, object>
            {
                ["name"] = name,
                ["DiscordGuildId"] = guildId
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.DiscordOASIS, record, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var holon = mapped.Result;
            holon.CreatedByAvatarId = owner;
            holon.IsPublic = true;
            holon.IsActive = true;
            holon.Children = Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey[PType.DiscordOASIS] = guildId;
            return StoredRecordJson.Serialize<IHolon>(holon);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            await RequestSlot.WaitAsync(token);
            try
            {
                if (DateTimeOffset.UtcNow < blockedUntil)
                    throw new HttpRequestException("The public Discord invite rate-limit window is active.", null,
                        HttpStatusCode.TooManyRequests);

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                if (response.StatusCode == HttpStatusCode.TooManyRequests && response.Headers.RetryAfter is { } retry)
                    blockedUntil = retry.Date ?? DateTimeOffset.UtcNow + (retry.Delta ?? TimeSpan.Zero);
                else if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) &&
                    remaining.FirstOrDefault() == "0" &&
                    response.Headers.TryGetValues("X-RateLimit-Reset-After", out var reset) &&
                    double.TryParse(reset.FirstOrDefault(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds) &&
                    seconds is > 0 and <= 86400)
                    blockedUntil = DateTimeOffset.UtcNow.AddSeconds(seconds);
                return response;
            }
            finally { RequestSlot.Release(); }
        }

        private static void RequireUniqueObject(JsonElement value)
        {
            var fields = new HashSet<string>(StringComparer.Ordinal);
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Any(field => !fields.Add(field.Name)))
                throw new InvalidDataException("The Discord invite and guild must contain unique JSON fields.");
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Discord guild CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Discord guild scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Discord exposes exact public guild reads."));
        public void Dispose() => client.Dispose();
    }
}
