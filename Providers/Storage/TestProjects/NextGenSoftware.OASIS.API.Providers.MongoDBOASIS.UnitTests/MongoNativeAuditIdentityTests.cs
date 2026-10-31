using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Native=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoNativeAuditIdentityTests
{
    private static Native.HolonBase Record(int kind)=>kind switch{0=>new Native.Holon(),1=>new Native.Avatar(),_=>new Native.AvatarDetail()};
    public static IEnumerable<object[]> Cases(){foreach(var kind in new[]{0,1,2})foreach(var field in new[]{0,1,2})yield return new object[]{kind,field};}
    private static void Text(Native.HolonBase record,int field,string value){if(field==0)record.CreatedByAvatarId=value;else if(field==1)record.ModifiedByAvatarId=value;else record.DeletedByAvatarId=value;}
    private static string Text(Native.HolonBase record,int field)=>field switch{0=>record.CreatedByAvatarId,1=>record.ModifiedByAvatarId,_=>record.DeletedByAvatarId};
    private static Guid Read(IAuditBase record,int field)=>field switch{0=>record.CreatedByAvatarId,1=>record.ModifiedByAvatarId,_=>record.DeletedByAvatarId};
    private static void Write(IAuditBase record,int field,Guid value){if(field==0)record.CreatedByAvatarId=value;else if(field==1)record.ModifiedByAvatarId=value;else record.DeletedByAvatarId=value;}

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public void AuditGetterReadsPersistedGuidWithoutRewritingText(int kind,int field)
    {
        var record=Record(kind);var id=Guid.NewGuid();var text=id.ToString("D").ToUpperInvariant();Text(record,field,text);
        Assert.AreEqual(id,Read(record,field));Assert.AreEqual(text,Text(record,field));
    }

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public void AuditSetterUpdatesOnlyItsPersistedField(int kind,int field)
    {
        var record=Record(kind);record.HolonId=Guid.NewGuid();var original=record.HolonId;
        var creatorId=Guid.NewGuid();record.CreatedByAvatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Id=creatorId};
        var id=Guid.NewGuid();Write(record,field,id);
        Assert.AreEqual(id.ToString("D"),Text(record,field));Assert.AreEqual(original,record.HolonId);Assert.AreEqual(creatorId,record.CreatedByAvatar.Id);
    }

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public void MissingOptionalAuditGuidIsEmpty(int kind,int field)
    {
        var record=Record(kind);Assert.AreEqual(Guid.Empty,Read(record,field));Text(record,field,"");Assert.AreEqual(Guid.Empty,Read(record,field));
    }

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public void MalformedStoredAuditGuidDoesNotBecomeAnonymous(int kind,int field)
    {
        var record=Record(kind);Text(record,field,"invalid-audit-id");
        Assert.ThrowsException<FormatException>(()=>Read(record,field));Assert.AreEqual("invalid-audit-id",Text(record,field));
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void AuditInterfacePersistsExistingGuidStringFields(int kind)
    {
        _=new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","native-audit-serializer-no-io");
        var record=Record(kind);record.Id="6adc6af6b7619719aaec0915";record.HolonId=Guid.NewGuid();
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};for(var index=0;index<3;index++)Write(record,index,ids[index]);
        var bson=record.ToBsonDocument();var names=new[]{"CreatedByAvatarId","ModifiedByAvatarId","DeletedByAvatarId"};
        for(var index=0;index<3;index++)Assert.AreEqual(ids[index].ToString("D"),bson[names[index]].AsString);
        Assert.AreEqual("6adc6af6b7619719aaec0915",bson["_id"].AsObjectId.ToString());
    }
}
