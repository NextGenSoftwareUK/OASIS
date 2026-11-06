using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Dapr.Client;
using Grpc.Net.Client;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DaprOASIS;

/// <summary>Bounded public reads from one reviewed local Dapr state component.</summary>
public sealed class DaprQuerySource : DaprOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly Guid probeId;
    private readonly BoundedPublicKeyValueQuery query;

    public DaprQuerySource(string storeName, Uri sidecar, string readerToken, Guid probeId)
        : this(storeName, probeId, CreateClient(storeName, sidecar, readerToken, probeId)) { }

    internal DaprQuerySource(string storeName, Guid probeId, IDaprStateApi api)
        : base(new DaprStateBackend(api, storeName, queryOnly: true))
    {
        this.probeId = probeId;
        query = new(QueryProviderType, (id, token) => Backend.GetAsync(RecordKey("holon", id), token));
    }

    private static DaprClientStateApi CreateClient(string storeName, Uri sidecar, string readerToken, Guid probeId)
    {
        if (storeName is not { Length: > 0 and <= 63 } ||
            !char.IsAsciiLetterOrDigit(storeName[0]) || !char.IsAsciiLetterOrDigit(storeName[^1]) ||
            storeName.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-') || storeName != storeName.ToLowerInvariant() ||
            probeId == Guid.Empty || readerToken is not { Length: > 0 and <= 1024 } ||
            readerToken.Any(c => c <= ' ' || c > '~') ||
            sidecar is not { IsAbsoluteUri: true, Scheme: "http" } ||
            sidecar.Host != IPAddress.Loopback.ToString() || sidecar.Port is <= 0 or 80 ||
            sidecar.UserInfo.Length != 0 || sidecar.AbsolutePath != "/" ||
            sidecar.Query.Length != 0 || sidecar.Fragment.Length != 0)
            throw new ArgumentException("An explicit loopback sidecar, state component, token and teaching probe are required.");

        var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false, UseCookies = false };
        var client = new DaprClientBuilder()
            .UseGrpcEndpoint(sidecar.AbsoluteUri)
            .UseDaprApiToken(readerToken)
            .UseGrpcChannelOptions(new GrpcChannelOptions
            {
                HttpHandler = handler,
                DisposeHttpClient = true,
                MaxReceiveMessageSize = 1024 * 1024
            }).Build();
        return new DaprClientStateApi(client);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.DaprOASIS;
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
        ((DaprStateBackend)Backend).Dispose();
    }
}
