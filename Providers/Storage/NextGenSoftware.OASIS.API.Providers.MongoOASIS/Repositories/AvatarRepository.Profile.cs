using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Providers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class AvatarRepository
{
    private sealed class ProfileRefusal(string code,string message):InvalidOperationException(message)
    { public string Code { get; }=code; }

    // One native unique index per public login identity. The partial predicate
    // excludes legacy missing/empty identities, not any valid nonempty identity.
    // The same indexes govern ordinary creates and saves, preventing write skew
    // between a profile change and a concurrent account registration.
    private static IEnumerable<CreateIndexModel<Avatar>> ProfileIdentityIndexes()
    {
        foreach(var field in new[]{nameof(Avatar.Username),nameof(Avatar.Email)})
            yield return new CreateIndexModel<Avatar>(new BsonDocument(field,1),new CreateIndexOptions<Avatar>
            {
                Name="oasis_avatar_"+field.ToLowerInvariant()+"_unique",Unique=true,
                Collation=Collation.Simple,
                PartialFilterExpression=new BsonDocument(field,new BsonDocument{{"$type","string"},{"$gt",""}})
            });
    }

    private static void RequireProfileRevision(HolonBase record,AvatarProfileRevision expected)
    {
        if(record.Version!=expected.Version || record.VersionId!=expected.VersionId)
            throw new ProfileRefusal(AvatarProfileErrors.Conflict,"The profile changed. Reload it before saving.");
    }

    private static OASISResult<AvatarProfileSnapshot> ProfileFailure(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};

    public async Task<OASISResult<AvatarProfileSnapshot>> ApplyAvatarProfileAsync(Guid authenticatedAvatarId,
        AvatarProfileMutation mutation,CancellationToken cancellationToken=default)
    {
        if(mutation==null)return ProfileFailure(AvatarProfileErrors.Invalid,"A profile edit is required.");
        if(authenticatedAvatarId==Guid.Empty || authenticatedAvatarId!=mutation.AvatarId)
            return ProfileFailure(AvatarProfileErrors.Forbidden,"An avatar may edit only its own profile.");
        var result=new OASISResult<AvatarProfileSnapshot>();
        try
        {
            using var session=await _dbContext.MongoClient.StartSessionAsync(cancellationToken:cancellationToken).ConfigureAwait(false);
            session.StartTransaction(new TransactionOptions(readConcern:ReadConcern.Snapshot,writeConcern:WriteConcern.WMajority));
            var commitAttempted=false;
            try
            {
                var retainHistory=_retainAccountHistory();
                var avatars=await _dbContext.Avatar.Find(session,row=>row.HolonId==mutation.AvatarId).Limit(2).ToListAsync(cancellationToken).ConfigureAwait(false);
                var details=await _dbContext.AvatarDetail.Find(session,row=>row.HolonId==mutation.AvatarId).Limit(2).ToListAsync(cancellationToken).ConfigureAwait(false);
                if(avatars.Count>1 || details.Count>1)throw new StorageIdentityAmbiguousException();
                if(avatars.Count!=1 || details.Count!=1)
                    throw new ProfileRefusal(AvatarProfileErrors.NotFound,"Both existing profile records are required.");
                var avatar=avatars[0];var detail=details[0];
                if(!avatar.IsActive || avatar.IsDeleted || detail.IsDeleted)
                    throw new ProfileRefusal(AvatarProfileErrors.Forbidden,"The account is not active.");
                RequireProfileRevision(avatar,mutation.AvatarRevision);
                RequireProfileRevision(detail,mutation.DetailRevision);
                var accountFields=new List<UpdateDefinition<Avatar>>();
                var detailFields=new List<UpdateDefinition<AvatarDetail>>();
                var accountUpdate=Builders<Avatar>.Update;
                var detailUpdate=Builders<AvatarDetail>.Update;
                var nameChanged=false;
                foreach(var field in mutation.Fields)
                {
                    switch(field.Key)
                    {
                        case AvatarProfileProperty.Title:
                            if(avatar.Title!=field.Value)accountFields.Add(accountUpdate.Set(row=>row.Title,field.Value));break;
                        case AvatarProfileProperty.FirstName:
                            if(avatar.FirstName!=field.Value){accountFields.Add(accountUpdate.Set(row=>row.FirstName,field.Value));nameChanged=true;}break;
                        case AvatarProfileProperty.LastName:
                            if(avatar.LastName!=field.Value){accountFields.Add(accountUpdate.Set(row=>row.LastName,field.Value));nameChanged=true;}break;
                        case AvatarProfileProperty.Username:
                            if(avatar.Username!=field.Value)accountFields.Add(accountUpdate.Set(row=>row.Username,field.Value));
                            if(detail.Username!=field.Value)detailFields.Add(detailUpdate.Set(row=>row.Username,field.Value));break;
                        case AvatarProfileProperty.Email:
                            if(avatar.Email!=field.Value)accountFields.Add(accountUpdate.Set(row=>row.Email,field.Value));
                            if(detail.Email!=field.Value)detailFields.Add(detailUpdate.Set(row=>row.Email,field.Value));break;
                        case AvatarProfileProperty.Address:
                            if(detail.Address!=field.Value)detailFields.Add(detailUpdate.Set(row=>row.Address,field.Value));break;
                        case AvatarProfileProperty.Portrait:
                            if(detail.Portrait!=field.Value)detailFields.Add(detailUpdate.Set(row=>row.Portrait,field.Value));break;
                        default:throw new ProfileRefusal(AvatarProfileErrors.Invalid,"Only editable profile fields are accepted.");
                    }
                }
                if(nameChanged)
                {
                    var name=new Core.Holons.Avatar
                    {
                        FirstName=mutation.Fields.TryGetValue(AvatarProfileProperty.FirstName,out var first)?first:avatar.FirstName,
                        LastName=mutation.Fields.TryGetValue(AvatarProfileProperty.LastName,out var last)?last:avatar.LastName
                    }.FullName;
                    accountFields.Add(accountUpdate.Set(row=>row.Name,name));
                }
                var modified=mutation.ModifiedAtUtc;
                if(accountFields.Count>0)
                {
                    accountFields.Add(accountUpdate.Set(row=>row.ModifiedDate,modified)
                        .Set(row=>row.ModifiedDateExactUtcTicks,MongoAuditDatePrecision.ExactTicks(modified))
                        .Set(row=>row.ModifiedByAvatarId,authenticatedAvatarId.ToString("D")));
                    await PatchAccountLifecycleAsync(session,_dbContext.Avatar,avatar.Id,accountUpdate.Combine(accountFields),
                        retainHistory,false,advanceVersion:true,cancellationToken:cancellationToken,nextVersionId:mutation.AvatarNextVersionId).ConfigureAwait(false);
                }
                if(detailFields.Count>0)
                {
                    detailFields.Add(detailUpdate.Set(row=>row.ModifiedDate,modified)
                        .Set(row=>row.ModifiedDateExactUtcTicks,MongoAuditDatePrecision.ExactTicks(modified))
                        .Set(row=>row.ModifiedByAvatarId,authenticatedAvatarId.ToString("D")));
                    await PatchAccountLifecycleAsync(session,_dbContext.AvatarDetail,detail.Id,detailUpdate.Combine(detailFields),
                        retainHistory,false,advanceVersion:true,cancellationToken:cancellationToken,nextVersionId:mutation.DetailNextVersionId).ConfigureAwait(false);
                }
                // Read the canonical proposal inside the same snapshot. No caller
                // snapshot or game/security/session field is ever copied back.
                avatar=await _dbContext.Avatar.Find(session,row=>row.HolonId==mutation.AvatarId).SingleAsync(cancellationToken).ConfigureAwait(false);
                detail=await _dbContext.AvatarDetail.Find(session,row=>row.HolonId==mutation.AvatarId).SingleAsync(cancellationToken).ConfigureAwait(false);
                if(avatar.Username!=detail.Username || avatar.Email!=detail.Email)
                    throw new ProfileRefusal(AvatarProfileErrors.Conflict,"The account profile identities are inconsistent.");
                var snapshot=new AvatarProfileSnapshot(avatar.HolonId,avatar.Title,avatar.FirstName,avatar.LastName,
                    avatar.Username,avatar.Email,detail.Address,new(avatar.Version,avatar.VersionId),new(detail.Version,detail.VersionId));
                var changed=accountFields.Count>0 || detailFields.Count>0;
                commitAttempted=changed;
                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
                result.Result=snapshot;result.IsSaved=changed;
                result.SavedCount=(accountFields.Count>0?1:0)+(detailFields.Count>0?1:0);
            }
            catch(Exception cause)
            {
                var code=AvatarProfileErrors.StorageUnavailable;
                Exception evidence=cause;
                if(commitAttempted)
                {
                    code=AvatarProfileErrors.CommitUnknown;
                    evidence=new MongoTransactionReconciliationException(session.ServerSession.Id.ToJson(),
                        session.WrappedCoreSession.CurrentTransaction.TransactionNumber,cause);
                }
                else if(cause is ProfileRefusal refusal)code=refusal.Code;
                else if(cause is StorageIdentityAmbiguousException)code=OASISResultErrorCodes.AmbiguousIdentity;
                else if(cause is MongoWriteException write && write.WriteError?.Category==ServerErrorCategory.DuplicateKey
                    || cause is MongoCommandException command && command.Code==11000)code=AvatarProfileErrors.IdentityConflict;
                else if(MongoTransactionFailure.IsWriteConflict(cause))code=AvatarProfileErrors.Conflict;
                result=ProfileFailure(code,"The profile edit was not acknowledged.",evidence);
                if(session.IsInTransaction)
                {
                    try{await session.AbortTransactionAsync(CancellationToken.None).ConfigureAwait(false);}
                    catch(Exception cleanup){result.Exception=new AggregateException(evidence,cleanup);}
                }
            }
        }
        catch(Exception cause)
        {
            var evidence=result.Exception==null?cause:new AggregateException(result.Exception,cause);
            result=ProfileFailure(result.ErrorCode??AvatarProfileErrors.StorageUnavailable,"The profile storage session failed.",evidence);
        }
        return result;
    }
}
