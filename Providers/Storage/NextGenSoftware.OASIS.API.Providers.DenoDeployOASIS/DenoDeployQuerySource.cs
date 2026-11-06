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

namespace NextGenSoftware.OASIS.API.Providers.DenoDeployOASIS;

/// <summary>Bounded public reads from one reviewed Deno KV database.</summary>
public sealed class DenoDeployQuerySource : DenoDeployOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly Guid probeId;
    private readonly BoundedPublicKeyValueQuery query;

    public DenoDeployQuerySource(Guid databaseId, Uri reviewedEndpoint, string readerToken, Guid probeId)
        : this(databaseId, reviewedEndpoint, readerToken, probeId, null) { }

    internal DenoDeployQuerySource(Guid databaseId, Uri reviewedEndpoint, string readerToken, Guid probeId, HttpMessageHandler handler)
        : base(CreateBackend(databaseId, reviewedEndpoint, readerToken, probeId, handler))
    {
        this.probeId = probeId;
        query = new(QueryProviderType, (id, token) => Backend.GetAsync(RecordKey("holon", id), token));
    }

    private static DenoKvConnectBackend CreateBackend(Guid databaseId, Uri reviewedEndpoint, string readerToken, Guid probeId, HttpMessageHandler handler)
    {
        if (databaseId == Guid.Empty || probeId == Guid.Empty || !IsValidToken(readerToken) ||
            reviewedEndpoint is not { IsAbsoluteUri: true, Scheme: "https", HostNameType: UriHostNameType.Dns } ||
            !reviewedEndpoint.IsDefaultPort || reviewedEndpoint.UserInfo.Length != 0 ||
            reviewedEndpoint.Query.Length != 0 || reviewedEndpoint.Fragment.Length != 0)
            throw new ArgumentException("A reviewed Deno KV database, HTTPS endpoint, reader and existing teaching probe are required.");

        var connectUrl = $"https://api.deno.com/v2/databases/{databaseId:D}/connect";
        return new DenoKvConnectBackend(connectUrl, readerToken, handler, reviewedEndpoint, databaseId);
    }

    internal static bool IsValidToken(string token) =>
        token is { Length: > 0 and <= 8192 } && token.All(character => character > ' ' && character <= '~');

    public ProviderType QueryProviderType => Core.Enums.ProviderType.DenoDeployOASIS;
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
        ((DenoKvConnectBackend)Backend).Dispose();
    }
}
