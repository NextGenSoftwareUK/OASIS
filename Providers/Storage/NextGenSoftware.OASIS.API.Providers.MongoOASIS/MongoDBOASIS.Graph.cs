using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using DataHelper = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public partial class MongoDBOASIS
    {
        private async Task<OASISResult<IHolon>> SaveGraphAtomicAsync(IHolon root, bool recursive, int maxChildDepth)
        {
            var saved = await SaveGraphsAtomicAsync(new[] { root }, true, recursive, maxChildDepth, 0, true).ConfigureAwait(false);
            return new OASISResult<IHolon> { Result = saved.IsError ? null : saved.Result.Single(),
                IsError = saved.IsError, IsSaved = saved.IsSaved, IsWarning = saved.IsWarning, WarningCount = saved.WarningCount, SavedCount = saved.SavedCount, Message = saved.Message,
                Exception = saved.Exception, ErrorCode = saved.ErrorCode };
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> SaveGraphsAtomicAsync(IEnumerable<IHolon> source,
            bool saveChildren, bool recursive, int maxChildDepth, int initialDepth, bool mapChildren)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var prepared = false;
            try
            {
                if (maxChildDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                if (initialDepth < 0) throw new ArgumentOutOfRangeException(nameof(initialDepth));
                if (source == null) throw new ArgumentNullException(nameof(source));
                var roots = source.ToList();
                foreach (var root in roots)
                {
                    var validation = ValidateMongoChildGraph(root);
                    if (validation != null) throw validation.Exception;
                }
                if (roots.Count == 0)
                {
                    result.Result = roots;
                    result.IsSaved = true;
                    return result;
                }
                var plan = HolonGraphWritePlan.Create(roots, saveChildren && mapChildren, recursive, maxChildDepth, initialDepth);
                // Preserve Mongo's embedded selected-child representation, but never alias caller nodes.
                var snapshots = plan.Nodes.ToDictionary(node => node.Source, node => node.Snapshot, new HolonReferenceComparer());
                foreach (var node in plan.Nodes)
                    node.Snapshot.Children = (node.Source.Children ?? Enumerable.Empty<IHolon>())
                        .Where(child => snapshots.ContainsKey(child) && snapshots[child].ParentHolonId == node.Snapshot.Id)
                        .Select(child => (IHolon)snapshots[child]).ToList();
                var entries = plan.Nodes.Select(node => DataHelper.ConvertOASISHolonToMongoEntity(node.Snapshot)).ToList();
                prepared = true;
                var rootIds = new HashSet<Guid>(roots.Select(root => snapshots[root].Id));
                var responses = new List<IHolon>();
                var saved = await _holonRepository.ImportAtomicAsync(entries, PreserveCreationAuditFields, () =>
                {
                    foreach (var entry in entries.Where(entry => rootIds.Contains(entry.HolonId)))
                    {
                        var response = DataHelper.ConvertMongoEntityToOASISHolon(new OASISResult<Entities.Holon> { Result = entry }, mapChildren);
                        if (response.IsError || response.Result == null)
                            throw response.Exception ?? new InvalidOperationException("Mongo graph response conversion failed.");
                        responses.Add(response.Result);
                    }
                }).ConfigureAwait(false);
                if (saved.IsError)
                {
                    result.IsError = true;
                    result.Message = saved.Message;
                    result.Exception = saved.Exception;
                    result.ErrorCode = saved.ErrorCode;
                    return result;
                }
                // A failed preparation/rollback/unknown commit must leave caller identity state unchanged.
                foreach (var node in plan.Nodes)
                {
                    node.Source.Id = node.Snapshot.Id;
                    node.Source.ParentHolonId = node.Snapshot.ParentHolonId;
                }
                result.Result = responses;
                result.IsSaved = true;
                result.SavedCount = saved.SavedCount;
                result.IsWarning = saved.IsWarning;
                result.WarningCount = saved.WarningCount;
                result.Exception = saved.Exception;
                result.Message = saved.Message;
            }
            catch (Exception error)
            {
                result.IsError = true;
                result.Exception = error;
                result.Message = prepared ? "Mongo graph operation failed." : "Mongo graph preparation failed.";
                if (!prepared) result.ErrorCode = "HYPERDRIVE_PAYLOAD_INVALID";
            }
            return result;
        }
    }
}
