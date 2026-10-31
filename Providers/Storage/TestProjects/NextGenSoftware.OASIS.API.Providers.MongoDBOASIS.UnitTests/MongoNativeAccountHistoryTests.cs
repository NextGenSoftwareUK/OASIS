using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoNativeAccountHistoryTests
{
    private static void PreparePolicy(bool detail,BsonDocument next,BsonDocument old,bool retain)
    {
        if(detail)MongoNativeRecordWriter.PrepareAccountHistory<Entities.AvatarDetail>(next,old,retain);
        else MongoNativeRecordWriter.PrepareAccountHistory<Entities.Avatar>(next,old,retain);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void RetentionPolicyIsExplicitAndDisabledModeDoesNotAppend(bool detail)
    {
        var id=Guid.NewGuid();var old=Document(detail,id,1,"old");var next=Document(detail,id,2,"new");next["_id"]=old["_id"];
        PreparePolicy(detail,next,old,false);Assert.IsFalse(next.Contains(MongoNativeHolonHistory.Field));
        PreparePolicy(detail,next,old,true);Assert.AreEqual(1,next[MongoNativeHolonHistory.Field]["records"].AsBsonArray.Count);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void RetentionRejectsAmbiguousSameVersionBeforeDispatch(bool detail)
    {
        var id=Guid.NewGuid();var old=Document(detail,id,1,"old");var next=Document(detail,id,1,"changed");next["_id"]=old["_id"];var before=old.ToBson();
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>PreparePolicy(detail,next,old,true));CollectionAssert.AreEqual(before,old.ToBson());
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void DisablingRetentionDoesNotAuthorizeCorruptExistingHistory(bool detail)
    {
        var id=Guid.NewGuid();var old=Document(detail,id,1,"old");var prior=Document(detail,id,0,"prior");prior["_id"]=old["_id"];Stage(detail,old,prior);
        var next=Document(detail,id,2,"new");next["_id"]=old["_id"];next[MongoNativeHolonHistory.Field]=old[MongoNativeHolonHistory.Field].DeepClone();
        next[MongoNativeHolonHistory.Field]["records"][0]["sha256"]=new string('0',64);
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>PreparePolicy(detail,next,old,false));
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void IdenticalRetainedSaveDoesNotGrowArchive(bool detail)
    {
        var id=Guid.NewGuid();var old=Document(detail,id,1,"old");var next=Document(detail,id,2,"new");next["_id"]=old["_id"];PreparePolicy(detail,next,old,true);
        var retry=next.DeepClone().AsBsonDocument;PreparePolicy(detail,retry,next,true);CollectionAssert.AreEqual(next.ToBson(),retry.ToBson());
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void DisabledAppendCannotMoveAnExistingArchiveBackwards(bool detail)
    {
        var id=Guid.NewGuid();var old=Document(detail,id,3,"old");var prior=Document(detail,id,1,"prior");prior["_id"]=old["_id"];Stage(detail,old,prior);
        var next=Document(detail,id,2,"backwards");next["_id"]=old["_id"];next[MongoNativeHolonHistory.Field]=old[MongoNativeHolonHistory.Field].DeepClone();
        Assert.ThrowsException<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(()=>PreparePolicy(detail,next,old,false));
    }

    private static BsonDocument Document(bool detail,Guid id,int version,string username)
    {
        _=new MongoDBOASIS("mongodb://127.0.0.1:1","account-history-no-io");
        Entities.HolonBase value=detail
            ?DataHelper.ConvertOASISAvatarDetailToMongoEntity(new AvatarDetail {Id=id,Version=version,Username=username})
            :DataHelper.ConvertOASISAvatarToMongoEntity(new Avatar {Id=id,Version=version,Username=username,Password="synthetic-password"});
        value.Id=ObjectId.GenerateNewId().ToString();return value.ToBsonDocument(value.GetType());
    }
    private static void Stage(bool detail,BsonDocument next,BsonDocument old)
    {if(detail)MongoNativeAccountHistory.StageDetail(next,old);else MongoNativeAccountHistory.StageAvatar(next,old);}

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void SharedArchivePreservesIndependentAccountPayloadAndObservedBson(bool detail)
    {
        var id=Guid.NewGuid();var old=Document(detail,id,1,"old-user");
        old["unknown-native-field"]=new BsonDocument("date",new BsonDateTime(DateTime.UtcNow));
        var bytes=old.ToBson();var next=Document(detail,id,2,"new-user");next["_id"]=old["_id"];Stage(detail,next,old);
        CollectionAssert.AreEqual(bytes,old.ToBson());
        CollectionAssert.AreEqual(bytes,next[MongoNativeHolonHistory.Field]["records"][0]["bson"].AsBsonDocument.ToBson());
        if(detail){var rows=MongoNativeAccountHistory.ReadDetail(next);Assert.AreEqual("old-user",rows[0].Username);Assert.AreEqual("new-user",rows[1].Username);}
        else{var rows=MongoNativeAccountHistory.ReadAvatar(next);Assert.AreEqual("old-user",rows[0].Username);Assert.AreEqual("synthetic-password",rows[0].Password);Assert.AreEqual("new-user",rows[1].Username);}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void TamperedAccountArchiveRejectsRatherThanReturningCurrentOnly(bool detail)
    {
        var id=Guid.NewGuid();var next=Document(detail,id,2,"new");var old=Document(detail,id,1,"old");next["_id"]=old["_id"];Stage(detail,next,old);
        next[MongoNativeHolonHistory.Field]["records"][0]["sha256"]=new string('0',64);
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>{if(detail)MongoNativeAccountHistory.ReadDetail(next);else MongoNativeAccountHistory.ReadAvatar(next);});
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void BackwardsAccountVersionDoesNotMutateReplacement(bool detail)
    {
        var id=Guid.NewGuid();var next=Document(detail,id,1,"new");var old=Document(detail,id,2,"old");next["_id"]=old["_id"];var before=next.ToBson();
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>Stage(detail,next,old));
        CollectionAssert.AreEqual(before,next.ToBson());
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void SameVersionWithChangedPrivateOrGameplayContentIsAmbiguous(bool detail)
    {
        var id=Guid.NewGuid();var old=Document(detail,id,1,"same-user");
        var next=old.DeepClone().AsBsonDocument;
        if(detail)next["Karma"]=new BsonInt64(123);
        else next["Password"]="different-synthetic-password";
        Stage(detail,next,old);
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>{if(detail)MongoNativeAccountHistory.ReadDetail(next);else MongoNativeAccountHistory.ReadAvatar(next);});
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void DifferentPhysicalIdentityCannotBeStagedForSamePublicAccount(bool detail)
    {
        var id=Guid.NewGuid();var old=Document(detail,id,1,"old");var next=Document(detail,id,2,"new");var before=next.ToBson();
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>Stage(detail,next,old));
        CollectionAssert.AreEqual(before,next.ToBson());
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void ChecksumValidArchiveStillCannotChangePhysicalIdentity(bool detail)
    {
        var id=Guid.NewGuid();var old=Document(detail,id,1,"old");var next=Document(detail,id,2,"new");next["_id"]=old["_id"];Stage(detail,next,old);
        var snapshot=next[MongoNativeHolonHistory.Field]["records"][0]["bson"].AsBsonDocument;snapshot["_id"]=ObjectId.GenerateNewId();
        next[MongoNativeHolonHistory.Field]["records"][0]["sha256"]=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(snapshot.ToBson()));
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>{if(detail)MongoNativeAccountHistory.ReadDetail(next);else MongoNativeAccountHistory.ReadAvatar(next);});
    }
}
