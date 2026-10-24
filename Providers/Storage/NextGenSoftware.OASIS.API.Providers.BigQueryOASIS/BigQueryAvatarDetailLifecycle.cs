using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.BigQuery.V2;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.BigQueryOASIS;

public partial class BigQueryOASIS
{
    public Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id,bool softDelete=true)
        => DeleteDetailRecordAsync("Id",id.ToString(),softDelete);
    public OASISResult<bool> DeleteAvatarDetail(Guid id,bool softDelete=true)
        => DeleteAvatarDetailAsync(id,softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string username,bool softDelete=true)
        => DeleteDetailRecordAsync("Username",username,softDelete);
    public OASISResult<bool> DeleteAvatarDetailByUsername(string username,bool softDelete=true)
        => DeleteAvatarDetailByUsernameAsync(username,softDelete).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email,bool softDelete=true)
        => DeleteDetailRecordAsync("Email",email,softDelete);
    public OASISResult<bool> DeleteAvatarDetailByEmail(string email,bool softDelete=true)
        => DeleteAvatarDetailByEmailAsync(email,softDelete).GetAwaiter().GetResult();

    private async Task<OASISResult<bool>> DeleteDetailRecordAsync(string field,string value,bool soft)
    {
        var result=new OASISResult<bool>();
        try
        {
            if(field=="Id")ValidatePointIdentity(Guid.Parse(value));
            else if(string.IsNullOrWhiteSpace(value))throw new ArgumentException("AvatarDetail identity selector must not be blank.",nameof(value));
            var column=field=="Id"?"id":FieldColumn(field);var parameter=field=="Id"?"id":"value";string? original=null;
            var rows=await ReadRowsAsync<AvatarDetail>($"SELECT doc FROM {Table(CollAvatarDetails)} WHERE {column} = @{parameter} LIMIT 2",new[]{StringParameter(parameter,value)},(_,raw)=>original=raw);
            ValidateIndexedSelection(rows,field,value);
            var current=UniqueRecord(rows)??throw new KeyNotFoundException("BigQuery AvatarDetail was not found.");
            if(soft)
            {
                if(current.IsDeleted&&!current.IsActive){result.Result=true;result.IsDeleted=true;result.DeletedCount=1;return result;}
                current.IsActive=false;if(!current.IsDeleted)current.DeletedDate=DateTime.UtcNow;
                var written=await SaveAcknowledgedRecordAsync(current,CollAvatarDetails,observationProvided:true,observedRaw:original);
                result.SavedCount=written.SavedCount;
                if(written.IsError||!written.IsSaved)throw written.Exception??new System.IO.InvalidDataException("BigQuery AvatarDetail soft delete received no complete acknowledgement.");
                result.IsSaved=true;
            }
            else await DeleteDocAsync(CollAvatarDetails,current.Id.ToString(),expectedDoc:original);
            result.Result=true;result.IsDeleted=true;result.DeletedCount=1;
        }
        catch(Exception cause)
        {
            OASISErrorHandling.HandleError(ref result,"BigQuery AvatarDetail deletion failed; inspect the original outcome before replay.",cause);
            if(cause is KeyNotFoundException)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
        }
        return result;
    }
}
