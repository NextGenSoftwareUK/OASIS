using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public partial class HolonRepository
    {
        internal async Task<OASISResult<bool>> ImportAtomicAsync(IEnumerable<Holon> holons, Action<Holon, Holon> preserveAudit, Action prepareResponse = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                var entries = holons.ToList();
                if (entries.Any(holon => holon == null || holon.HolonId == Guid.Empty)
                    || entries.GroupBy(holon => holon.HolonId).Any(group => group.Count() > 1))
                    throw new ArgumentException("Import requires distinct nonempty holon identities.");
                using var session = await _dbContext.MongoClient.StartSessionAsync();
                session.StartTransaction();
                var commitAttempted = false;
                try
                {
                    foreach (var holon in entries)
                    {
                        var matches = await _dbContext.Holon.Find(session, existing => existing.HolonId == holon.HolonId)
                            .Limit(2).ToListAsync();
                        if (matches.Count > 1) throw new StorageIdentityAmbiguousException();
                        var existing = matches.SingleOrDefault();
                        preserveAudit(holon, existing);
                        if (existing == null && holon.CreatedProviderType == null)
                            holon.CreatedProviderType = new NextGenSoftware.Utilities.EnumValue<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.MongoDBOASIS);
                        PrepareNativeHolonIdentity(holon);
                        if (existing == null) await _dbContext.Holon.InsertOneAsync(session, holon);
                        else
                        {
                            var replaced = await _dbContext.Holon.ReplaceOneAsync(session, item => item.Id == existing.Id, holon);
                            if (!replaced.IsAcknowledged || replaced.MatchedCount != 1)
                                throw new InvalidOperationException("Imported replacement was not acknowledged.");
                        }
                    }
                    // Response conversion is part of preparation: failure must still abort,
                    // not turn an already committed write into an apparently replayable error.
                    prepareResponse?.Invoke();
                    commitAttempted = true;
                    await session.CommitTransactionAsync();
                    result.Result = true;
                    result.IsSaved = true;
                }
                catch (Exception error)
                {
                    result.IsError = true;
                    result.Exception = commitAttempted
                        ? new MongoTransactionReconciliationException(session.ServerSession.Id.ToJson(), session.WrappedCoreSession.CurrentTransaction.TransactionNumber, error)
                        : error;
                    result.Message = "Mongo holon import transaction failed.";
                    if (error is StorageIdentityAmbiguousException) result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity;
                    try { if (session.IsInTransaction) await session.AbortTransactionAsync(); }
                    catch (Exception abortError) { result.Exception = new AggregateException(result.Exception, abortError); }
                }
            }
            catch (Exception error)
            {
                result.IsError = true; result.Exception = error; result.Message = "Mongo holon import preparation failed.";
            }
            return result;
        }
    }
}
