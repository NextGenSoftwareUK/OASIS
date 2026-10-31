using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public sealed class CosmosKnownAcknowledgementCountTests
{
    public static IEnumerable<object[]> PointCases()
    {foreach(var kind in Enumerable.Range(0,3))foreach(var sync in new[]{false,true})foreach(var known in new[]{false,true})yield return new object[]{kind,sync,known,"bound"};
        foreach(var sync in new[]{false,true})foreach(var mode in new[]{"assigned","foreign","invalid"})yield return new object[]{2,sync,true,mode};}

    [DataTestMethod][DynamicData(nameof(PointCases),DynamicDataSourceType.Method)]
    public async Task PointErrorsCountOnlyBoundNativeAcknowledgements(int kind,bool synchronous,bool known,string mode)
    {
        IHolonBase row=kind==0?new Avatar():kind==1?new AvatarDetail():new Holon();
        row.Id=mode=="assigned"?Guid.Empty:Guid.NewGuid();row.Version=7;
        var acknowledged=mode is "assigned" or "foreign"?Guid.NewGuid():row.Id;
        var cause=new CosmosMutationReconciliationException(mode=="invalid"?"not-a-native-id":acknowledged.ToString(),new IOException("owned native outcome"),known);
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned-count",new()){IsProviderActivated=true};
        var avatar=new Mock<IAvatarRepository>();var detail=new Mock<IAvatarDetailRepository>();var holon=new Mock<IHolonRepository>();
        avatar.Setup(store=>store.UpsertAsync(It.IsAny<IAvatar>())).ThrowsAsync(cause);
        detail.Setup(store=>store.UpsertAsync(It.IsAny<IAvatarDetail>())).ThrowsAsync(cause);
        holon.Setup(store=>store.UpsertAsync(It.IsAny<IHolon>())).ThrowsAsync(cause);
        foreach(var pair in new Dictionary<string,object>{{"avatarRepository",avatar.Object},{"avatarDetailRepository",detail.Object},{"holonRepository",holon.Object}})
            typeof(AzureCosmosDBOASIS).GetField(pair.Key,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,pair.Value);
        var before=StoredRecordJson.CaptureRuntimeToken(row).ToString();var expected=known&&(mode is "bound" or "assigned")?1:0;
        void Check<T>(OASISResult<T> result)
        {
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreSame(cause,result.Exception);
            Assert.AreEqual(expected,result.SavedCount,"Only an explicitly acknowledged native identity belonging to this dispatched record counts.");
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));Assert.AreEqual(before,StoredRecordJson.CaptureRuntimeToken(row).ToString());
        }
        if(kind==0)Check(synchronous?provider.SaveAvatar((IAvatar)row):await provider.SaveAvatarAsync((IAvatar)row));
        else if(kind==1)Check(synchronous?provider.SaveAvatarDetail((IAvatarDetail)row):await provider.SaveAvatarDetailAsync((IAvatarDetail)row));
        else Check(synchronous?provider.SaveHolon((IHolon)row,saveChildren:false):await provider.SaveHolonAsync((IHolon)row,saveChildren:false));
    }

    private sealed class Writer(string mode,bool known,bool asResult,Guid foreign):AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned-count",new())
    {
        public int Calls;public Exception Cause=null!;
        protected override Task<OASISResult<IHolon>> SavePlannedHolonAsync(PlannedHolonWrite write)
        {
            Calls++;
            if(mode=="second"&&Calls==1)
            {
                write.Snapshot.Version=1;write.Snapshot.VersionId=Guid.NewGuid();write.Snapshot.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(write,HolonChildRoleWriteKind.SourceSave,false,null);
                return Task.FromResult(new OASISResult<IHolon>{IsSaved=true,Result=write.Snapshot,SavedCount=99});
            }
            var error=new CosmosMutationReconciliationException((mode=="foreign"?foreign:write.Snapshot.Id).ToString(),new IOException("owned native outcome"),known);
            Cause=mode=="aggregate"?new AggregateException(error,error):error;
            return asResult?Task.FromResult(new OASISResult<IHolon>{IsError=true,Exception=Cause,SavedCount=99}):Task.FromException<OASISResult<IHolon>>(Cause);
        }
    }
    public static IEnumerable<object[]> BatchCases()
    {foreach(var sync in new[]{false,true})foreach(var asResult in new[]{false,true}){
        foreach(var mode in new[]{"first","second"})foreach(var known in new[]{false,true})yield return new object[]{sync,mode,known,asResult};
        foreach(var mode in new[]{"aggregate","foreign"})yield return new object[]{sync,mode,true,asResult};}}

    [DataTestMethod][DynamicData(nameof(BatchCases),DynamicDataSourceType.Method)]
    public async Task GraphErrorsCountDistinctAcknowledgedDispatchedIdentities(bool synchronous,string mode,bool known,bool asResult)
    {
        var rows=new[]{new Holon{Id=Guid.NewGuid()},new Holon{Id=Guid.NewGuid()},new Holon{Id=Guid.NewGuid()}};
        var provider=new Writer(mode,known,asResult,rows[1].Id);
        var result=synchronous?provider.SaveHolons(rows,continueOnError:true):await provider.SaveHolonsAsync(rows,continueOnError:true);
        var expected=(mode=="second"?1:0)+(known&&mode!="foreign"?1:0);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(expected,result.SavedCount);
        Assert.AreEqual(mode=="second"?2:1,provider.Calls,"Unknown/reconciliation-required outcomes must stop subsequent native dispatch.");
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
        Assert.AreSame(provider.Cause,result.Exception is CosmosPartialMutationException partial?partial.InnerException:result.Exception);
        if(result.Exception is CosmosPartialMutationException graph)Assert.AreEqual(expected,graph.AcknowledgedRecordIds.Distinct().Count());
        Assert.AreEqual(mode=="second"?1:0,rows[0].Version);Assert.AreEqual(0,rows[1].Version);Assert.AreEqual(0,rows[2].Version);
    }
}
