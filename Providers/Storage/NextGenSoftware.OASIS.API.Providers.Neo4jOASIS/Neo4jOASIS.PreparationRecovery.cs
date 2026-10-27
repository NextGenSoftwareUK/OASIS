using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : IHolonWritePreparationRecoveryStorageProvider
{
    public async Task<OASISResult<HolonWritePreparation>> ReadHolonWritePreparationAsync(
        Guid operationId, Guid actorId, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<HolonWritePreparation>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (operationId == Guid.Empty || actorId == Guid.Empty)
                return PreparationFailure(result, "COSMIC_PREPARATION_INVALID", new ArgumentException("Operation and authenticated actor are required."));
            if (!IsProviderActivated || _driver == null)
                return PreparationFailure(result, "COSMIC_PREPARATION_PROVIDER_NOT_READY", new InvalidOperationException("Activate Neo4j before reading preparation."));
            using var session = _driver.AsyncSession();
            // MATCH only: this lookup must never claim or lock the receipt.
            var rows = await (await session.RunAsync(
                "MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r LIMIT 2",
                new { operation = operationId.ToString("D") })).ToListAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (rows.Count > 1) throw new InvalidDataException("Preparation identity is not unique.");
            if (rows.Count == 1)
            {
                var node = rows[0]["r"].As<Neo4j.Driver.INode>();
                if (node.Properties.ContainsKey("PreparationJson"))
                {
                    RequirePreparationOwner(node, actorId);
                    result.Result = ProtectedPreparationJson.Read(operationId, actorId, Core.Enums.ProviderType.Neo4jOASIS,
                        (string)node.Properties["PreparationJson"]);
                }
                else if (node.Properties.ContainsKey("PreparationActorId") || node.Properties.ContainsKey("PreparationFormat")
                    || node.Properties.ContainsKey("PreparationCreatedAtUtc"))
                    throw new InvalidDataException("Stored preparation is incomplete.");
            }
            result.IsLoaded = true;
            return result;
        }
        catch (Exception cause)
        {
            return PreparationFailure(result, cause is PreparationActorException ? "COSMIC_PREPARATION_ACTOR_MISMATCH"
                : cause is OperationCanceledException ? "COSMIC_PREPARATION_CANCELLED" : "COSMIC_PREPARATION_UNVERIFIED", cause);
        }
    }
}
