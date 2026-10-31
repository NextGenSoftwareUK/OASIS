using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoMetadataIntentTests
{
    public class Boundary : DispatchProxy
    {
        public Func<MethodInfo,object?[]?,object?> Call=null!;
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>Call(method!,args);
        public static T Wrap<T>(Func<MethodInfo,object?[]?,object?> call)where T:class{var proxy=Create<T,Boundary>();((Boundary)(object)proxy).Call=call;return proxy;}
    }
    private sealed class EmptyCursor:IAsyncCursor<NativeHolon>
    {
        public IEnumerable<NativeHolon> Current=>Array.Empty<NativeHolon>();
        public bool MoveNext(CancellationToken token=default)=>false;
        public Task<bool> MoveNextAsync(CancellationToken token=default)=>Task.FromResult(false);
        public void Dispose(){}
    }
    public static IEnumerable<object[]> Cases(){foreach(var sync in new[]{false,true})foreach(var mode in new[]{MetaKeyValuePairMatchMode.All,MetaKeyValuePairMatchMode.Any})foreach(var edit in Enumerable.Range(0,4))yield return new object[]{sync,mode,edit};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task MetadataIsDetachedBeforeSessionAwait(bool synchronous,MetaKeyValuePairMatchMode mode,int edit)
    {
        var provider=new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","metadata-intent-controls");
        var context=new MongoDbContext(provider.ConnectionString,provider.DBName);var native=context.Holon;
        var pairs=new Dictionary<string,string>{{"tag","original"},{"other","keep"}};
        var expected=(HolonRepository.BuildHolonTypeFilter(HolonType.All)&HolonRepository.BuildMetaDataMultiFilter(pairs,mode))
            .Render(new RenderArgs<NativeHolon>(native.DocumentSerializer,native.Settings.SerializerRegistry)).ToJson();
        string? observed=null;var reads=0;
        var collection=Boundary.Wrap<IMongoCollection<NativeHolon>>((method,args)=>{
            if(method.Name is "FindSync" or "FindAsync"){
                reads++;var filter=args!.OfType<FilterDefinition<NativeHolon>>().Single();
                observed=filter.Render(new RenderArgs<NativeHolon>(native.DocumentSerializer,native.Settings.SerializerRegistry)).ToJson();
                IAsyncCursor<NativeHolon> cursor=new EmptyCursor();return method.Name=="FindSync"?cursor:Task.FromResult(cursor);
            }
            return method.Invoke(native,args);
        });
        context.MongoDB=Boundary.Wrap<IMongoDatabase>((method,args)=>method.Name=="GetCollection"?collection:throw new InvalidOperationException("Unexpected database I/O."));
        var session=Boundary.Wrap<IClientSessionHandle>((method,args)=>method.Name=="Dispose"?null:throw new InvalidOperationException("Unexpected session I/O."));
        void Mutate(){if(edit==0)pairs["tag"]="changed";else if(edit==1)pairs.Remove("tag");else if(edit==2)pairs.Clear();else pairs.Add("new","changed");}
        async Task<IClientSessionHandle> Start(){await Task.Yield();Mutate();return session;}
        context.MongoClient=Boundary.Wrap<IMongoClient>((method,args)=>{
            if(method.Name=="StartSessionAsync")return Start();
            if(method.Name=="StartSession"){Mutate();return session;}
            throw new InvalidOperationException("Unexpected client I/O.");
        });
        provider.Database=context;
        var result=synchronous?provider.LoadHolonsByMetaData(pairs,mode,loadChildrenFromProvider:true):await provider.LoadHolonsByMetaDataAsync(pairs,mode,loadChildrenFromProvider:true);
        Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.AreEqual(1,reads);
        Assert.AreEqual(expected,observed,"Session-boundary edits must not change the original metadata predicate.");
    }
}
