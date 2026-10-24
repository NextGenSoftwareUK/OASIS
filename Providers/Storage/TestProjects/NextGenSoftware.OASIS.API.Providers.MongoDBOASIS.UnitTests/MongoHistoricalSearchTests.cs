using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoHistoricalSearchTests
{
    [DataTestMethod][DataRow(false,1)][DataRow(false,2)][DataRow(false,3)][DataRow(true,1)][DataRow(true,2)][DataRow(true,3)]
    public void SelectionPrecedesPredicatesAndPreservesNativeTypesWithoutChangingEvidence(bool avatar,int version)
    {
        var document=History(avatar);var before=document.ToBson();
        var selected=SearchRepository.SelectSearchVersion(document,version,avatar);
        CollectionAssert.AreEqual(before,document.ToBson());
        if(version==3){Assert.IsNull(selected);return;}
        Assert.AreEqual(version,selected["Version"].AsInt32);Assert.IsFalse(selected.Contains(MongoNativeHolonHistory.Field));
        Assert.AreEqual(version==1?"old":"new",selected[avatar?"Username":"Name"].AsString);
        Assert.IsTrue(selected["MetaData"]["date"].IsBsonDateTime);Assert.IsTrue(selected["MetaData"]["dateText"].IsString);
        selected["MetaData"]["dateText"]="detached";CollectionAssert.AreEqual(before,document.ToBson());
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void ArchiveTamperingCannotBecomeSearchResults(bool avatar)
    {
        var document=History(avatar);document[MongoNativeHolonHistory.Field]["records"][0]["sha256"]=new string('0',64);
        Assert.ThrowsException<InvalidDataException>(()=>SearchRepository.SelectSearchVersion(document,1,avatar));
    }
    private static BsonDocument History(bool avatar)
    {
        _=new MongoDBOASIS("mongodb://127.0.0.1:1","search-history-no-io");var id=Guid.NewGuid();var physical=ObjectId.GenerateNewId();
        BsonDocument Document(int version,string name){
            Entities.HolonBase native=avatar?DataHelper.ConvertOASISAvatarToMongoEntity(new Avatar{Id=id,Version=version,Username=name})
                :DataHelper.ConvertOASISHolonToMongoEntity(new Holon{Id=id,Version=version,Name=name});
            native.Id=physical.ToString();var result=native.ToBsonDocument(native.GetType());
            result["MetaData"]=new BsonDocument{{"date",new BsonDateTime(new DateTime(2026,10,24,0,0,0,DateTimeKind.Utc))},{"dateText","2026-10-24T00:00:00Z"}};return result;
        }
        var old=Document(1,"old");var current=Document(2,"new");
        if(avatar)MongoNativeAccountHistory.StageAvatar(current,old);else MongoNativeHolonHistory.Stage(current,old);
        return current;
    }
}
