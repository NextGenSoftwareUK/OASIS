using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : IConditionalHolonWriteRecoveryStorageProvider
{
    public async Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plan = original?.Write.ReplicationEnrollment == null
                ? ConditionalHolonPlan.Create(original, Core.Enums.ProviderType.MongoDBOASIS)
                : ConditionalHolonPlan.CreateReplicating(original, Core.Enums.ProviderType.MongoDBOASIS, original.Write.ReplicationEnrollment);
            if (!IsProviderActivated)throw new InvalidOperationException("MongoDB provider is not activated.");
            BsonDocument receipt;
            if(plan.ReplicationEnrollment?.SourceStorageIdentity!=null)
            {
                var repository=_holonRepository ?? throw new InvalidOperationException("The native repository is closed.");
                receipt=await repository.ReadBoundConditionalReceiptAsync(plan.OperationId,plan.ReplicationEnrollment.SourceStorageIdentity,cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var database=Database?.MongoDB ?? throw new InvalidOperationException("MongoDB provider is not activated.");
                receipt=await database.GetCollection<BsonDocument>("CosmicWriteReceipt")
                    .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority)
                    .Find(new BsonDocument("_id",plan.OperationId.ToString("D"))).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            }
            HolonCommittedDelta delta = null;
            var state = ConditionalHolonReceiptState.Absent;
            if (receipt != null)
            {
                if (!receipt.TryGetValue("Fingerprint", out var fingerprint) || !fingerprint.IsString
                    || fingerprint.AsString != plan.Fingerprint
                    || !receipt.TryGetValue("ResultJson", out var json) || !json.IsString)
                    throw new InvalidDataException("Receipt is not bound to the original conditional intent.");
                _ = ConditionalHolonReceipt.Read(json.AsString, plan, out delta);
                state = ConditionalHolonReceiptState.Committed;
            }
            return new OASISResult<ConditionalHolonWriteOutcome>(new ConditionalHolonWriteOutcome(
                plan.OperationId, plan.ActorId, plan.Authority, plan.Operation, state, delta?.SummaryJson)) { IsLoaded = true };
        }
        catch (Exception cause)
        {
            return new OASISResult<ConditionalHolonWriteOutcome> { IsError = true, ErrorCount = 1,
                ErrorCode = cause is OperationCanceledException ? "COSMIC_OPERATION_STATUS_CANCELLED" : "COSMIC_OPERATION_STATUS_UNVERIFIED",
                Message = "The original conditional operation receipt could not be verified.", Exception = cause };
        }
    }
}
