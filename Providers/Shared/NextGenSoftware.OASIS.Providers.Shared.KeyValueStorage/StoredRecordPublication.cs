using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Validates caller identity before saved metadata is published. Does not undo acknowledged I/O.</summary>
public static class StoredRecordPublication
{
    public static void EnsureIdentityUnchanged(IHolonBase source, Guid originalId)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Id != originalId)
            throw new InvalidOperationException("Caller identity changed during the save; saved metadata was not published.");
    }
}
