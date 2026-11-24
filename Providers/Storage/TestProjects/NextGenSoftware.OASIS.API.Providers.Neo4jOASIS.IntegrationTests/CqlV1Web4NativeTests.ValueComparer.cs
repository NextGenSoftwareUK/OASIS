using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public sealed record ComparedCode {public string Value {get;set;}="";}
    public sealed class ComparedReadingHolon:Holon {public ComparedCode? Code {get;set;}}

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeValueComparersControlStateFieldsAndAuthorizedNoops(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyValueComparerJourney,nativeJourneySuffix:"-value-comparer-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyValueComparerJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var other=Guid.NewGuid();
        var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        int snapshots=0,hashes=0,equalities=0;
        var comparer=new CosmicValueComparer<ComparedCode>(
            (left,right)=>{equalities++;return StringComparer.OrdinalIgnoreCase.Equals(left.Value,right.Value);},
            value=>{hashes++;return StringComparer.OrdinalIgnoreCase.GetHashCode(value.Value);},
            value=>{snapshots++;return new ComparedCode{Value=value.Value};});
        var conversion=new CosmicValueConverter<ComparedCode,string>(
            value=>"cmp:"+value.Value,
            value=>value.StartsWith("cmp:",StringComparison.Ordinal)
                ?new ComparedCode{Value=value.Substring(4)}:throw new ArgumentException("Invalid compared code."));
        var builder=new CosmicModelBuilder();
        builder.Entity<ComparedReadingHolon>().Property(row=>row.Code)
            .HasValueComparer(comparer).HasConversion(conversion).HasMetadataKey("compared_code");
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        var model=built.Result.WithoutQueryFilters();
        builder.Entity<ComparedReadingHolon>().Property(row=>row.Code).HasValueComparer(
            new CosmicValueComparer<ComparedCode>(
                (left,right)=>StringComparer.Ordinal.Equals(left.Value,right.Value),
                value=>StringComparer.Ordinal.GetHashCode(value.Value),
                value=>new ComparedCode{Value=value.Value}));
        using var writer=CosmicSession.Create(manager,adapter,access,model).Result;
        using var reader=CosmicSession.Create(manager,adapter,access,model).Result;
        ComparedReadingHolon New(Guid creator,string name,string value)
        {
            var row=new ComparedReadingHolon{Id=Guid.NewGuid(),Name=name,Code=new ComparedCode{Value=value},
                CreatedByAvatarId=creator,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        var first=New(actor,"first","Alpha");var second=New(actor,"second","Beta");
        var foreign=New(other,"foreign","Foreign");
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        async Task<ComparedReadingHolon> Read(Guid id,string timing)
        {
            var query=reader.Query<ComparedReadingHolon>().Result.Where(row=>row.Id==id);
            Assert.IsFalse(query.IsError,query.Message);
            var rows=await Timed(timing,()=>reader.ReadAsync(query.Result,trackResults:false));
            Assert.IsFalse(rows.IsError,rows.Message+rows.Exception);Assert.AreEqual(1,rows.Result.Count);
            return rows.Result[0];
        }
        var added=writer.AddRange(new[]{first,second});Assert.IsFalse(added.IsError,added.Message);
        var created=await Timed("create-own",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        using(var foreignWriter=CosmicSession.Create(manager,adapter,
            CosmicQueryReadAccess.OwnerOrPublic(other).Result,model).Result)
        {
            Assert.IsFalse(foreignWriter.Add(foreign).IsError);
            var foreignCreated=await Timed("create-foreign",()=>foreignWriter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(foreignCreated.IsError,foreignCreated.Message+foreignCreated.Exception);
            Assert.IsTrue(foreignCreated.IsSaved);
        }
        var firstVersion=first.Version;var secondVersion=second.Version;
        first.Code!.Value="alpha";
        var state=writer.State(first);Assert.IsFalse(state.IsError,state.Message);
        Assert.AreEqual(CosmicEntityState.Unchanged,state.Result);
        var noop=await Timed("noop-equal",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(noop.IsError,noop.Message+noop.Exception);Assert.IsFalse(noop.IsSaved);
        Assert.IsFalse(noop.HasAnyHolonsChanged);Assert.AreEqual(firstVersion,first.Version);
        first.Name="partial first";
        var partial=await Timed("selected-name",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(partial.IsError,partial.Message+partial.Exception);Assert.IsTrue(partial.IsSaved);
        Assert.AreEqual(firstVersion+1,first.Version);
        Assert.AreEqual("alpha",first.Code.Value,"Unwritten equivalent caller values must be preserved.");
        var storedFirst=await Read(first.Id,"read-selected-name");
        Assert.AreEqual("partial first",storedFirst.Name);
        Assert.AreEqual("Alpha",storedFirst.Code!.Value,"An equivalent unsaved property must not overwrite native data.");
        Assert.AreEqual(CosmicEntityState.Unchanged,writer.State(first).Result);

        first.Name="pending first";second.Code!.Value="Gamma";
        Assert.IsFalse(writer.SetLifecycle(new CosmicSessionLifecycle(beforeSave:(context,token)=>
        {
            first.Name="partial first";
            return Task.FromResult(new OASISResult<bool>(true));
        })).IsError);
        var mixed=await Timed("mixed-callback",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(mixed.IsError,mixed.Message+mixed.Exception);Assert.IsTrue(mixed.IsSaved);
        Assert.AreEqual(firstVersion+1,first.Version,"The callback-created no-op must not receive a write revision.");
        Assert.AreEqual(secondVersion+1,second.Version);
        var unchanged=await Read(first.Id,"read-unchanged-mixed");
        var changed=await Read(second.Id,"read-written-mixed");
        Assert.AreEqual(firstVersion+1,unchanged.Version);Assert.AreEqual("Alpha",unchanged.Code!.Value);
        Assert.AreEqual("Gamma",changed.Code!.Value);Assert.AreEqual(secondVersion+1,changed.Version);
        Assert.AreEqual(CosmicEntityState.Unchanged,writer.State(first).Result);
        Assert.AreEqual(CosmicEntityState.Unchanged,writer.State(second).Result);

        var marked=HolonChangeTracking.SetPropertyModified(first,"/Code");
        Assert.IsFalse(marked.IsError,marked.Message);
        Assert.AreEqual(CosmicEntityState.Modified,writer.State(first).Result);
        var explicitWrite=await Timed("explicit-equal",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(explicitWrite.IsError,explicitWrite.Message+explicitWrite.Exception);Assert.IsTrue(explicitWrite.IsSaved);
        Assert.AreEqual(firstVersion+2,first.Version);
        var explicitStored=await Read(first.Id,"read-explicit");
        Assert.AreEqual("alpha",explicitStored.Code!.Value);
        var flags=HolonChangeTracking.GetPropertyValues(first,"/Code");
        Assert.IsFalse(flags.IsError,flags.Message);Assert.IsFalse(flags.Result.IsExplicitlyModified);
        Assert.AreEqual(CosmicEntityState.Unchanged,writer.State(first).Result);

        Assert.IsFalse(writer.Attach(foreign).IsError);
        second.Code!.Value="Delta";foreign.Name="pending forbidden";
        Assert.IsFalse(writer.SetLifecycle(new CosmicSessionLifecycle(beforeSave:(context,token)=>
        {
            foreign.Name="foreign";
            return Task.FromResult(new OASISResult<bool>(true));
        })).IsError);
        var denied=await Timed("foreign-noop-denied",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(denied.IsError,"A callback-created no-op must remain inside selected-root authorization.");
        Assert.IsFalse(denied.IsSaved);
        Assert.AreEqual("COSMIC_WRITE_FORBIDDEN",denied.ErrorCode,denied.Message+denied.Exception);
        var preserved=await Read(second.Id,"read-after-denied");
        Assert.AreEqual("Gamma",preserved.Code!.Value);
        Assert.AreEqual(secondVersion+1,preserved.Version);
        Assert.IsTrue(snapshots>0 && hashes>0 && equalities>0);
        return new NativeFixtureJourneyProof(8,6,new Dictionary<string,bool>{
            ["typedComparerAndImmutableModel"]=true,["equivalentChangeIsNoop"]=true,
            ["selectedWritePreservesEquivalentStorageAndCaller"]=true,
            ["callbacksRecomputePrivateChanges"]=true,["mixedNoopDoesNotBumpRevision"]=true,
            ["explicitModificationOverridesEquality"]=true,["noopsRemainAuthorized"]=true,
            ["detachedSnapshotsAndHashContractUsed"]=true});
    }
}
