using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests;

public partial class PostgreSqlNativeAcceptanceTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeHolonDeletePreservesChildrenAndRejectsStaleRevival(bool key,bool synchronous)
    {
        var id=Guid.NewGuid();var childId=Guid.NewGuid();var missingId=Guid.NewGuid();
        await using var fixture=new GraphFixture($"native-holon-delete-{key}-{synchronous}",new[]{id,childId,missingId});await fixture.Begin();
        var child=new Holon{Id=childId,Name="retained-child",MetaData=new(){["owned-fixture"]=fixture.Marker}};
        var root=new Holon{Id=id,Name="retained-root",Children=new List<IHolon>{child},MetaData=new(){["owned-fixture"]=fixture.Marker,["precision"]=0.1234567890123456789012345678m}};
        fixture.BeforeMutation();var saved=await fixture.Provider.SaveHolonAsync(root);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());await fixture.Observe();
        var original=await fixture.Body(id);var childBody=await fixture.Body(childId);Assert.IsNotNull(original);Assert.IsNotNull(childBody);
        var cached=await fixture.Provider.LoadHolonAsync(id,loadChildrenFromProvider:true);Assert.IsFalse(cached.IsError,cached.Message);
        fixture.BeforeMutation();var deleted=key?(synchronous?fixture.Provider.DeleteHolon(id.ToString()):await fixture.Provider.DeleteHolonAsync(id.ToString())):(synchronous?fixture.Provider.DeleteHolon(id):await fixture.Provider.DeleteHolonAsync(id));
        Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());await fixture.Observe();Assert.IsTrue(deleted.IsDeleted);Assert.IsTrue(deleted.Result.IsDeleted);Assert.IsFalse(deleted.Result.IsActive);Assert.AreEqual(1,deleted.DeletedCount);Assert.AreEqual(1,deleted.SavedCount);Assert.AreEqual(2,deleted.Result.Version);Assert.AreEqual(DateTimeKind.Utc,deleted.Result.DeletedDate.Kind);
        var tombstone=await fixture.Body(id);Assert.IsNotNull(tombstone);Assert.IsTrue(StoredRecordPayloadEquality.Equivalent(WithoutLifecycleFields(original),WithoutLifecycleFields(tombstone)));
        Assert.AreEqual(childBody,await fixture.Body(childId));Assert.IsFalse((await fixture.Provider.LoadHolonAsync(childId,loadChildren:false)).IsError);
        Assert.IsTrue((await fixture.Provider.LoadHolonAsync(id,loadChildren:false)).IsError);Assert.IsTrue((await fixture.Provider.LoadHolonAsync(id,loadChildren:false,version:1)).IsError);
        var all=await fixture.Provider.LoadAllHolonsAsync(version:1);Assert.IsFalse(all.IsError,all.Message);Assert.IsFalse(all.Result.Any(row=>row.Id==id));
        fixture.BeforeMutation();var stale=await fixture.Provider.SaveHolonAsync(cached.Result);Assert.IsTrue(stale.IsError);Assert.IsInstanceOfType<StorageLifecycleConflictException>(stale.Exception);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,stale.ErrorCode);await fixture.Observe();Assert.AreEqual(0,stale.SavedCount);Assert.AreEqual(1,cached.Result.Version);Assert.AreEqual(tombstone,await fixture.Body(id));Assert.AreEqual(childBody,await fixture.Body(childId));
        fixture.BeforeMutation();var repeat=await fixture.Provider.DeleteHolonAsync(id);Assert.IsFalse(repeat.IsError,repeat.Exception?.ToString());await fixture.Observe();Assert.AreEqual(0,repeat.SavedCount);Assert.AreEqual(tombstone,await fixture.Body(id));
        fixture.BeforeMutation();var missing=await fixture.Provider.DeleteHolonAsync(missingId);Assert.IsTrue(missing.IsError);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);Assert.IsFalse(missing.IsDeleted);await fixture.Observe();
    }
}
