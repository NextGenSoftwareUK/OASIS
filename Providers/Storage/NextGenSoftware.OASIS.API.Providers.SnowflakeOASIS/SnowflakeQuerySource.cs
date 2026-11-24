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
using System.Data;
using System.Data.Common;
using Snowflake.Data.Client;

namespace NextGenSoftware.OASIS.API.Providers.SnowflakeOASIS;

public sealed class SnowflakeQuerySource : SnowflakeOASIS, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly BoundedPublicKeyValueQuery query;
    private readonly Guid probeId;

    public SnowflakeQuerySource(string connectionString, Guid probeId) : base(connectionString)
    {
        var settings = new DbConnectionStringBuilder { ConnectionString = connectionString };
        string[] required = ["account", "user", "db", "schema", "warehouse", "role"];
        var allowed = new HashSet<string>(required, StringComparer.OrdinalIgnoreCase)
        {
            "authenticator", "password", "private_key", "private_key_pwd", "token"
        };
        foreach (string key in settings.Keys)
            if (!allowed.Contains(key))
                throw new ArgumentException("Unsupported Snowflake reader connection setting.");
        foreach (var key in required)
            if (!settings.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value?.ToString()))
                throw new ArgumentException("Snowflake requires a dedicated account, user, database, schema, warehouse and role.");
        if (probeId == Guid.Empty)
            throw new ArgumentException("A public Snowflake teaching probe is required.", nameof(probeId));

        this.probeId = probeId;
        query = new BoundedPublicKeyValueQuery(QueryProviderType, ReadAsync);

        async Task<string> ReadAsync(Guid id, CancellationToken cancellationToken)
        {
            await using var connection = new SnowflakeDbConnection { ConnectionString = settings.ConnectionString };
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, doc FROM oasis_holons WHERE id = ? LIMIT 2";
            var key = id.ToString("D");
            var parameter = command.CreateParameter();
            parameter.ParameterName = "1";
            parameter.DbType = DbType.String;
            parameter.Value = key;
            command.Parameters.Add(parameter);
            await using var rows = await command.ExecuteReaderAsync(cancellationToken);
            if (!await rows.ReadAsync(cancellationToken)) return null!;
            if (rows.GetString(0) != key || rows.IsDBNull(1))
                throw new InvalidDataException("Snowflake returned an invalid teaching record.");
            var json = rows.GetString(1);
            if (await rows.ReadAsync(cancellationToken))
                throw new InvalidDataException("Snowflake returned duplicate teaching records.");
            return json;
        }
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.SnowflakeOASIS;
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
