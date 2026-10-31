using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<OASISResult<HolonReplicaInitializationOutcome>> ReadReplicaInitializationOutcomeAsync(CommittedHolonReplicaRequest original,CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            var plan=CommittedHolonReplicaPlan.CreateInitialBaseline(original,ProviderType.MongoDBOASIS);
            var rows=await _dbContext.MongoDB.GetCollection<BsonDocument>("CosmicWriteReceipt").WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority)
                .Find(new BsonDocument("_id",plan.OperationId.ToString("D"))).Limit(2).ToListAsync(token).ConfigureAwait(false);
            if(rows.Count>1)throw new InvalidDataException("Initialization receipt identity is ambiguous.");
            string digest=null;
            if(rows.Count==1)
            {
                var row=rows.Single();
                if(row.GetValue("Fingerprint",BsonNull.Value)!=plan.Fingerprint || !row.TryGetValue("ResultJson",out var body) || !body.IsString)
                    throw new InvalidDataException("Initialization receipt does not match the original intent.");
                _=ConditionalHolonReceipt.Read(body.AsString,plan);digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.AsString)));
            }
            return new(new(plan.OperationId,ProviderType.MongoDBOASIS,digest==null ? ConditionalHolonReceiptState.Absent:ConditionalHolonReceiptState.Committed,plan.Fingerprint,digest)){IsLoaded=true};
        }
        catch(Exception cause){return new(){IsError=true,ErrorCount=1,ErrorCode=cause is OperationCanceledException
            ? "COSMIC_INITIALIZATION_STATUS_CANCELLED":"COSMIC_INITIALIZATION_STATUS_UNVERIFIED",Message="The original initialization receipt could not be verified.",Exception=cause};}
    }
}
