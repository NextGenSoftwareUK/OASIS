using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public Task<OASISResult<HolonReplicaInitializationOutcome>> ReadReplicaInitializationOutcomeAsync(CommittedHolonReplicaRequest original,CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            var plan=CommittedHolonReplicaPlan.CreateInitialBaseline(original,ProviderType.SQLLiteDBOASIS);
            var settings=new SqliteConnectionStringBuilder(_dbContext.Database.GetDbConnection().ConnectionString);
            if(settings.Mode==SqliteOpenMode.Memory || settings.DataSource==":memory:")throw new InvalidOperationException("Durable receipt recovery requires a file-backed authority.");
            settings.Mode=SqliteOpenMode.ReadOnly;settings.Pooling=false;
            using var connection=new SqliteConnection(settings.ToString());connection.Open();
            using var command=connection.CreateCommand();
            command.CommandText="SELECT RequestHash,ResultJson FROM CosmicWriteReceipt WHERE OperationId=$id LIMIT 2";
            command.Parameters.AddWithValue("$id",plan.OperationId.ToString("D"));using var reader=command.ExecuteReader();
            string digest=null;
            if(reader.Read())
            {
                if(reader.IsDBNull(0) || reader.GetString(0)!=plan.Fingerprint || reader.IsDBNull(1))throw new InvalidDataException("Initialization receipt does not match the original intent.");
                var json=reader.GetString(1);_=ConditionalHolonReceipt.Read(json,plan);
                digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
                if(reader.Read())throw new InvalidDataException("Initialization receipt identity is ambiguous.");
            }
            token.ThrowIfCancellationRequested();
            return Task.FromResult(new OASISResult<HolonReplicaInitializationOutcome>(new(plan.OperationId,ProviderType.SQLLiteDBOASIS,
                digest==null ? ConditionalHolonReceiptState.Absent:ConditionalHolonReceiptState.Committed,plan.Fingerprint,digest)){IsLoaded=true});
        }
        catch(Exception cause){return Task.FromResult(new OASISResult<HolonReplicaInitializationOutcome>{IsError=true,ErrorCount=1,
            ErrorCode=cause is OperationCanceledException ? "COSMIC_INITIALIZATION_STATUS_CANCELLED":"COSMIC_INITIALIZATION_STATUS_UNVERIFIED",
            Message="The original initialization receipt could not be verified.",Exception=cause});}
    }
}
