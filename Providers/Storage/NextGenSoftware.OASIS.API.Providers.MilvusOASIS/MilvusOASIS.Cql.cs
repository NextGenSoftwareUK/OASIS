using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS;

public partial class MilvusOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly Guid _queryProbeId;
    private readonly BoundedPublicKeyValueQuery? _publicQuery;

    public MilvusOASIS(string baseUrl, string? token, Guid publicProbeId)
        : this(baseUrl, token, publicProbeId, null) { }

    internal MilvusOASIS(string baseUrl, string? token, Guid publicProbeId, HttpMessageHandler? transport)
        : this(baseUrl, token, transport)
    {
        if (publicProbeId == Guid.Empty)
            throw new ArgumentException("An existing public probe is required.", nameof(publicProbeId));
        _queryProbeId = publicProbeId;
        _publicQuery = new(QueryProviderType, (id, cancellationToken) =>
            ReadExactDocumentAsync(CollHolons, id.ToString("D"), cancellationToken)!);
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.MilvusOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken = default)
    {
        IsProviderActivated = false;
        if (_publicQuery == null)
            return QueryUnavailable<bool>();
        var result = await _publicQuery.VerifyProbeAsync(_queryProbeId, cancellationToken);
        IsProviderActivated = !result.IsError && result.Result;
        return result;
    }

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon =>
        _publicQuery == null
            ? Task.FromResult(QueryUnavailable<IReadOnlyList<IHolon>>())
            : _publicQuery.ExecuteAsync(plan, maximumResults, IsProviderActivated, IsVersionControlEnabled, cancellationToken);

    private static OASISResult<T> QueryUnavailable<T>() => new()
    {
        IsError = true, ErrorCount = 1, ErrorCode = "CQL_SOURCE_UNAVAILABLE",
        Message = "Configure an existing public Milvus probe before activating the query source."
    };
}
