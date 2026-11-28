using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public sealed class CompositeBook
    {
        public string Section {get;set;}="";
        public string Code {get;set;}="";
        public decimal Edition {get;set;}
        public string Title {get;set;}="";
        public int Stock {get;set;}
    }
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeCompositePrimaryKeysOwnIdentityAndAtomicUniqueness(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyCompositePrimaryKey,
            nativeJourneySuffix:"-composite-primary-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyCompositePrimaryKey(IOASISStorageProvider provider,
        List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();
        var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var contract="M09_"+Guid.NewGuid().ToString("N");
        var builder=new CosmicPocoModelBuilder<CompositeBook>(contract)
            .HasCompositeKey(row=>row.Section,row=>row.Code,row=>row.Edition);
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message+built.Exception);var model=built.Result;
        CollectionAssert.AreEqual(new[]{"Section","Code","Edition"},model.PrimaryKeyProperties.ToArray());
        builder.HasCompositeKey(row=>row.Section,row=>row.Title);
        using var writer=CosmicPocoContext<CompositeBook>.Create(manager,adapter,access,model).Result;
        CosmicPocoContext<CompositeBook> Open()=>CosmicPocoContext<CompositeBook>.Create(manager,adapter,access,model).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var timer=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}}
        CompositeBook Book(string section,string code,string title)=>new(){Section=section,Code=code,Edition=1.00m,Title=title,Stock=3};
        void Add(CosmicPocoContext<CompositeBook> context,CompositeBook book)
        {var registered=context.Add(book);Assert.IsFalse(registered.IsError,registered.Message+registered.Exception);owned.Add(context.Identity(book).Result);}
        var first=Book("a|b","c","first");var second=Book("a","b|c","second");Add(writer,first);Add(writer,second);
        var firstId=writer.Identity(first).Result;var secondId=writer.Identity(second).Result;Assert.AreNotEqual(firstId,secondId);
        Assert.IsTrue(writer.Add(Book("a|b","c","same logical identity")).IsError);
        Assert.IsTrue(writer.Add(new CompositeBook{Section=null!,Code="null",Edition=1m}).IsError);
        var created=await Timed("create-composite-batch",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);Assert.AreEqual(2,created.Result.Count);
        using var reader=Open();
        var found=await Timed("lookup-complete-key",()=>reader.FindByPrimaryKeyAsync(new object[]{"a|b","c",1.0m}));
        Assert.IsFalse(found.IsError,found.Message+found.Exception);Assert.IsTrue(found.IsLoaded);Assert.AreEqual("first",found.Result.Title);
        Assert.AreEqual(firstId,reader.Identity(found.Result).Result);
        var distinct=await Timed("lookup-framed-key",()=>reader.FindByPrimaryKeyAsync(new object[]{"a","b|c",1m}));
        Assert.IsFalse(distinct.IsError,distinct.Message+distinct.Exception);Assert.AreEqual("second",distinct.Result.Title);
        Assert.AreEqual(secondId,reader.Identity(distinct.Result).Result);
        using var duplicate=Open();Add(duplicate,Book("a|b","c","rejected duplicate"));
        var rejected=await Timed("reject-native-duplicate",()=>duplicate.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.IsSaved);Assert.AreEqual("COSMIC_UNIQUE_KEY_CONFLICT",rejected.ErrorCode);
        using var atomic=Open();Add(atomic,Book("fresh","new","must rollback"));Add(atomic,Book("a|b","c","batch duplicate"));
        var rolledBack=await Timed("rollback-key-batch",()=>atomic.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(rolledBack.IsError);Assert.IsFalse(rolledBack.IsSaved);
        using var verifier=Open();
        var absent=await Timed("verify-atomic-absence",()=>verifier.FindByPrimaryKeyAsync(new object[]{"fresh","new",1m}));
        Assert.IsFalse(absent.IsError,absent.Message+absent.Exception);Assert.IsNull(absent.Result);
        using var physical=CosmicSession.Create(manager,adapter,access).Result;
        var failedIds=owned.Skip(3).Take(2).ToArray();
        var physicalAbsence=await Timed("verify-atomic-row-absence",()=>physical.Linq<NextGenSoftware.OASIS.API.Core.Holons.Holon>().Result
            .Where(row=>row.Id==failedIds[0]).ToCosmicListAsync());
        Assert.IsFalse(physicalAbsence.IsError,physicalAbsence.Message+physicalAbsence.Exception);Assert.AreEqual(0,physicalAbsence.Result.Count);
        found.Result.Title="updated";found.Result.Stock=8;
        var changed=await Timed("update-same-primary",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(changed.IsError,changed.Message+changed.Exception);Assert.IsTrue(changed.IsSaved);
        Assert.AreEqual(firstId,reader.Identity(found.Result).Result);
        using var foreign=CosmicPocoContext<CompositeBook>.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model).Result;
        var hidden=await Timed("foreign-primary-visibility",()=>foreign.FindByPrimaryKeyAsync(new object[]{"a|b","c",1m}));
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.IsNull(hidden.Result);
        found.Result.Code="mutated";
        var immutable=await Timed("reject-primary-change",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(immutable.IsError);Assert.IsFalse(immutable.IsSaved);Assert.AreEqual("COSMIC_SESSION_IDENTITY_CHANGED",immutable.ErrorCode);
        found.Result.Code="c";Assert.IsFalse(reader.Remove(found.Result).IsError);
        var deleted=await Timed("delete-release-primary",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(deleted.IsError,deleted.Message+deleted.Exception);Assert.IsTrue(deleted.IsSaved);
        using var reuse=Open();var replacement=Book("a|b","c","replacement");Add(reuse,replacement);
        var reused=await Timed("reuse-released-primary",()=>reuse.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(reused.IsError,reused.Message+reused.Exception);Assert.IsTrue(reused.IsSaved);
        var replacementId=reuse.Identity(replacement).Result;Assert.AreNotEqual(firstId,replacementId);
        using var final=Open();
        var finalRead=await Timed("verify-reused-primary",()=>final.FindByPrimaryKeyAsync(new object[]{"a|b","c",1.000m}));
        Assert.IsFalse(finalRead.IsError,finalRead.Message+finalRead.Exception);Assert.AreEqual("replacement",finalRead.Result.Title);
        Assert.AreEqual(replacementId,final.Identity(finalRead.Result).Result);
        return new NativeFixtureJourneyProof(10,5,new Dictionary<string,bool>{
            ["compositeLogicalPrimaryWithoutGuidProperty"]=true,["typedTupleFramingAndNumericEquality"]=true,
            ["nativeAtomicUniqueness"]=true,["wholeBatchRollback"]=true,["identityMapConflict"]=true,
            ["requiredPrimaryComponents"]=true,["immutablePrimaryUpdates"]=true,["actorBoundKeyLookup"]=true,
            ["deleteReleasesLogicalIdentity"]=true,["immutablePrimaryModel"]=true});
    }
}
