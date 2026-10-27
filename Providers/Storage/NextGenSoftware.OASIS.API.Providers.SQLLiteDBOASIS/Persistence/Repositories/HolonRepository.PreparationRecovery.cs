using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public Task<OASISResult<HolonWritePreparation>> ReadHolonWritePreparationAsync(
        Guid operationId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<HolonWritePreparation>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (operationId == Guid.Empty || actorId == Guid.Empty)
                return Task.FromResult(PreparationError("COSMIC_PREPARATION_INVALID", "Operation and authenticated actor are required."));
            var settings = new SqliteConnectionStringBuilder(_dbContext.Database.GetDbConnection().ConnectionString);
            if (settings.Mode == SqliteOpenMode.Memory || settings.DataSource == ":memory:")
                throw new InvalidOperationException("Durable preparation recovery requires a file-backed SQLite authority.");
            settings.Mode = SqliteOpenMode.ReadOnly;
            settings.Pooling = false;
            using var connection = new SqliteConnection(settings.ToString());
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT ActorId,length(PayloadJson),PayloadJson FROM CosmicWritePreparation WHERE OperationId=$op LIMIT 2";
            command.Parameters.AddWithValue("$op", operationId.ToString("D"));
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                if (reader.IsDBNull(0) || reader.GetString(0) != actorId.ToString("D"))
                    return Task.FromResult(PreparationError("COSMIC_PREPARATION_ACTOR_MISMATCH", "The operation is owned by another actor."));
                if (reader.IsDBNull(1) || reader.GetInt64(1) > ProtectedPreparationJson.MaximumPayloadCharacters)
                    throw new InvalidDataException("Stored preparation exceeds its transport limit.");
                result.Result = ProtectedPreparationJson.Read(operationId, actorId, Core.Enums.ProviderType.SQLLiteDBOASIS, reader.GetString(2));
                if (reader.Read()) throw new InvalidDataException("Preparation identity is not unique.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            result.IsLoaded = true;
        }
        catch (Exception cause)
        {
            result = PreparationError(cause is OperationCanceledException ? "COSMIC_PREPARATION_CANCELLED" : "COSMIC_PREPARATION_UNVERIFIED",
                "The original protected preparation could not be read.", cause);
        }
        return Task.FromResult(result);
    }
}
