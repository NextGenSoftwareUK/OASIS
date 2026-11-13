#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace NextGenSoftware.OASIS.API.Providers.PushProtocolOASIS;

/// <summary>Exact reviewed public Push Protocol channel metadata.</summary>
public sealed class PushProtocolPublicChannelQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "push-protocol-public-channel/eip155:1";
    private const string Probe = "0xb88460bb2696cab9d66013a05dff29a28330689d";
    private static readonly HashSet<string> Reviewed = new(StringComparer.Ordinal)
    {
        Probe,
        "0x90a48d5cf7343b08da12e067680b4c6dbfe551be",
        "0xdbc5936e4daae94f415c39d284f6a69c4d553f2f"
    };

    private readonly ChannelBackend backend;
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public PushProtocolPublicChannelQuerySource(Guid owner) : this(owner, handler: null) { }

    internal PushProtocolPublicChannelQuerySource(Guid owner, HttpMessageHandler? handler)
        : this(new ChannelBackend(owner, handler), owner) { }

    private PushProtocolPublicChannelQuerySource(ChannelBackend backend, Guid owner) : base(backend, "push-public-channel:")
    {
        this.backend = backend;
        probeId = LogicalId(owner, Probe);
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Reviewed public Push Protocol channels";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Network);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    public static Guid LogicalId(Guid owner, string address)
    {
        if (owner == Guid.Empty || address is null || !Reviewed.Contains(address))
            throw new ArgumentException("A teaching owner and reviewed Push Protocol channel are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.PushProtocolOASIS, Source, address);
    }

    public PType QueryProviderType => PType.PushProtocolOASIS;
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

    private sealed class ChannelBackend : IKeyValueBackend, IDisposable
    {
        private const string Route = "https://backend.epns.io/apis/v1/channels/eip155:1:";
        private readonly Guid owner;
        private readonly Dictionary<Guid, string> channels;
        private readonly HttpClient client;

        public ChannelBackend(Guid owner, HttpMessageHandler? handler)
        {
            this.owner = owner;
            channels = Reviewed.ToDictionary(address => LogicalId(owner, address));
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
            if (!channels.TryGetValue(id, out var address))
                throw new InvalidDataException("The selected ID is outside the reviewed Push Protocol channel catalogue.");

            using var request = new HttpRequestMessage(HttpMethod.Get, Route + address);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidDataException("Push Protocol must return a JSON channel representation.");

            await response.Content.LoadIntoBufferAsync(128 * 1024, token);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
                new JsonDocumentOptions { MaxDepth = 16 });
            var channel = document.RootElement;
            var fields = new HashSet<string>(StringComparer.Ordinal);
            if (channel.ValueKind != JsonValueKind.Object || channel.EnumerateObject().Any(field => !fields.Add(field.Name)))
                throw new InvalidDataException("Push Protocol must return a unique-field channel object.");

            var name = channel.GetProperty("name").GetString();
            if (!string.Equals(channel.GetProperty("channel").GetString(), address, StringComparison.OrdinalIgnoreCase) ||
                name is not { Length: >= 1 and <= 160 } || string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl) ||
                !channel.GetProperty("verified_status").TryGetInt32(out var verification) || verification is < 0 or > 2)
                throw new InvalidDataException("Push Protocol must return the exact channel identity and valid public metadata.");

            var record = new NativeRecord(Source, address, new Dictionary<string, object>
            {
                ["name"] = name,
                ["PushChannelAddress"] = address,
                ["PushChannelChainId"] = "eip155:1",
                ["PushChannelVerification"] = verification
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.PushProtocolOASIS, record, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var row = mapped.Result;
            row.CreatedByAvatarId = owner;
            row.IsPublic = true;
            row.IsActive = true;
            row.Children = Array.Empty<IHolon>();
            row.ProviderUniqueStorageKey[PType.PushProtocolOASIS] = address;
            return StoredRecordJson.Serialize<IHolon>(row);
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Push Protocol channel CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Push Protocol channel scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Push Protocol exposes exact public channel reads."));
        public void Dispose() => client.Dispose();
    }
}
