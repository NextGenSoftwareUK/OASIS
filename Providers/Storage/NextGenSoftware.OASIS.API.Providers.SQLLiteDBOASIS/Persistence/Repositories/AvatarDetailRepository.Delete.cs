using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public partial class AvatarDetailRepository
    {
        public OASISResult<bool> DeleteAvatarDetail(Guid id, bool softDelete = true)
        {
            var affected = new HashSet<object>();
            var persistenceEntered = false;
            try
            {
                using var transaction = _dbContext.Database.BeginTransaction();
                var detail = _dbContext.AvatarDetails.SingleOrDefault(d => d.Id == id.ToString());
                if (detail == null) return MissingDetail();
                affected.Add(detail);
                if (softDelete) { detail.IsActive = false; detail.DeletedDate = DateTime.UtcNow; }
                else
                {
                    new AvatarRepository(_dbContext).RemoveAvatarDetailDependents(detail, affected);
                    _dbContext.Remove(detail);
                }
                var commitEntered = false;
                try
                {
                    persistenceEntered = true;
                    _dbContext.SaveChanges();
                    commitEntered = true;
                    transaction.Commit();
                }
                catch (Exception cause)
                {
                    if (commitEntered) throw; // Do not attempt to undo an outcome whose commit may already have completed.
                    try
                    {
                        transaction.Rollback();
                        persistenceEntered = false;
                    }
                    catch (Exception rollback) { throw new AggregateException("SQLite detail deletion and rollback failed.", cause, rollback); }
                    throw;
                }
                return new OASISResult<bool> { Result = true, IsDeleted = true };
            }
            catch (Exception exception)
            {
                exception = DetailDeletionFailure(id, affected, persistenceEntered, exception);
                return new OASISResult<bool> { IsError = true, Exception = exception, Message = exception.Message };
            }
        }

        public async Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id, bool softDelete = true)
        {
            var affected = new HashSet<object>();
            var persistenceEntered = false;
            try
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                var detail = await _dbContext.AvatarDetails.SingleOrDefaultAsync(d => d.Id == id.ToString());
                if (detail == null) return MissingDetail();
                affected.Add(detail);
                if (softDelete) { detail.IsActive = false; detail.DeletedDate = DateTime.UtcNow; }
                else
                {
                    await new AvatarRepository(_dbContext).RemoveAvatarDetailDependentsAsync(detail, affected);
                    _dbContext.Remove(detail);
                }
                var commitEntered = false;
                try
                {
                    persistenceEntered = true;
                    await _dbContext.SaveChangesAsync();
                    commitEntered = true;
                    await transaction.CommitAsync();
                }
                catch (Exception cause)
                {
                    if (commitEntered) throw; // Do not attempt to undo an outcome whose commit may already have completed.
                    try
                    {
                        await transaction.RollbackAsync();
                        persistenceEntered = false;
                    }
                    catch (Exception rollback) { throw new AggregateException("SQLite detail deletion and rollback failed.", cause, rollback); }
                    throw;
                }
                return new OASISResult<bool> { Result = true, IsDeleted = true };
            }
            catch (Exception exception)
            {
                exception = DetailDeletionFailure(id, affected, persistenceEntered, exception);
                return new OASISResult<bool> { IsError = true, Exception = exception, Message = exception.Message };
            }
        }

        private Exception DetailDeletionFailure(Guid id, HashSet<object> affected, bool persistenceEntered, Exception cause)
        {
            try { DetachRejectedDetailDeletion(affected); }
            catch (Exception cleanup) { cause = new AggregateException("SQLite failure cleanup also failed.", cause, cleanup); }
            return persistenceEntered ? new SqliteMutationReconciliationException(id, cause) : cause;
        }

        private static OASISResult<bool> MissingDetail() => new OASISResult<bool> {
            IsError = true, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Avatar detail not found." };

        private void DetachRejectedDetailDeletion(HashSet<object> affected)
        {
            foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
                if (affected.Contains(entry.Entity)) entry.State = EntityState.Detached;
        }
    }
}
