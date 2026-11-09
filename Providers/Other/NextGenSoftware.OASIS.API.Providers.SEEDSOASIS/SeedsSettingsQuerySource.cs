#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
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

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.QueryTests")]

namespace NextGenSoftware.OASIS.API.Providers.SEEDSOASIS;

/// <summary>
/// Public SEEDS configuration observations at three exact Telos native-table keys.
/// A preceding head observation identifies the network; it does not pin or prove table state.
/// </summary>
public sealed class SeedsSettingsQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "seeds/telos/config";
    private readonly SettingsBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    public SeedsSettingsQuerySource(Guid owner, IEnumerable<string> settings)
        : this(owner, settings, null) { }

    public static SeedsSettingsQuerySource ForReviewedTeachingCatalogue(Guid owner) =>
        new(owner, new[] { "unity.high", "unity.medium", "unity.low" });

    internal SeedsSettingsQuerySource(Guid owner, IEnumerable<string> settings, HttpMessageHandler? handler)
        : this(new SettingsBackend(owner, settings, handler)) { }

    private SeedsSettingsQuerySource(SettingsBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Public SEEDS configuration observations";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Application);
        query = new(QueryProviderType, backend.ReadAsync, maximumConcurrentReads: 3,
            admittedRecordIds: backend.RecordIds);
    }

    public PType QueryProviderType => PType.SEEDSOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static Guid LogicalId(Guid owner, string settingKey)
    {
        if (owner == Guid.Empty || settingKey is not ("unity.high" or "unity.medium" or "unity.low"))
            throw new ArgumentException("A source owner and reviewed public setting key are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.SEEDSOASIS, Source,
            settingKey);
    }

    public const string ChainId = "4667b205c6838ef70ff7988f6e8257e8be0e1284a2f59699054a018f743b1d11";
    public const string Account = "settgs.seeds";
    public const string Consistency = "Current table observation; preceding head does not pin table state";

    public static ulong ReviewedPrimaryKey(string key) => key switch
    {
        "unity.high" => 15338590735463301120UL,
        "unity.medium" => 15338590738079003936UL,
        "unity.low" => 15338590737719623680UL,
        _ => throw new ArgumentException("A reviewed SEEDS configuration key is required.")
    };

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(backend.Probe, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose() { IsProviderActivated = false; backend.Dispose(); }

    private sealed class SettingsBackend : IKeyValueBackend, IDisposable
    {
        private static readonly Uri Endpoint = new("https://mainnet.telos.net/");
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> settings;
        private readonly HttpClient client;
        public Guid Probe { get; }
        public IReadOnlySet<Guid> RecordIds => settings.Keys.ToHashSet();

        public SettingsBackend(Guid owner, IEnumerable<string> configured, HttpMessageHandler? handler)
        {
            if (configured is null) throw new ArgumentNullException(nameof(configured));
            var selected = configured.Take(4).ToArray();
            if (selected.Length is < 1 or > 3 || selected.Distinct().Count() != selected.Length)
                throw new ArgumentException("One to three distinct reviewed setting keys are required.");
            settings = selected.ToDictionary(key => LogicalId(owner, key));
            this.owner = owner; Probe = LogicalId(owner, selected[0]);
            if (handler is null)
            {
                var guarded = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
                guarded.AllowAutoRedirect = false; guarded.UseProxy = false; guarded.UseCookies = false;
                handler = guarded;
            }
            client = new HttpClient(handler);
        }

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!settings.TryGetValue(id, out var key))
                throw new InvalidDataException("The ID is outside the configured SEEDS catalogue.");
            using var info = await ReadJsonAsync("v1/chain/get_info", new { }, token);
            var header = info.RootElement;
            if (header.GetProperty("chain_id").GetString() != ChainId)
                throw new InvalidDataException("The SEEDS network identity differs.");
            var head = header.GetProperty("head_block_num").GetUInt32();
            var irreversible = header.GetProperty("last_irreversible_block_num").GetUInt32();
            var hash = header.GetProperty("head_block_id").GetString();
            if (head == 0 || irreversible == 0 || irreversible > head || hash is null ||
                !Regex.IsMatch(hash, "^[0-9a-f]{64}$") || hash[..8] != head.ToString("x8", CultureInfo.InvariantCulture))
                throw new InvalidDataException("The SEEDS network head observation is malformed.");
            var lower = ReviewedPrimaryKey(key);
            using var table = await ReadJsonAsync("v1/chain/get_table_rows", new
            {
                json = true, code = Account, scope = Account, table = "config",
                lower_bound = lower.ToString(CultureInfo.InvariantCulture),
                upper_bound = checked(lower + 1).ToString(CultureInfo.InvariantCulture),
                key_type = "i64", index_position = "1", limit = 1, reverse = false, show_payer = false
            }, token);
            var root = table.RootElement;
            var rows = root.GetProperty("rows");
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != 1 ||
                root.GetProperty("more").ValueKind != JsonValueKind.False ||
                (root.TryGetProperty("next_key", out var next) && next.GetString() != ""))
                throw new InvalidDataException("One exact SEEDS setting with no continuation is required.");
            var row = rows[0];
            if (row.GetProperty("param").GetString() != key || !row.GetProperty("value").TryGetUInt64(out var value))
                throw new InvalidDataException("The SEEDS setting key or uint64 value differs.");
            var description = row.GetProperty("description").GetString();
            var impact = row.GetProperty("impact").GetString();
            if (string.IsNullOrWhiteSpace(description) || description.Length > 2048 || description.Any(char.IsControl) ||
                impact is not ("high" or "med" or "low"))
                throw new InvalidDataException("A bounded setting description and reviewed impact are required.");
            var compact = JsonSerializer.Serialize(row);
            var fields = new Dictionary<string, object>
            {
                ["name"] = "SEEDS " + key, ["description"] = description,
                ["SeedsSettingKey"] = key,
                ["SeedsSettingValue"] = value.ToString(CultureInfo.InvariantCulture),
                ["SeedsSettingDescription"] = description, ["SeedsSettingImpact"] = impact,
                ["SeedsChainId"] = ChainId, ["SeedsContract"] = Account, ["SeedsTable"] = "config",
                ["SeedsObservedHeadBlock"] = head.ToString(CultureInfo.InvariantCulture),
                ["SeedsObservedHeadId"] = hash,
                ["SeedsObservedIrreversibleBlock"] = irreversible.ToString(CultureInfo.InvariantCulture),
                ["SeedsConsistency"] = Consistency, ["SeedsRowJson"] = compact,
                ["SeedsRowJsonSha256"] = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(compact)))
            };
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.SEEDSOASIS,
                new NativeRecord(Source, key, fields), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var result = mapped.Result;
            result.CreatedByAvatarId = owner; result.IsPublic = true; result.IsActive = true;
            result.Children = Array.Empty<IHolon>(); result.ProviderUniqueStorageKey[PType.SEEDSOASIS] = key;
            return StoredRecordJson.Serialize<IHolon>(result);
        }

        private async Task<JsonDocument> ReadJsonAsync(string route, object body, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Endpoint, route))
                { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
            request.Headers.Accept.ParseAdd("application/json"); request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new HttpRequestException("The exact SEEDS read must return HTTP200.", null, response.StatusCode);
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("The native SEEDS response must be JSON.");
            await response.Content.LoadIntoBufferAsync(131072, token);
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            var document = JsonDocument.Parse(new UTF8Encoding(false, true).GetString(bytes),
                new JsonDocumentOptions { MaxDepth = 32 });
            try
            {
                UniqueFields(document.RootElement);
                if (document.RootElement.ValueKind != JsonValueKind.Object || document.RootElement.TryGetProperty("error", out _))
                    throw new InvalidDataException("An error-free native SEEDS object is required.");
                return document;
            }
            catch { document.Dispose(); throw; }
        }

        private static void UniqueFields(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate native SEEDS property.");
                    UniqueFields(property.Value);
                }
            }
            else if (value.ValueKind == JsonValueKind.Array)
                foreach (var item in value.EnumerateArray()) UniqueFields(item);
        }
        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact SEEDS CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("SEEDS setting scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("SEEDS configuration observations are read-only."));
        public void Dispose() => client.Dispose();
    }
}
