using System;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static void ValidateCurrentAvatarVersion(int version)
    {
        if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
        if(version>0) throw new NotSupportedException("Current-record reader requires version 0; positive versions must use the historical reader.");
    }

    private async Task<OASISResult<IAvatar>> ReadOneAvatarAsync(string field,string value,int version)
    {
        if(version>0)return await ReadHistoricalAvatarAsync(field,value,version);
        var result=new OASISResult<IAvatar>();
        try
        {
            ValidateCurrentAvatarVersion(version);
            var query=AvatarSelectionQuery(field);
            if(string.IsNullOrWhiteSpace(value) || field=="Id" && (!Guid.TryParse(value,out var id) || id==Guid.Empty))
                throw new ArgumentException("A nonempty avatar lookup identity is required.");
            if(!IsProviderActivated || _driver==null) throw new InvalidOperationException("Neo4j provider is not activated.");
            using var session=_driver.AsyncSession();
            var rows=await (await session.RunAsync(query,new {value})).ToListAsync();
            if(rows.Count==0) return new OASISResult<IAvatar> {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Avatar not found in Neo4j."};
            if(rows.Count!=1) throw new StorageIdentityAmbiguousException();
            result.Result=ReadAvatarNode(rows[0]["a"].As<Neo4j.Driver.INode>());result.IsLoaded=true;
        }
        catch(Exception cause)
        {
            result.IsError=true;result.Exception=cause;result.Message=cause.Message;
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
        }
        return result;
    }
}
