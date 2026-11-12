using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PlanetScaleOASIS;

public sealed class PlanetScaleQuerySource : PlanetScaleOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public PlanetScaleQuerySource(string connectionString, Guid probeId) : base(connectionString)
    {
        if (probeId == Guid.Empty)
            throw new ArgumentException("An existing public teaching probe is required.", nameof(probeId));
        var settings = new MySqlConnectionStringBuilder(connectionString);
        if (settings.SslMode != MySqlSslMode.VerifyFull || string.IsNullOrWhiteSpace(settings.Server)
            || string.IsNullOrWhiteSpace(settings.Database) || string.IsNullOrWhiteSpace(settings.UserID))
            throw new ArgumentException("A PlanetScale teaching reader with verified TLS is required.", nameof(connectionString));

        this.probeId = probeId;
        query = new BoundedPublicKeyValueQuery(QueryProviderType, ReadAsync);

        async Task<string> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new MySqlCommand("""
                SELECT id, data_json FROM oasis_holons
                WHERE id = @id AND is_deleted = 0 LIMIT 2
                """, connection);
            var key = id.ToString("D");
            command.Parameters.AddWithValue("@id", key);
            await using var rows = await command.ExecuteReaderAsync(cancellationToken);
            if (!await rows.ReadAsync(cancellationToken)) return null!;
            if (!string.Equals(rows.GetString(0), key, StringComparison.Ordinal) || rows.IsDBNull(1)
                || rows.GetChars(1, 0, null, 0, 0) > 1024 * 1024)
                throw new InvalidDataException("PlanetScale returned an invalid teaching record.");
            var json = rows.GetString(1);
            if (await rows.ReadAsync(cancellationToken))
                throw new InvalidDataException("PlanetScale returned duplicate teaching records.");
            return json;
        }
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.PlanetScaleOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();
    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public override Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        IsProviderActivated = false;
        return base.DeActivateProviderAsync();
    }

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
