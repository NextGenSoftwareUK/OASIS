using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
public partial class HolonRepository
{
    private OASISResult<IHolon> DeleteOrdinaryHolon(Guid? id,string key,bool soft)
    {
        Entities.HolonModel row=null;SqliteTransaction native=null;IDbContextTransaction enlisted=null;
        var opened=false;var commitEntered=false;var known=false;var rollbackFailed=false;var admitted=false;
        Exception failure=null;OASISResult<IHolon> result=null;
        void Record(Exception cause)=>failure=failure==null ? cause:new AggregateException(failure,cause);
        try
        {
            if(_dbContext.Database.CurrentTransaction!=null || _dbContext.ChangeTracker.HasChanges())
                throw new InvalidOperationException("An ordinary delete must own its transaction and cannot commit unrelated pending changes.");
            admitted=true;
            _dbContext.Database.OpenConnection();opened=true;
            var connection=(SqliteConnection)_dbContext.Database.GetDbConnection();
            native=connection.BeginTransaction(deferred:false);enlisted=_dbContext.Database.UseTransaction(native);
            row=id.HasValue ? _dbContext.Holons.AsNoTracking().SingleOrDefault(value=>value.Id==id.Value.ToString("D"))
                : _dbContext.Holons.AsNoTracking().SingleOrDefault(value=>value.LocalProviderKey==key);
            if(row==null)
                result=new OASISResult<IHolon>{IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Holon not found."};
            else
            {
                if(_versionControlEnabled())ArchiveHolonRevision(connection,native,row);
                // Read the persisted snapshot under the write reservation rather than reusing a stale tracked row.
                DetachOwnedFailedWrites(new[]{row.Id},true);
                if(soft)
                {
                    row.Version=checked(row.Version+1);row.IsActive=false;row.DeletedDate=DateTime.UtcNow;
                    row.ProviderRevisionId=Guid.NewGuid();row.PreviousVersionId=row.VersionId;row.VersionId=Guid.NewGuid();
                    _dbContext.Holons.Update(row);
                }
                else _dbContext.Holons.Remove(row);
                _dbContext.SaveChanges();
                var acknowledged=GetHolonFromEntity(row);
                commitEntered=true;enlisted.Commit();known=true;
                result=new OASISResult<IHolon>{IsDeleted=true,DeletedCount=1,Result=acknowledged,Message="Holon deleted successfully."};
            }
        }
        catch(Exception cause){Record(cause);}
        if(native!=null && !commitEntered)
        {try{native.Rollback();}catch(Exception cause){rollbackFailed=true;Record(cause);}}
        foreach(var resource in new IDisposable[]{enlisted,native})
        {try{resource?.Dispose();}catch(Exception cause){Record(cause);}}
        if(opened){try{_dbContext.Database.CloseConnection();}catch(Exception cause){Record(cause);}}
        if(failure!=null)
        {
            if(known)
            {result.IsWarning=true;result.WarningCount++;result.Exception=failure;result.Message="The delete committed, but SQLite cleanup failed.";}
            else
            {
                if(admitted)failure=HolonSaveFailure(row==null ? new():new(){row},commitEntered || rollbackFailed,failure,true);
                result=new OASISResult<IHolon>{IsError=true,ErrorCount=1,Exception=failure,
                    ErrorCode=commitEntered || rollbackFailed ? "SQLITE_WRITE_RECONCILIATION_REQUIRED":"SQLITE_DELETE_FAILED",
                    Message=commitEntered || rollbackFailed ? "Reconcile the original delete before another mutation.":"The delete did not commit."};
            }
        }
        return result;
    }
}
