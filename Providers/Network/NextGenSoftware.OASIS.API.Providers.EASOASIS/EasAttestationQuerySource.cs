using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
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

namespace NextGenSoftware.OASIS.API.Providers.EASOASIS;

public sealed class EasAttestationQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Origin = "https://easscan.org/graphql";
    private const string Source = "eas-ethereum-attestation-metadata";
    private readonly AttestationBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    private EasAttestationQuerySource(AttestationBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<ProviderType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed Ethereum attestation metadata";
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
        query = new(QueryProviderType, backend.ReadAsync, 3, backend.Ids.ToHashSet());
    }

    public static EasAttestationQuerySource FromJson(string json)
    {
        var config = JsonSerializer.Deserialize<Catalogue>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        });
        if (config is null || config.Schema != 1 || config.Origin != Origin || config.SourceOwner == Guid.Empty ||
            config.Uids is not { Length: 3 } || config.Uids.Any(uid => !IsUid(uid)) ||
            config.Uids.Distinct(StringComparer.Ordinal).Count() != 3)
            throw new ArgumentException("An exact Ethereum attestation teaching catalogue is required.", nameof(json));
        return new(new AttestationBackend(config));
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.EASOASIS;
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

    private static bool IsUid(string value) => value is { Length: 66 } &&
        value.StartsWith("0x", StringComparison.Ordinal) && value.Skip(2).All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    private sealed record Catalogue(int Schema, string Origin, Guid SourceOwner, string[] Uids);

    private sealed class AttestationBackend(Catalogue catalogue) : IKeyValueBackend, IDisposable
    {
        private const string Selection = "query ReviewedAttestations($ids:[String!]!) { attestations(where:{id:{in:$ids}},take:3) { id schemaId revoked revocationTime expirationTime } }";
        private readonly HttpClient client = new(NativeConnectionGuard.CreateGuardedHttpHandler());
        private readonly Dictionary<Guid, string> records = catalogue.Uids.ToDictionary(uid =>
            NativeRecordHolonMapper.StableHolonId(catalogue.SourceOwner, Core.Enums.ProviderType.EASOASIS, Source, uid));
        public IEnumerable<Guid> Ids => records.Keys;

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!records.TryGetValue(id, out var uid)) return null;
            using var request = new HttpRequestMessage(HttpMethod.Post, Origin)
            {
                Content = JsonContent.Create(new { query = Selection, variables = new { ids = new[] { uid } } })
            };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            var bytes = new byte[65537];
            var length = await stream.ReadAtLeastAsync(bytes, bytes.Length, false, token);
            if (length == bytes.Length) throw new InvalidDataException("EAS metadata response exceeds 64 KiB.");
            using var document = JsonDocument.Parse(bytes.AsMemory(0, length));
            var envelope = document.RootElement;
            if (envelope.TryGetProperty("errors", out _))
                throw new InvalidDataException("EASScan rejected the reviewed metadata query.");
            var rows = envelope.GetProperty("data").GetProperty("attestations");
            if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != 1)
                throw new InvalidDataException("EASScan must return exactly one reviewed attestation.");
            var row = rows[0];
            var schema = row.GetProperty("schemaId").GetString();
            if (row.GetProperty("id").GetString() != uid || !IsUid(schema))
                throw new InvalidDataException("EASScan returned a different attestation or invalid schema identity.");
            const ProviderType provider = Core.Enums.ProviderType.EASOASIS;
            var mapped = NativeRecordHolonMapper.CreateHolon(catalogue.SourceOwner, provider,
                new NativeRecord(Source, uid, new Dictionary<string, object>
                {
                    ["name"] = "Ethereum attestation " + uid,
                    ["EasAttestationUid"] = uid,
                    ["EasSchemaUid"] = schema,
                    ["EasRevoked"] = row.GetProperty("revoked").GetBoolean(),
                    ["EasRevocationTime"] = row.GetProperty("revocationTime").GetInt64(),
                    ["EasExpirationTime"] = row.GetProperty("expirationTime").GetInt64()
                }), NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var holon = mapped.Result;
            holon.CreatedByAvatarId = catalogue.SourceOwner;
            holon.IsPublic = true;
            holon.IsActive = true;
            holon.Children = Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey[provider] = uid;
            return StoredRecordJson.Serialize<IHolon>(holon);
        }

        private static NotSupportedException Unsupported() => new("Use a bounded EAS attestation metadata CQL read.");
        public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Unsupported());
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(Unsupported());
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public void Dispose() => client.Dispose();
    }
}
