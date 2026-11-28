using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    // Deliberately no holon inheritance, OASIS interface or persistence attributes.
    public sealed class PlainStock
    {
        public Guid Id {get;set;}
        public string Name {get;set;}="";
        public int Quantity {get;set;}
        public decimal Price {get;set;}
        public string Display=>Name+" / "+Quantity;
    }
    public sealed record ImmutablePlainStock(Guid Id,string Name,int Quantity);
    public sealed class ShadowIdentityStock {public string Name {get;set;}="";public int Quantity {get;set;}}

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeOrdinaryClassesPersistQueryAndTrackWithoutHolonInheritance(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyOrdinaryClassJourney,
            nativeJourneySuffix:"-ordinary-class-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyOrdinaryClassJourney(IOASISStorageProvider provider,
        List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();
        var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var timer=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}}
        var contract="M01_"+Guid.NewGuid().ToString("N");
        var builder=new CosmicPocoModelBuilder<PlainStock>(contract);
        builder.Ignore(row=>row.Display);
        builder.Property(row=>row.Name).IsRequired().HasMaxLength(32).HasMetadataKey("stock_name");
        builder.Property(row=>row.Price).HasPrecision(8,2);
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message+built.Exception);var model=built.Result;
        // A later builder change cannot alter the existing constructor, key or field paths.
        builder.Ignore(row=>row.Id);builder.Property(row=>row.Name).HasMetadataKey("later_name");
        using var writer=CosmicPocoContext<PlainStock>.Create(manager,adapter,access,model).Result;
        var first=new PlainStock{Name="alpha",Quantity=10,Price=12.25m};
        var second=new PlainStock{Id=Guid.NewGuid(),Name="beta",Quantity=20,Price=4.50m};
        Assert.IsFalse(writer.Add(first).IsError);Assert.IsFalse(writer.Add(second).IsError);
        var firstId=writer.Identity(first).Result;owned.Add(firstId);owned.Add(second.Id);
        Assert.AreEqual(Guid.Empty,first.Id,"Generated keys are private until native acknowledgement.");
        var created=await Timed("create-poco-batch",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        Assert.AreEqual(2,created.Result.Count);Assert.AreEqual(firstId,first.Id);
        using var reader=CosmicPocoContext<PlainStock>.Create(manager,adapter,access,model).Result;
        var root=reader.Linq().Result;
        var query=root.Where(row=>row.Quantity>=10).OrderByDescending(row=>row.Quantity).ThenBy(row=>row.Name);
        var read=await Timed("filter-order-poco",()=>query.ToCosmicListAsync());
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(2,read.Result.Count);
        Assert.AreEqual(second.Id,read.Result[0].Id);Assert.AreEqual("alpha",read.Result[1].Name);
        var loaded=read.Result[1];Assert.AreNotSame(first,loaded);Assert.AreEqual(12.25m,loaded.Price);
        var projected=await Timed("project-poco",()=>root.Where(row=>row.Id==first.Id)
            .Select(row=>new{row.Name,row.Quantity,row.Price}).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);Assert.AreEqual("alpha",projected.Result.Single().Name);
        var count=await Timed("count-poco",()=>root.ToCosmicCountAsync(row=>row.Quantity>=10));
        Assert.IsFalse(count.IsError,count.Message+count.Exception);Assert.AreEqual(2,count.Result);
        var sum=await Timed("sum-poco",()=>root.ToCosmicSumAsync(row=>row.Quantity));
        Assert.IsFalse(sum.IsError,sum.Message+sum.Exception);Assert.AreEqual(30,sum.Result);
        var element=await Timed("element-poco",()=>root.ToCosmicSingleAsync(row=>row.Id==first.Id));
        Assert.IsFalse(element.IsError,element.Message+element.Exception);Assert.AreSame(loaded,element.Result);
        loaded.Quantity=31;loaded.Name="changed";
        var updated=await Timed("update-poco",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);Assert.AreEqual(1,updated.Result.Count);
        var fresh=await Timed("fresh-poco",()=>writer.Linq(trackingMode:CosmicQueryTrackingMode.NoTracking).Result
            .Where(row=>row.Id==first.Id&&row.Quantity==31).ToCosmicListAsync());
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual("changed",fresh.Result.Single().Name);
        Assert.AreNotSame(first,fresh.Result[0]);Assert.AreEqual(10,first.Quantity);
        // Writer still has its old native revision: no implicit refresh may overwrite the newer row.
        first.Quantity=99;
        var stale=await Timed("reject-stale-poco",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(stale.IsError);Assert.IsFalse(stale.IsSaved);Assert.AreEqual(CosmicConcurrencyException.Code,stale.ErrorCode);
        using var foreign=CosmicPocoContext<PlainStock>.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model).Result;
        var hidden=await Timed("foreign-poco-visibility",()=>foreign.Linq().Result.ToCosmicListAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        var immutableBuilder=new CosmicPocoModelBuilder<ImmutablePlainStock>(contract+"_immutable")
            .MaterializeUsing(v=>new ImmutablePlainStock(v.Id,v.Get<string>("Name"),v.Get<int>("Quantity")));
        var immutableModel=immutableBuilder.Build();Assert.IsFalse(immutableModel.IsError,immutableModel.Message+immutableModel.Exception);
        var immutable=new ImmutablePlainStock(Guid.NewGuid(),"constructor",3);owned.Add(immutable.Id);
        using var immutableWriter=CosmicPocoContext<ImmutablePlainStock>.Create(manager,adapter,access,immutableModel.Result).Result;
        Assert.IsFalse(immutableWriter.Add(immutable).IsError);
        var immutableSaved=await Timed("create-immutable-poco",()=>immutableWriter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(immutableSaved.IsError,immutableSaved.Message+immutableSaved.Exception);
        using var immutableReader=CosmicPocoContext<ImmutablePlainStock>.Create(manager,adapter,access,immutableModel.Result).Result;
        var immutableRead=await Timed("read-immutable-poco",()=>immutableReader.Linq().Result.ToCosmicSingleAsync());
        Assert.IsFalse(immutableRead.IsError,immutableRead.Message+immutableRead.Exception);Assert.AreEqual(immutable,immutableRead.Result);
        var replacement=immutableRead.Result with{Quantity=7};
        Assert.IsFalse(immutableReader.Replace(immutableRead.Result,replacement).IsError);
        var immutableUpdated=await Timed("update-immutable-poco",()=>immutableReader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(immutableUpdated.IsError,immutableUpdated.Message+immutableUpdated.Exception);Assert.IsTrue(immutableUpdated.IsSaved);
        var shadowModel=new CosmicPocoModelBuilder<ShadowIdentityStock>(contract+"_shadow").Build();
        Assert.IsFalse(shadowModel.IsError,shadowModel.Message+shadowModel.Exception);
        using var shadow=CosmicPocoContext<ShadowIdentityStock>.Create(manager,adapter,access,shadowModel.Result).Result;
        var noKey=new ShadowIdentityStock{Name="no base or key",Quantity=8};Assert.IsFalse(shadow.Add(noKey).IsError);
        owned.Add(shadow.Identity(noKey).Result);
        var shadowSaved=await Timed("create-shadow-key-poco",()=>shadow.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(shadowSaved.IsError,shadowSaved.Message+shadowSaved.Exception);Assert.IsTrue(shadowSaved.IsSaved);
        using var shadowReader=CosmicPocoContext<ShadowIdentityStock>.Create(manager,adapter,access,shadowModel.Result).Result;
        var shadowRead=await Timed("read-shadow-key-poco",()=>shadowReader.Linq().Result.ToCosmicSingleAsync());
        Assert.IsFalse(shadowRead.IsError,shadowRead.Message+shadowRead.Exception);Assert.AreEqual(8,shadowRead.Result.Quantity);
        Assert.AreEqual(shadow.Identity(noKey).Result,shadowReader.Identity(shadowRead.Result).Result);
        // Schema discriminators keep the other two ordinary-class contracts out of this root.
        var isolated=await Timed("contract-isolation-poco",()=>root.ToCosmicCountAsync());
        Assert.IsFalse(isolated.IsError,isolated.Message+isolated.Exception);Assert.AreEqual(2,isolated.Result);
        Assert.IsFalse(reader.Remove(loaded).IsError);
        var removed=await Timed("delete-poco",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(removed.IsError,removed.Message+removed.Exception);Assert.IsTrue(removed.IsSaved);
        var absent=await Timed("read-deleted-poco",()=>root.Where(row=>row.Id==first.Id).ToCosmicListAsync());
        Assert.IsFalse(absent.IsError,absent.Message+absent.Exception);Assert.AreEqual(0,absent.Result.Count);
        return new NativeFixtureJourneyProof(10,7,new Dictionary<string,bool>{
            ["ordinaryClassNoBaseOrInterface"]=true,["nativeScalarFilterOrderProjection"]=true,
            ["asyncNativeCardinalityReductionElement"]=true,["trackedUpdateAndIdentityMap"]=true,
            ["staleNativeRevisionRejected"]=true,["callerVisibilityPreserved"]=true,
            ["constructorOnlyReplacement"]=true,["shadowNativeIdentity"]=true,
            ["immutableMappingAndContractIsolation"]=true,["acknowledgedSoftDelete"]=true});
    }
}
