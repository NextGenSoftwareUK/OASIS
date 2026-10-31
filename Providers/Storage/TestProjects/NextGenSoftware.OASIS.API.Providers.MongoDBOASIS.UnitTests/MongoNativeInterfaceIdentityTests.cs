using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Native=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoNativeInterfaceIdentityTests
{
    private static Native.HolonBase Record(int kind)=>kind switch{0=>new Native.Holon(),1=>new Native.Avatar(),_=>new Native.AvatarDetail()};
    public static IEnumerable<object[]> GetterCases(){foreach(var kind in new[]{0,1,2})foreach(var creator in new[]{false,true})yield return new object[]{kind,creator};}

    [DataTestMethod][DynamicData(nameof(GetterCases),DynamicDataSourceType.Method)]
    public void InterfaceIdentityBelongsToRecordRegardlessOfCreator(int kind,bool hasCreator)
    {
        var record=Record(kind);record.HolonId=Guid.NewGuid();
        if(hasCreator)record.CreatedByAvatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Id=Guid.NewGuid()};
        Assert.AreEqual(record.HolonId,((IHolonBase)record).Id);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void InterfaceIdentitySetterCannotMutateCreatorOrPhysicalKey(int kind)
    {
        var record=Record(kind);record.HolonId=Guid.NewGuid();record.Id="6adc6af6b7619719aaec0915";
        var creatorId=Guid.NewGuid();record.CreatedByAvatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Id=creatorId};
        var replacement=Guid.NewGuid();((IHolonBase)record).Id=replacement;
        Assert.AreEqual(creatorId,record.CreatedByAvatar.Id);
        Assert.AreEqual(replacement,record.HolonId);
        Assert.AreEqual("6adc6af6b7619719aaec0915",record.Id);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void InterfaceGuidAndStoredObjectIdRemainDistinct(int kind)
    {
        _=new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","native-identity-serializer-no-io");
        var record=Record(kind);record.Id="6adc6af6b7619719aaec0915";
        var id=Guid.NewGuid();((IHolonBase)record).Id=id;
        var bson=record.ToBsonDocument();
        Assert.AreEqual("6adc6af6b7619719aaec0915",bson["_id"].AsObjectId.ToString());
        Assert.AreEqual(id.ToString("D"),bson["HolonId"].AsString);
        Assert.IsNull(record.CreatedByAvatar);
    }
}
