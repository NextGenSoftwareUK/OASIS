using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using INode = Neo4j.Driver.INode;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : IConditionalHolonWriteRecoveryStorageProvider
{
    public async Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonWriteOutcomeAsync(
        ConditionalHolonWriteRequest original, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plan = original?.Write.ReplicationEnrollment == null
                ? ConditionalHolonPlan.Create(original, Core.Enums.ProviderType.Neo4jOASIS)
                : ConditionalHolonPlan.CreateReplicating(original, Core.Enums.ProviderType.Neo4jOASIS, original.Write.ReplicationEnrollment);
            if (!IsProviderActivated || _driver == null)
                throw new InvalidOperationException("Neo4j provider is not activated.");
            using var session = _driver.AsyncSession();
            // Deliberately no MERGE, claim, preparation, lock or mutation. An
            // absent/incomplete receipt does not establish a terminal rejection.
            var rows = await (await session.RunAsync(
                "MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r LIMIT 2",
                new { operation = plan.OperationId.ToString("D") })).ToListAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (rows.Count > 1) throw new InvalidDataException("Conditional operation receipt identity is not unique.");
            HolonCommittedDelta delta = null;
            var state = ConditionalHolonReceiptState.Absent;
            if (rows.Count == 1)
            {
                var receipt = rows[0]["r"].As<INode>();
                if (receipt.Properties.TryGetValue("ResultJson", out var stored))
                {
                    if (!receipt.Properties.TryGetValue("Fingerprint", out var fingerprint)
                        || fingerprint is not string intent || intent != plan.Fingerprint)
                        throw new InvalidDataException("Receipt is not bound to the original conditional intent.");
                    if (stored is not string json) throw new InvalidDataException("Stored conditional receipt is invalid.");
                    // Reuse the execution owner's pure integrity/actor/authority/
                    // topology/revision validator without its write-path lookup.
                    _ = ConditionalHolonReceipt.Read(json, plan, out delta);
                    state = ConditionalHolonReceiptState.Committed;
                }
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
