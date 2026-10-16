using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
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
                var selected = new List<IHolon>();
                var identities = new HashSet<Guid>();
                var references = new HashSet<IHolon>(new HolonReferenceComparer());
                var pending = new Stack<(IHolon Node, int Depth)>();
                foreach (var root in roots.AsEnumerable().Reverse()) pending.Push((root, initialDepth));
                while (pending.Count != 0)
                {
                    var (node, depth) = pending.Pop();
                    if (!references.Add(node)) throw new ArgumentException("Bulk graph repeats a node.");
                    if (node.Id == Guid.Empty) node.Id = Guid.NewGuid();
                    if (!identities.Add(node.Id)) throw new ArgumentException("Bulk graph repeats a holon identity.");
                    selected.Add(node);
                    if (!saveChildren || !mapChildren || node.Children == null || (depth > initialDepth && !recursive)
                        || (maxChildDepth != 0 && depth >= maxChildDepth)) continue;
                    foreach (var child in node.Children)
                    {
                        if (child.ParentHolonId != Guid.Empty && child.ParentHolonId != node.Id)
                            throw new ArgumentException("Child parent identity conflicts with its graph parent.");
                        child.ParentHolonId = node.Id;
                        pending.Push((child, depth + 1));
                    }
                }
                // Map only after all selected identities and relationships are assigned.
                var entries = selected.Select(node => DataHelper.ConvertOASISHolonToMongoEntity(node)).ToList();
                prepared = true;
                var rootIds = new HashSet<Guid>(roots.Select(root => root.Id));
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
