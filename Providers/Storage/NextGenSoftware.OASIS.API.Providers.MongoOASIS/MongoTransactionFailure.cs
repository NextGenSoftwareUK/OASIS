using System;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal static class MongoTransactionFailure
{
    // Only use before commit dispatch in an explicit transaction. This is a native
    // concurrency rejection, not an outage or permission to overwrite another store.
    internal static bool IsWriteConflict(Exception cause)
        => cause is MongoCommandException command && command.Code==112
            || cause is MongoWriteException write && write.WriteError?.Code==112 && write.WriteConcernError==null;
}
