using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Security.Cryptography;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoNativeHolonHistoryTests
{
    private static BsonDocument Document(Guid id,int version,string name)
    {
        _=new MongoDBOASIS("mongodb://127.0.0.1:1","history-codec-no-io");
        var record=DataHelper.ConvertOASISHolonToMongoEntity(new Holon{Id=id,Version=version,VersionId=Guid.NewGuid(),Name=name});
        record.Id=ObjectId.GenerateNewId().ToString();
        return record.ToBsonDocument();
    }

    [TestMethod]
    public void StageRetainsDetachedCanonicalVersionsWithoutChangingObservedBson()
    {
        var id=Guid.NewGuid();var first=Document(id,1,"original");var before=first.ToJson();var second=Document(id,2,"replacement");
        MongoNativeHolonHistory.Stage(second,first);
        var versions=MongoNativeHolonHistory.Read(second);
        CollectionAssert.AreEqual(new[]{1,2},versions.Select(row=>row.Version).ToArray());
        Assert.AreEqual("original",versions[0].Name);Assert.AreEqual(before,first.ToJson());
    }

    [TestMethod]
    public void ModifiedArchiveChecksumIsRejected()
    {
        var id=Guid.NewGuid();var second=Document(id,2,"replacement");MongoNativeHolonHistory.Stage(second,Document(id,1,"original"));
        second[MongoNativeHolonHistory.Field]["records"][0]["sha256"]=new string('0',64);
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>MongoNativeHolonHistory.Read(second));
    }

    [TestMethod]
    public void ArchivePreservesNativeDateWithoutGuessingDateLookingString()
    {
        var id=Guid.NewGuid();var observed=Document(id,1,"original");
        observed["MetaData"]=new BsonDocument{{"date",new BsonDateTime(new DateTime(2026,10,24,0,0,0,DateTimeKind.Utc))},{"dateText","2026-10-24T00:00:00Z"}};
        var next=Document(id,2,"replacement");MongoNativeHolonHistory.Stage(next,observed);
        var old=MongoNativeHolonHistory.Read(next).Single(row=>row.Version==1);
        Assert.IsInstanceOfType(old.MetaData["date"],typeof(DateTime));
        Assert.IsInstanceOfType(old.MetaData["dateText"],typeof(string));
        Assert.AreEqual("2026-10-24T00:00:00Z",old.MetaData["dateText"]);
    }

    [TestMethod]
    public void NativeSnapshotTamperingIsRejected()
    {
        var id=Guid.NewGuid();var next=Document(id,2,"replacement");MongoNativeHolonHistory.Stage(next,Document(id,1,"original"));
        next[MongoNativeHolonHistory.Field]["records"][0]["bson"]["Name"]="tampered";
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>MongoNativeHolonHistory.Read(next));
    }

    [TestMethod]
    public void EarlierJsonEntriesRemainByteExactWhenAppendingNativeSnapshot()
    {
        var id=Guid.NewGuid();var original=Document(id,1,"legacy");
        var core=DataHelper.ConvertMongoEntityToOASISHolon(BsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon>(original),true);
        var json=StoredRecordJson.Serialize<IHolon>(core);
        var entry=new BsonDocument{{"json",json},{"sha256",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))}};
        var observed=Document(id,2,"observed");observed[MongoNativeHolonHistory.Field]=new BsonDocument{{"schema",1},{"records",new BsonArray{entry}}};
        var next=Document(id,3,"next");MongoNativeHolonHistory.Stage(next,observed);
        CollectionAssert.AreEqual(entry.ToBson(),next[MongoNativeHolonHistory.Field]["records"][0].AsBsonDocument.ToBson());
        CollectionAssert.AreEqual(new[]{1,2,3},MongoNativeHolonHistory.Read(next).Select(row=>row.Version).ToArray());
    }

    [TestMethod]
    public void ValidChecksumDoesNotAllowArchivedVersionAfterCurrent()
    {
        var id=Guid.NewGuid();var current=Document(id,4,"current");
        MongoNativeHolonHistory.Stage(current,Document(id,3,"archived"));
        current["Version"]=2;
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>MongoNativeHolonHistory.Read(current));
    }

    [TestMethod]
    public void BackwardsStagingFailsWithoutChangingReplacement()
    {
        var id=Guid.NewGuid();var current=Document(id,2,"backwards");var before=current.ToJson();
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>MongoNativeHolonHistory.Stage(current,Document(id,3,"observed")));
        Assert.AreEqual(before,current.ToJson());
    }

    [DataTestMethod]
    [DataRow(true)] [DataRow(false)]
    public void InvalidCurrentIdentityOrVersionIsRejected(bool emptyId)
    {
        var current=Document(emptyId?Guid.Empty:Guid.NewGuid(),emptyId?1:-1,"invalid");
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>MongoNativeHolonHistory.Read(current));
    }

    [TestMethod]
    public void SameLogicalVersionCanBePreservedButCannotBeSilentlySelected()
    {
        var id=Guid.NewGuid();var second=Document(id,1,"replacement");MongoNativeHolonHistory.Stage(second,Document(id,1,"original"));
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>MongoNativeHolonHistory.Read(second));
        var third=Document(id,2,"next");MongoNativeHolonHistory.Stage(third,second);
        Assert.AreEqual(3,MongoNativeHolonHistory.Read(third,false).Count);
    }
}
