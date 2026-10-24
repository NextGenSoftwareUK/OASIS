using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
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
    public void SameLogicalVersionCanBePreservedButCannotBeSilentlySelected()
    {
        var id=Guid.NewGuid();var second=Document(id,1,"replacement");MongoNativeHolonHistory.Stage(second,Document(id,1,"original"));
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>MongoNativeHolonHistory.Read(second));
        var third=Document(id,2,"next");MongoNativeHolonHistory.Stage(third,second);
        Assert.AreEqual(3,MongoNativeHolonHistory.Read(third,false).Count);
    }
}
