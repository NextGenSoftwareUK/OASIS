using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MotherDuckOASIS;

public sealed class MotherDuckQuerySource : MotherDuckOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public MotherDuckQuerySource(string database, string readerToken, Guid probeId)
        : base("md:" + database)
    {
        if (database is not { Length: > 0 and <= 63 } || !char.IsAsciiLetter(database[0]) ||
            !database.All(c => char.IsAsciiLetterOrDigit(c) || c == '_') ||
            readerToken is not { Length: > 0 and <= 4096 } ||
            readerToken.Any(c => c is < '!' or > '~') || probeId == Guid.Empty)
            throw new ArgumentException("An explicit MotherDuck database, restricted reader and public probe are required.");

        this.probeId = probeId;
        var connectionString = $"Data Source=md:{database}?motherduck_token={Uri.EscapeDataString(readerToken)}";
        query = new BoundedPublicKeyValueQuery(QueryProviderType, ReadAsync);

        async Task<string> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            await using var connection = new DuckDBConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT id, CASE WHEN octet_length(encode(doc)) <= 1048576 THEN doc END
                FROM oasis_holons WHERE id = $id LIMIT 2
                """;
            command.Parameters.Add(new DuckDBParameter("id", id.ToString("D")));
            await using var rows = await command.ExecuteReaderAsync(cancellationToken);
            if (!await rows.ReadAsync(cancellationToken)) return null!;
            if (rows.GetString(0) != id.ToString("D") || rows.IsDBNull(1))
                throw new InvalidDataException("MotherDuck returned an invalid teaching record.");
            var json = rows.GetString(1);
            if (await rows.ReadAsync(cancellationToken))
                throw new InvalidDataException("MotherDuck returned duplicate teaching records.");
            return json;
        }
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.MotherDuckOASIS;
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
