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
            var database = Database?.MongoDB;
            if (!IsProviderActivated || database == null)
                throw new InvalidOperationException("MongoDB provider is not activated.");
            // Observe durable acknowledgement on the authority. No operation
            // fence, transaction claim, preparation or mutation is entered.
            var collection = database.GetCollection<BsonDocument>("CosmicWriteReceipt")
                .WithReadPreference(ReadPreference.Primary).WithReadConcern(ReadConcern.Majority);
            var receipt = await collection.Find(new BsonDocument("_id", plan.OperationId.ToString("D")))
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
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
