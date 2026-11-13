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

namespace NextGenSoftware.OASIS.API.Providers.SignProtocolOASIS;

public sealed class SignAttestationQuerySource : KeyValueStorageProviderBase,
    ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Origin = "https://mainnet-rpc.sign.global/api/";
    private const string Source = "sign-base-attestation-metadata";
    private readonly AttestationBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    private SignAttestationQuerySource(AttestationBackend backend) : base(backend, Source + ":")
    {
        this.backend = backend;
        ProviderType = new EnumValue<ProviderType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed Base attestation metadata";
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
        query = new(QueryProviderType, backend.ReadAsync, 3, backend.Ids.ToHashSet());
    }

    public static SignAttestationQuerySource FromJson(string json)
    {
        var config = JsonSerializer.Deserialize<Catalogue>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
        });
        if (config is null || config.Schema != 1 || config.Origin != Origin || config.SourceOwner == Guid.Empty ||
            config.Uids is not { Length: 3 } || config.Uids.Any(uid => !IsUid(uid)) ||
            config.Uids.Distinct(StringComparer.Ordinal).Count() != 3)
            throw new ArgumentException("An exact Base attestation teaching catalogue is required.", nameof(json));
        return new(new AttestationBackend(config));
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.SignProtocolOASIS;
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

    private static bool IsUid(string value) => value is "onchain_evm_8453_0x1" or "onchain_evm_8453_0x2" or "onchain_evm_8453_0x3";

    private sealed record Catalogue(int Schema, string Origin, Guid SourceOwner, string[] Uids);

    private sealed class AttestationBackend(Catalogue catalogue) : IKeyValueBackend, IDisposable
    {
        private readonly HttpClient client = new(NativeConnectionGuard.CreateGuardedHttpHandler());
        private readonly Dictionary<Guid, string> records = catalogue.Uids.ToDictionary(uid =>
            NativeRecordHolonMapper.StableHolonId(catalogue.SourceOwner, Core.Enums.ProviderType.SignProtocolOASIS, Source, uid));
        public IEnumerable<Guid> Ids => records.Keys;

        public async Task<string> ReadAsync(Guid id, CancellationToken token)
        {
            if (!records.TryGetValue(id, out var uid)) return null;
            using var response = await client.GetAsync(Origin + "index/attestations/" + uid,
                HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            var bytes = new byte[65537];
            var length = await stream.ReadAtLeastAsync(bytes, bytes.Length, false, token);
            if (length == bytes.Length) throw new InvalidDataException("Sign metadata response exceeds 64 KiB.");
            using var document = JsonDocument.Parse(bytes.AsMemory(0, length));
            var envelope = document.RootElement;
            if (!envelope.GetProperty("success").GetBoolean() || envelope.GetProperty("statusCode").GetInt32() != 200)
                throw new InvalidDataException("SignScan rejected the reviewed metadata query.");
            var row = envelope.GetProperty("data");
            var schema = row.GetProperty("fullSchemaId").GetString();
            if (row.GetProperty("id").GetString() != uid || row.GetProperty("chainId").GetString() != "8453" ||
                row.GetProperty("mode").GetString() != "onchain" || row.GetProperty("chainType").GetString() != "evm" ||
                schema is not { Length: > 0 and <= 96 } || !schema.StartsWith("onchain_evm_8453_0x", StringComparison.Ordinal))
                throw new InvalidDataException("SignScan returned a different attestation or invalid schema identity.");
            const ProviderType provider = Core.Enums.ProviderType.SignProtocolOASIS;
            var mapped = NativeRecordHolonMapper.CreateHolon(catalogue.SourceOwner, provider,
                new NativeRecord(Source, uid, new Dictionary<string, object>
                {
                    ["name"] = "Base attestation " + uid,
                    ["SignAttestationUid"] = uid,
                    ["SignSchemaUid"] = schema,
                    ["SignRevoked"] = row.GetProperty("revoked").GetBoolean(),
                    ["SignAttestTimestamp"] = row.GetProperty("attestTimestamp").GetString(),
                    ["SignExpirationTime"] = row.GetProperty("validUntil").GetString()
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

        private static NotSupportedException Unsupported() => new("Use a bounded Sign attestation metadata CQL read.");
        public Task VerifyAsync(CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) => Task.FromException<string>(Unsupported());
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(Unsupported());
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.FromException(Unsupported());
        public void Dispose() => client.Dispose();
    }
}
