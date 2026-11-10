using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.FlowOASIS;

public sealed class FlowSealedHeaderQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Origin = "https://rest-mainnet.onflow.org/";
    private const string Source = "flow-mainnet-sealed-headers";
    private readonly HeaderBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    private FlowSealedHeaderQuerySource(HeaderBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<ProviderType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed sealed Flow mainnet headers";
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
        query = new(QueryProviderType, backend.ReadAsync, 3, backend.Ids.ToHashSet());
    }

    public static FlowSealedHeaderQuerySource FromJson(string json)
    {
        var config = JsonSerializer.Deserialize<Catalogue>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        });
        if (config is null || config.Schema != 1 || config.Origin != Origin || config.SourceOwner == Guid.Empty ||
            config.Blocks is not { Length: 3 } ||
            config.Blocks.Any(block => block is null || !IsHash(block.Id) || !ulong.TryParse(block.Height, out var height) ||
                height.ToString(System.Globalization.CultureInfo.InvariantCulture) != block.Height) ||
            config.Blocks.Select(block => block.Id).Distinct(StringComparer.Ordinal).Count() != 3 ||
            config.Blocks.Select(block => block.Height).Distinct(StringComparer.Ordinal).Count() != 3)
            throw new ArgumentException("An exact Flow mainnet teaching catalogue is required.", nameof(json));
        return new(new HeaderBackend(config));
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.FlowOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(backend.Ids.First(), cancellationToken);
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

    private static bool IsHash(string value) => value is { Length: 64 } &&
        value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    private sealed record Catalogue(int Schema, string Origin, Guid SourceOwner, Block[] Blocks);
    private sealed record Block(string Height, string Id);

    private sealed class HeaderBackend(Catalogue catalogue) : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client = new(NativeConnectionGuard.CreateGuardedHttpHandler());
        private readonly Dictionary<Guid, Block> blocks = catalogue.Blocks.ToDictionary(block =>
            NativeRecordHolonMapper.StableHolonId(catalogue.SourceOwner, Core.Enums.ProviderType.FlowOASIS, Source, block.Id));
        public IEnumerable<Guid> Ids => blocks.Keys;

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!blocks.TryGetValue(id, out var block)) return null;
            using var response = await client.GetAsync(Origin + "v1/blocks?height=" + block.Height,
                HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            var bytes = new byte[65537];
            var length = await stream.ReadAtLeastAsync(bytes, bytes.Length, false, token);
            if (length == bytes.Length) throw new InvalidDataException("Flow header response exceeds 64 KiB.");
            using var document = JsonDocument.Parse(bytes.AsMemory(0, length));
            var rows = document.RootElement;
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != 1)
                throw new InvalidDataException("Flow must return exactly one sealed block.");
            var row = rows[0];
            var header = row.GetProperty("header");
            var parent = header.GetProperty("parent_id").GetString();
            var timestamp = header.GetProperty("timestamp").GetDateTimeOffset();
            if (row.GetProperty("block_status").GetString() != "BLOCK_SEALED" ||
                header.GetProperty("id").GetString() != block.Id || header.GetProperty("height").GetString() != block.Height ||
                !IsHash(parent))
                throw new InvalidDataException("Flow header differs from the reviewed sealed block.");
            const ProviderType provider = Core.Enums.ProviderType.FlowOASIS;
            var mapped = NativeRecordHolonMapper.CreateHolon(catalogue.SourceOwner, provider,
                new NativeRecord(Source, block.Id, new Dictionary<string, object>
                {
                    ["name"] = "Flow sealed block " + block.Height,
                    ["FlowBlockId"] = block.Id, ["FlowBlockHeight"] = block.Height,
                    ["FlowParentBlockId"] = parent, ["FlowBlockTimestamp"] = timestamp.ToString("O")
                }), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var holon = mapped.Result;
            holon.CreatedByAvatarId = catalogue.SourceOwner;
            holon.IsPublic = true;
            holon.IsActive = true;
            holon.Children = Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey[provider] = block.Id;
            return StoredRecordJson.Serialize<IHolon>(holon);
        }

        private static NotSupportedException Unsupported() => new("Use a bounded Flow sealed-header CQL read.");
        public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Unsupported());
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(Unsupported());
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public void Dispose() => client.Dispose();
    }
}
