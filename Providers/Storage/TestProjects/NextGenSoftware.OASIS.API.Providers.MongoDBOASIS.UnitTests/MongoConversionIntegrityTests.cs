using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;
using NextGenSoftware.OASIS.Common;
using Native=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoConversionIntegrityTests
{
    private record Outcome(bool Error,bool Loaded,int Count,bool Saved,int SavedCount,object Value,Exception Cause,string Code,bool SourceError);
    private static Outcome Capture<T>(OASISResult<T> result,bool sourceError)=>new(result.IsError,result.IsLoaded,result.LoadedCount,result.IsSaved,result.SavedCount,result.Result,result.Exception,result.ErrorCode,sourceError);
    private static Native.HolonBase Record(int kind){Native.HolonBase row=kind switch{0=>new Native.Avatar(),1=>new Native.AvatarDetail(),_=>new Native.Holon()};row.HolonId=Guid.NewGuid();row.Id="6adc6af6b7619719aaec0915";return row;}
    private static OASISResult<T> Source<T>(T row,bool saved=false,Exception error=null)=>new(){Result=row,IsLoaded=row!=null,LoadedCount=row==null?0:1,IsSaved=saved,SavedCount=saved?1:0,IsError=error!=null,Exception=error,ErrorCode=error==null?null:"synthetic-storage-error"};
    private static Outcome Single(int kind,Native.HolonBase row,bool saved=false,Exception error=null)
    {
        if(kind==0){var source=Source((Native.Avatar)row,saved,error);var result=DataHelper.ConvertMongoEntityToOASISAvatar(source,false);return Capture(result,source.IsError);}
        if(kind==1){var source=Source((Native.AvatarDetail)row,saved,error);var result=DataHelper.ConvertMongoEntityToOASISAvatarDetail(source,false);return Capture(result,source.IsError);}
        {var source=Source((Native.Holon)row,saved,error);var result=DataHelper.ConvertMongoEntityToOASISHolon(source,false);return Capture(result,source.IsError);}
    }
    public static IEnumerable<object[]> AuditCases(){foreach(var kind in new[]{0,1,2})foreach(var field in new[]{0,1,2})yield return new object[]{kind,field};}
    private static void Corrupt(Native.HolonBase row,int field){if(field==0)row.CreatedByAvatarId="bad-audit-id";else if(field==1)row.ModifiedByAvatarId="bad-audit-id";else row.DeletedByAvatarId="bad-audit-id";}

    [DataTestMethod][DynamicData(nameof(AuditCases),DynamicDataSourceType.Method)]
    public void MalformedAuditFailsWithoutPublishingPartialRecord(int kind,int field)
    {
        var row=Record(kind);Corrupt(row,field);var result=Single(kind,row);
        Assert.IsTrue(result.Error);Assert.IsFalse(result.Loaded);Assert.AreEqual(0,result.Count);Assert.IsNull(result.Value);Assert.IsNotNull(result.Cause);
        Assert.IsFalse(result.SourceError);Assert.AreEqual(0,row.ProviderUniqueStorageKey.Count);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void ConversionFailurePreservesAcknowledgedSaveFacts(int kind)
    {
        var row=Record(kind);Corrupt(row,0);var result=Single(kind,row,saved:true);
        Assert.IsTrue(result.Error);Assert.IsNull(result.Value);Assert.IsTrue(result.Saved);Assert.AreEqual(1,result.SavedCount);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void OriginalStorageFailureRemainsAuthoritative(int kind)
    {
        var row=Record(kind);Corrupt(row,0);var cause=new System.IO.IOException("synthetic storage failure");var result=Single(kind,row,error:cause);
        Assert.IsTrue(result.Error);Assert.AreSame(cause,result.Cause);Assert.AreEqual("synthetic-storage-error",result.Code);Assert.IsNull(result.Value);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void MissingRecordDoesNotMutateSourceEnvelope(int kind)
    {
        var result=Single(kind,null);Assert.IsNull(result.Value);Assert.IsFalse(result.SourceError);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public void ValidProjectionDoesNotRepairInputProviderKeyDictionary(int kind)
    {
        var row=Record(kind);var actor=Guid.NewGuid();row.CreatedByAvatarId=actor.ToString("D");var result=Single(kind,row);
        Assert.IsFalse(result.Error);Assert.AreEqual(actor,((IHolonBase)result.Value).CreatedByAvatarId);Assert.AreEqual(0,row.ProviderUniqueStorageKey.Count);
    }

    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
    public void AccountCollectionFailurePublishesNoPartialList(int kind,bool nullRow)
    {
        var good=Record(kind);var bad=Record(kind);Corrupt(bad,0);
        Outcome result;
        if(kind==0){var source=new OASISResult<IEnumerable<Native.Avatar>>{Result=new[]{(Native.Avatar)good,nullRow?null:(Native.Avatar)bad},IsLoaded=true,LoadedCount=2};result=Capture(DataHelper.ConvertMongoEntitysToOASISAvatars(source,false),source.IsError);}
        else{var source=new OASISResult<IEnumerable<Native.AvatarDetail>>{Result=new[]{(Native.AvatarDetail)good,nullRow?null:(Native.AvatarDetail)bad},IsLoaded=true,LoadedCount=2};result=Capture(DataHelper.ConvertMongoEntitysToOASISAvatarDetails(source,false),source.IsError);}
        Assert.IsTrue(result.Error);Assert.IsNull(result.Value);Assert.IsFalse(result.Loaded);Assert.AreEqual(0,result.Count);Assert.IsFalse(result.SourceError);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void PlainHolonCollectionCannotSilentlySkipCorruptRows(bool nullRow)
    {
        var good=(Native.Holon)Record(2);var bad=(Native.Holon)Record(2);Corrupt(bad,0);
        Assert.ThrowsException<System.IO.InvalidDataException>(()=>DataHelper.ConvertMongoEntitysToOASISHolons(new[]{good,nullRow?null:bad},false).ToList());
    }

    private static IEnumerable<T> UnreadableRows<T>(){yield return ThrowEnumeration<T>();}
    private static T ThrowEnumeration<T>()=>throw new InvalidOperationException("An already-failed native result must not be enumerated.");

    [DataTestMethod][DataRow(0)][DataRow(1)]
    public void FailedNativeCollectionIsNotEnumeratedOrReplaced(int kind)
    {
        var cause=new System.IO.IOException("synthetic collection failure");Outcome result;
        if(kind==0){var source=new OASISResult<IEnumerable<Native.Avatar>>{IsError=true,Exception=cause,ErrorCode="native-collection-error",Result=UnreadableRows<Native.Avatar>()};result=Capture(DataHelper.ConvertMongoEntitysToOASISAvatars(source),source.IsError);}
        else{var source=new OASISResult<IEnumerable<Native.AvatarDetail>>{IsError=true,Exception=cause,ErrorCode="native-collection-error",Result=UnreadableRows<Native.AvatarDetail>()};result=Capture(DataHelper.ConvertMongoEntitysToOASISAvatarDetails(source),source.IsError);}
        Assert.IsTrue(result.Error);Assert.AreSame(cause,result.Cause);Assert.AreEqual("native-collection-error",result.Code);Assert.IsNull(result.Value);
    }

    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
    public void EmptyCollectionAndMissingCollectionAreNotConflated(int kind,bool missing)
    {
        Outcome result;
        if(kind==0){var source=new OASISResult<IEnumerable<Native.Avatar>>{Result=missing?null:Array.Empty<Native.Avatar>()};result=Capture(DataHelper.ConvertMongoEntitysToOASISAvatars(source),source.IsError);}
        else{var source=new OASISResult<IEnumerable<Native.AvatarDetail>>{Result=missing?null:Array.Empty<Native.AvatarDetail>()};result=Capture(DataHelper.ConvertMongoEntitysToOASISAvatarDetails(source),source.IsError);}
        Assert.AreEqual(missing,result.Error);Assert.AreEqual(!missing,result.Loaded);Assert.AreEqual(0,result.Count);
        if(missing)Assert.IsNull(result.Value);else Assert.IsNotNull(result.Value);
    }
}
