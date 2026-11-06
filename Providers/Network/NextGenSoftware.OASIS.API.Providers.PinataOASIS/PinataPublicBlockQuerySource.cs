#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ipfs;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.PinataOASIS;

/// <summary>Bounded public IPFS block metadata verified at the Pinata gateway.</summary>
public sealed class PinataPublicBlockQuerySource : KeyValueStorageProviderBase, ICosmicQueryStorageProvider,
    ICosmicReadOnlyQueryActivation, IDisposable
{
    private const string Source = "pinata-public-ipfs-block";
    private const string Root = "QmP4eZj61tDxCmtb6aqSwQsrxRQAN2gwxTEXkTed9TujHh";
    private static readonly HashSet<string> Reviewed = new(StringComparer.Ordinal)
    {
        Root,
        "QmeBt6f2Dem8QnZuZ8PDRT1iuWpphmza3gP17en2ZoReYR",
        "Qmb733sded7TRm8U8bM4FHif89jUVKd8yQiurKCKgCLyQk"
    };

    private readonly BlockBackend backend;
    private readonly BoundedPublicKeyValueQuery query;

    public PinataPublicBlockQuerySource(Guid owner) : this(owner, handler: null) { }

    internal PinataPublicBlockQuerySource(Guid owner, HttpMessageHandler? handler)
        : this(new BlockBackend(owner, handler), owner) { }

    private PinataPublicBlockQuerySource(BlockBackend backend, Guid owner) : base(backend, "pinata-public-block:")
    {
        this.backend = backend;
        ProbeId = LogicalId(owner, Root);
        ProviderType = new EnumValue<PType>(QueryProviderType);
        ProviderName = QueryProviderType.ToString();
        ProviderDescription = "Verified public Pinata IPFS block metadata";
        ProviderCategory = new EnumValue<Core.Enums.ProviderCategory>(Core.Enums.ProviderCategory.Storage);
        query = new(QueryProviderType, backend.ReadAsync);
    }

    private Guid ProbeId { get; }

    public static Guid LogicalId(Guid owner, string cid)
    {
        if (owner == Guid.Empty || cid is null || !Reviewed.Contains(cid))
            throw new ArgumentException("A teaching owner and reviewed public IPFS block CID are required.");
        return NativeRecordHolonMapper.StableHolonId(owner, PType.PinataOASIS, Source, cid);
    }

    public PType QueryProviderType => PType.PinataOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(ProbeId, cancellationToken);
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

    private sealed class BlockBackend : IKeyValueBackend, IDisposable
    {
        private const string RawBlockMediaType = "application/vnd.ipld.raw";
        private const int MaximumBlockBytes = 512 * 1024;
        private readonly Guid owner;
        private readonly Dictionary<Guid, Cid> blocks;
        private readonly HttpClient client;

        public BlockBackend(Guid owner, HttpMessageHandler? handler)
        {
            this.owner = owner;
            blocks = Reviewed.ToDictionary(value => LogicalId(owner, value), Cid.Decode);
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
            if (!blocks.TryGetValue(id, out var block))
                throw new InvalidDataException("The selected ID is outside the reviewed Pinata block catalogue.");

            var cid = block.Encode();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://gateway.pinata.cloud/ipfs/{cid}?format=raw");
            request.Headers.Accept.ParseAdd(RawBlockMediaType);
            request.Headers.UserAgent.ParseAdd("OASISAPI/1.0");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentType?.MediaType != RawBlockMediaType)
                throw new InvalidDataException("Pinata must return the requested IPFS raw block representation.");

            await response.Content.LoadIntoBufferAsync(MaximumBlockBytes, token);
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            if (bytes.Length == 0 || !block.Hash.Matches(bytes))
                throw new InvalidDataException("The Pinata block bytes differ from the requested CID.");

            var record = new NativeRecord(Source, cid, new Dictionary<string, object>
            {
                ["name"] = cid,
                ["PinataCid"] = cid,
                ["PinataBlockBytes"] = bytes.Length,
                ["PinataBlockSha256"] = Convert.ToHexStringLower(block.Hash.Digest),
                ["PinataBlockCodec"] = block.ContentType
            });
            var mapped = NativeRecordHolonMapper.CreateHolon(owner, PType.PinataOASIS, record, NativeDataMapping.ForSource(Source));
            if (mapped.IsError) throw new InvalidDataException(mapped.ErrorCode);
            var holon = mapped.Result;
            holon.CreatedByAvatarId = owner;
            holon.IsPublic = true;
            holon.IsActive = true;
            holon.Children = Array.Empty<IHolon>();
            holon.ProviderUniqueStorageKey[PType.PinataOASIS] = cid;
            return StoredRecordJson.Serialize<IHolon>(holon);
        }

        public Task VerifyAsync(CancellationToken cancellationToken = default) => Unsupported();
        public Task<string> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new NotSupportedException("Use an admitted exact Pinata block CQL plan."));
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<string>>(new NotSupportedException("Pinata block scans are unavailable."));
        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default) => Unsupported();
        public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Unsupported();
        private static Task Unsupported() => Task.FromException(new NotSupportedException("Pinata exposes exact public block reads."));
        public void Dispose() => client.Dispose();
    }
}
