using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Google.Cloud.BigQuery.V2;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS;

public sealed class BigQueryQuerySource : BigQueryOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public BigQueryQuerySource(string project, string dataset, Guid probeId) : base(project, dataset)
    {
        if (!Regex.IsMatch(project ?? "", @"\A[a-z][a-z0-9-]{4,28}[a-z0-9]\z") ||
            !Regex.IsMatch(dataset ?? "", @"\A[A-Za-z_][A-Za-z0-9_]{0,127}\z") || probeId == Guid.Empty)
            throw new ArgumentException("BigQuery requires a project, teaching dataset and public probe.");

        this.probeId = probeId;
        query = new BoundedPublicKeyValueQuery(QueryProviderType, ReadAsync, maximumConcurrentReads: 1);

        async Task<string> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            using var client = await new BigQueryClientBuilder { ProjectId = project }.BuildAsync(cancellationToken);
            var key = id.ToString("D");
            var rows = await client.ExecuteQueryAsync(
                $"SELECT id, doc FROM `{project}.{dataset}.oasis_holons` WHERE id = @id LIMIT 2",
                new[] { new BigQueryParameter("id", BigQueryDbType.String, key) },
                new QueryOptions { UseLegacySql = false, UseQueryCache = false, MaximumBytesBilled = 10 * 1024 * 1024 },
                cancellationToken: cancellationToken);
            await using var iterator = rows.GetRowsAsync().GetAsyncEnumerator(cancellationToken);
            if (!await iterator.MoveNextAsync()) return null!;
            if (iterator.Current["id"] is not string returnedKey || returnedKey != key ||
                iterator.Current["doc"] is not string json)
                throw new InvalidDataException("BigQuery returned an invalid teaching record.");
            if (await iterator.MoveNextAsync())
                throw new InvalidDataException("BigQuery returned duplicate teaching records.");
            return json;
        }
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.BigQueryOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();
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
}
