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
                _dbContext.SaveChanges();
                transaction.Commit();
                return new OASISResult<bool> { Result = true, IsDeleted = true };
            }
            catch (Exception exception)
            {
                DetachRejectedDetailDeletion(affected);
                return new OASISResult<bool> { IsError = true, Exception = exception, Message = exception.Message };
            }
        }

        public async Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id, bool softDelete = true)
        {
            var affected = new HashSet<object>();
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
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                return new OASISResult<bool> { Result = true, IsDeleted = true };
            }
            catch (Exception exception)
            {
                DetachRejectedDetailDeletion(affected);
                return new OASISResult<bool> { IsError = true, Exception = exception, Message = exception.Message };
            }
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
