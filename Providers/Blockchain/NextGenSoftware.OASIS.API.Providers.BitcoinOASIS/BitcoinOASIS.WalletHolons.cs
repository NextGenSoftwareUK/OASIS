using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private async Task<OASISResult<IHolon>> LoadWalletHolonAsync(
        Guid id, bool loadChildren, bool recursive, int maxDepth, int version)
    {
        var matches = await LoadWalletHolonsAsync(holon => holon.Id == id, loadChildren, recursive, maxDepth, 0, version);
        var result = new OASISResult<IHolon>();
        if (matches.IsError)
        {
            OASISErrorHandling.HandleError(ref result, matches.Message, matches.Exception);
            return result;
        }

        result.Result = matches.Result.SingleOrDefault();
        if (result.Result == null)
        {
            result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
            OASISErrorHandling.HandleError(ref result, "The Bitcoin wallet has no matching holon.");
        }

        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> LoadWalletHolonsAsync(
        Func<IHolon, bool> select, bool loadChildren, bool recursive, int maxDepth, int currentDepth, int version)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            var records = await ReadWalletRecordsAsync<IHolon>(DecodeHolonTransactionAsync, version);
            var selected = records.Where(select).ToArray();
            if (loadChildren)
            {
                var children = records.Where(holon => holon.ParentHolonId != Guid.Empty)
                    .ToLookup(holon => holon.ParentHolonId);
                foreach (var root in selected)
                    AttachChildren(root, children, new HashSet<Guid>(), recursive, maxDepth, currentDepth);
            }

            result.Result = selected;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Bitcoin wallet holon query failed ({ex.GetType().Name}).");
        }

        return result;
    }

    private static void AttachChildren(IHolon parent, ILookup<Guid, IHolon> children,
        HashSet<Guid> ancestors, bool recursive, int maxDepth, int currentDepth)
    {
        if (!ancestors.Add(parent.Id))
            throw new InvalidOperationException("Bitcoin holon history contains a parent cycle.");

        parent.Children = maxDepth > 0 && currentDepth >= maxDepth
            ? new List<IHolon>()
            : children[parent.Id].ToList();
        foreach (var child in parent.Children)
        {
            if (ancestors.Contains(child.Id))
                throw new InvalidOperationException("Bitcoin holon history contains a parent cycle.");
            if (recursive)
                AttachChildren(child, children, ancestors, recursive, maxDepth, currentDepth + 1);
        }
        ancestors.Remove(parent.Id);
    }
}
