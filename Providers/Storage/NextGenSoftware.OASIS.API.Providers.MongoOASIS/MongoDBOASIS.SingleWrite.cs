using System;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    private async Task<OASISResult<IHolon>> SaveSingleCapturedHolonAsync(IHolon source, bool children, bool recursive, int depth, bool synchronous)
    {
        var admitted = false;
        try
        {
            // One detached admission owns scalar values and selected membership. Routing
            // cannot inspect caller getters again to choose the native mutation owner.
            var graph = HolonGraphWritePlan.Create(new[] { source }, children, recursive, depth, 0);
            admitted = true;
            if (graph.Nodes.Count > 1)
            {
                var saved = await SaveGraphsAtomicAsync(graph.Roots, children, recursive, depth, 0, capturedGraph: graph).ConfigureAwait(false);
                return new OASISResult<IHolon> { Result = saved.IsError ? null : saved.Result.Single(),
                    IsError = saved.IsError, IsSaved = saved.IsSaved, IsWarning = saved.IsWarning,
                    WarningCount = saved.WarningCount, SavedCount = saved.SavedCount, ErrorCount = saved.ErrorCount,
                    Message = saved.Message, Exception = saved.Exception, ErrorCode = saved.ErrorCode };
            }
            // A single record retains the native atomic replacement path, including
            // standalone Mongo support. Resolve by public identity, never caller audit flags.
            var node = graph.Nodes.Single();
            var entity = DataHelper.ConvertOASISHolonToMongoEntity(node.Snapshot, children);
            var previous = synchronous ? _holonRepository.GetHolon(entity.HolonId)
                : await _holonRepository.GetHolonAsync(entity.HolonId).ConfigureAwait(false);
            PreserveCreationAuditFields(entity, previous);
            var mutation = previous == null
                ? synchronous ? _holonRepository.Add(entity) : await _holonRepository.AddAsync(entity).ConfigureAwait(false)
                : synchronous ? _holonRepository.Update(entity, preserveChildren: !children)
                    : await _holonRepository.UpdateAsync(entity, preserveChildren: !children).ConfigureAwait(false);
            var result = DataHelper.ConvertMongoEntityToOASISHolon(mutation, children);
            if (!result.IsError && result.IsSaved)
            { node.Source.Id = node.Snapshot.Id; node.Source.ParentHolonId = node.Snapshot.ParentHolonId; }
            return result;
        }
        catch (Exception cause)
        {
            return new OASISResult<IHolon> { IsError = true, ErrorCount = 1, Exception = cause,
                ErrorCode = !admitted ? "HYPERDRIVE_PAYLOAD_INVALID" : cause is StorageIdentityAmbiguousException ? OASISResultErrorCodes.AmbiguousIdentity : null,
                Message = admitted ? "Mongo single holon save failed." : "Mongo single holon admission failed." };
        }
    }
}
