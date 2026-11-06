using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.AppwriteOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.AppwriteOASIS;

/// <summary>Bounded reads from one reviewed Appwrite teaching collection.</summary>
public sealed class AppwriteQuerySource : AppwriteOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly Guid probeId;
    private readonly BoundedPublicKeyValueQuery query;

    public AppwriteQuerySource(Uri endpoint, string projectId, string databaseId, string readerToken, Guid probeId)
        : this(endpoint, projectId, databaseId, readerToken, probeId, null) { }

    internal AppwriteQuerySource(Uri endpoint, string projectId, string databaseId, string readerToken, Guid probeId, HttpMessageHandler? handler)
        : base(endpoint?.AbsoluteUri ?? throw new ArgumentNullException(nameof(endpoint)), projectId, readerToken, databaseId,
            CreateClient(endpoint, projectId, databaseId, readerToken, probeId, handler), queryOnly: true)
    {
        this.probeId = probeId;
        query = new(QueryProviderType, ReadExactHolonAsync);
    }

    private static HttpClient CreateClient(Uri endpoint, string projectId, string databaseId, string readerToken, Guid probeId, HttpMessageHandler? handler)
    {
        static bool IsIdentifier(string value) => value is { Length: > 0 and <= 36 } &&
            char.IsAsciiLetterOrDigit(value[0]) && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_');
        if (endpoint is not { IsAbsoluteUri: true, Scheme: "https", HostNameType: UriHostNameType.Dns, IsDefaultPort: true } ||
            endpoint.UserInfo.Length != 0 || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0 ||
            endpoint.AbsolutePath.TrimEnd('/') != "/v1" || !IsIdentifier(projectId) || !IsIdentifier(databaseId) ||
            probeId == Guid.Empty || readerToken is not { Length: > 0 and <= 1024 } || readerToken.Any(c => c <= ' ' || c > '~'))
            throw new ArgumentException("A reviewed Appwrite HTTPS endpoint, project, database, reader and public probe are required.");
        if (handler != null) return new HttpClient(handler, disposeHandler: false);
        var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        transport.UseProxy = false;
        transport.UseCookies = false;
        return new HttpClient(transport);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.AppwriteOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync();

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        var result = await query.VerifyProbeAsync(probeId, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public override Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        IsProviderActivated = false;
        return base.DeActivateProviderAsync();
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        query.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    public void Dispose()
    {
        IsProviderActivated = false;
        DisposeTransport();
    }
}
