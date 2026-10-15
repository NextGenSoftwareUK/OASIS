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
            var result = new OASISResult<IHolon>();
            var prepared = false;
            try
            {
                if (maxChildDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                var selected = new List<IHolon>();
                var pending = new Stack<(IHolon Node, int Depth)>();
                pending.Push((root, 0));
                while (pending.Count != 0)
                {
                    var (node, depth) = pending.Pop();
                    if (node.Id == Guid.Empty) node.Id = Guid.NewGuid();
                    selected.Add(node);
                    if (node.Children == null || (depth > 0 && !recursive)
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
                OASISResult<IHolon> response = null;
                var saved = await _holonRepository.ImportAtomicAsync(entries, PreserveCreationAuditFields, () =>
                {
                    response = DataHelper.ConvertMongoEntityToOASISHolon(new OASISResult<Entities.Holon> { Result = entries[0] }, true);
                    if (response.IsError || response.Result == null)
                        throw response.Exception ?? new InvalidOperationException("Mongo graph response conversion failed.");
                });
                if (saved.IsError)
                {
                    result.IsError = true;
                    result.Message = saved.Message;
                    result.Exception = saved.Exception;
                    result.ErrorCode = saved.ErrorCode;
                    return result;
                }
                result.Result = response.Result;
                result.IsSaved = true;
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
