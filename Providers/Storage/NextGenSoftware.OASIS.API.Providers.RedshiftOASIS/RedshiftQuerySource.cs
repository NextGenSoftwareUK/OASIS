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
using Npgsql;

namespace NextGenSoftware.OASIS.API.Providers.RedshiftOASIS;

public sealed class RedshiftQuerySource : RedshiftOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public RedshiftQuerySource(string connectionString, Guid probeId) : base(connectionString)
    {
        var settings = new NpgsqlConnectionStringBuilder(connectionString);
        if (settings.SslMode != SslMode.VerifyFull || string.IsNullOrWhiteSpace(settings.Host) ||
            settings.Host.Contains(',') || string.IsNullOrWhiteSpace(settings.Database) ||
            string.IsNullOrWhiteSpace(settings.Username) || probeId == Guid.Empty)
            throw new ArgumentException("A certificate-verified Redshift reader and public probe are required.");

        this.probeId = probeId;
        query = new BoundedPublicKeyValueQuery(QueryProviderType, ReadAsync);

        async Task<string> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                "SELECT id, doc FROM oasis_holons WHERE id = @id LIMIT 2", connection);
            var key = id.ToString("D");
            command.Parameters.AddWithValue("id", key);
            await using var rows = await command.ExecuteReaderAsync(cancellationToken);
            if (!await rows.ReadAsync(cancellationToken)) return null!;
            if (rows.GetString(0) != key || rows.IsDBNull(1))
                throw new InvalidDataException("Redshift returned an invalid teaching record.");
            var json = rows.GetString(1);
            if (await rows.ReadAsync(cancellationToken))
                throw new InvalidDataException("Redshift returned duplicate teaching records.");
            return json;
        }
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.RedshiftOASIS;
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
