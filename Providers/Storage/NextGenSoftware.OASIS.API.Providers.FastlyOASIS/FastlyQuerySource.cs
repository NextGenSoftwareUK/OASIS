using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.FastlyOASIS;

/// <summary>Bounded public reads from one reviewed Fastly KV store.</summary>
public sealed class FastlyQuerySource : FastlyOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly Guid probeId;
    private readonly BoundedPublicKeyValueQuery query;

    public FastlyQuerySource(string storeId, string readerToken, Guid probeId)
        : this(storeId, readerToken, probeId, null) { }

    internal FastlyQuerySource(string storeId, string readerToken, Guid probeId, HttpMessageHandler handler)
        : base(CreateBackend(storeId, readerToken, probeId, handler))
    {
        this.probeId = probeId;
        query = new(QueryProviderType, (id, token) => Backend.GetAsync(RecordKey("holon", id), token));
    }

    private static FastlyKvBackend CreateBackend(string storeId, string readerToken, Guid probeId, HttpMessageHandler handler)
    {
        if (probeId == Guid.Empty || readerToken is not { Length: > 0 and <= 1024 } ||
            readerToken.Any(character => character <= ' ' || character > '~') ||
            storeId is not { Length: > 0 and <= 64 } ||
            storeId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')))
            throw new ArgumentException("A reviewed Fastly store, reader and existing teaching probe are required.");
        return new FastlyKvBackend(readerToken, storeId, handler, queryOnly: true);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.FastlyOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
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
        ((FastlyKvBackend)Backend).Dispose();
    }
}
