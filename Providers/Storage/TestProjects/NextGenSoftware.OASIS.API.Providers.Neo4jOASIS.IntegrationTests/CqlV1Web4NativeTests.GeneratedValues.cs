using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public sealed class GeneratedValueHolon:Holon
    {
        public string? Status {get;set;}
        public int Priority {get;set;}
        public string? Code {get;set;}
        public int Quantity {get;set;}
        public int UnitPrice {get;set;}
        public int Total {get;set;}
    }

    // M24: model-declared defaults, insert generators and computed values.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeModelDefaultsGeneratorsAndComputedValuesPersist(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyGeneratedValuesJourney,nativeJourneySuffix:"-generated-values-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyGeneratedValuesJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var checks=0;

        // 1. Model admission: type-checked defaults, one generation rule per property, identity excluded.
        var builder=new CosmicModelBuilder();
        var entity=builder.Entity<GeneratedValueHolon>();
        entity.Property(row=>row.Status).HasMetadataKey("gen_status");
        entity.Property(row=>row.Priority).HasMetadataKey("gen_priority");
        entity.Property(row=>row.Code).HasMetadataKey("gen_code");
        entity.Property(row=>row.Quantity).HasMetadataKey("gen_quantity");
        entity.Property(row=>row.UnitPrice).HasMetadataKey("gen_unit_price");
        entity.Property(row=>row.Total).HasMetadataKey("gen_total");
        entity.Property(row=>row.Status).HasDefaultValue("draft").HasMaxLength(8);
        entity.Property(row=>row.Priority).HasDefaultValue(3);
        entity.Property(row=>row.Code).HasValueGeneratedOnAdd(row=>"GV-"+row.Name);
        entity.Property(row=>row.Total).HasComputedValue(row=>row.Quantity*row.UnitPrice);
        Assert.ThrowsException<ArgumentException>(()=>entity.Property(row=>row.Priority).HasDefaultValue("three"));
        Assert.ThrowsException<ArgumentException>(()=>entity.Property(row=>row.Status).HasDefaultValue("again"));
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        var model=built.Result;
        Assert.AreEqual(4,model.GeneratedValues(typeof(GeneratedValueHolon)).Count);
        checks++;

        GeneratedValueHolon New(string name)
        {
            var row=new GeneratedValueHolon{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        using var writer=CosmicSession.Create(manager,adapter,access,model).Result;
        var plain=New("alpha");plain.Quantity=4;plain.UnitPrice=25;plain.Total=-1;
        var explicitValues=New("beta");explicitValues.Status="active";explicitValues.Priority=9;explicitValues.Code="CUSTOM";
        explicitValues.Quantity=2;explicitValues.UnitPrice=3;
        Assert.IsFalse(writer.Add(plain).IsError);Assert.IsFalse(writer.Add(explicitValues).IsError);
        var created=await Timed("generated-create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);

        // 2. Defaults, generator and computed value are published back to the caller's entity.
        Assert.AreEqual("draft",plain.Status);Assert.AreEqual(3,plain.Priority);
        Assert.AreEqual("GV-alpha",plain.Code);Assert.AreEqual(100,plain.Total);
        checks++;

        // 3. Explicit insert values win over defaults and generators; computed values always recompute.
        Assert.AreEqual("active",explicitValues.Status);Assert.AreEqual(9,explicitValues.Priority);
        Assert.AreEqual("CUSTOM",explicitValues.Code);Assert.AreEqual(6,explicitValues.Total);
        checks++;

        // 4. The native store holds the produced values (fresh untracked read, native predicate on computed value).
        using var reader=CosmicSession.Create(manager,adapter,access,model).Result;
        var id=plain.Id;
        var query=reader.Query<GeneratedValueHolon>().Result.Where(row=>row.Id==id && row.Total==100 && row.Status=="draft").Result;
        var stored=await Timed("generated-native-read",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(stored.IsError,stored.Message+stored.Exception);Assert.AreEqual(1,stored.Result.Count);
        Assert.AreEqual(3,stored.Result[0].Priority);Assert.AreEqual("GV-alpha",stored.Result[0].Code);
        checks++;

        // 5. Updates recompute the computed value but never reapply insert defaults to cleared properties.
        plain.Quantity=5;plain.Status=null;plain.Code=null;
        var updated=await Timed("generated-update",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        Assert.AreEqual(125,plain.Total);Assert.IsNull(plain.Status);Assert.IsNull(plain.Code);
        var reread=await Timed("generated-update-read",()=>reader.ReadAsync(
            reader.Query<GeneratedValueHolon>().Result.Where(row=>row.Id==id && row.Total==125).Result,trackResults:false));
        Assert.IsFalse(reread.IsError,reread.Message+reread.Exception);Assert.AreEqual(1,reread.Result.Count);
        Assert.IsNull(reread.Result[0].Status);
        checks++;

        // 6. Caller-assigned computed values are replaced, not stored.
        plain.Total=999;
        var forced=await Timed("generated-computed-override",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(forced.IsError,forced.Message+forced.Exception);
        Assert.AreEqual(125,plain.Total);
        var noOverride=await Timed("generated-override-read",()=>reader.ReadAsync(
            reader.Query<GeneratedValueHolon>().Result.Where(row=>row.Id==id && row.Total==999).Result,trackResults:false));
        Assert.IsFalse(noOverride.IsError,noOverride.Message+noOverride.Exception);Assert.AreEqual(0,noOverride.Result.Count);
        checks++;

        // 7. Generated values still pass model constraints: an over-length default is refused before any write.
        var strictBuilder=new CosmicModelBuilder();
        strictBuilder.Entity<GeneratedValueHolon>().Property(row=>row.Status).HasMetadataKey("gen_status");
        strictBuilder.Entity<GeneratedValueHolon>().Property(row=>row.Status).HasDefaultValue("far-too-long").HasMaxLength(4);
        var strict=strictBuilder.Build();Assert.IsFalse(strict.IsError,strict.Message);
        using var strictWriter=CosmicSession.Create(manager,adapter,access,strict.Result).Result;
        var refused=New("gamma");
        Assert.IsFalse(strictWriter.Add(refused).IsError);
        var rejected=await Timed("generated-constraint-refusal",()=>strictWriter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(rejected.IsError,"An over-length default must fail model validation.");
        var absent=await Timed("generated-refusal-read",()=>reader.ReadAsync(
            reader.Query<GeneratedValueHolon>().Result.Where(row=>row.Id==refused.Id).Result,trackResults:false));
        Assert.IsFalse(absent.IsError,absent.Message+absent.Exception);Assert.AreEqual(0,absent.Result.Count);
        owned.Remove(refused.Id);
        checks++;

        // 8. A failing generator refuses the whole unit of work without writing.
        var faultyBuilder=new CosmicModelBuilder();
        faultyBuilder.Entity<GeneratedValueHolon>().Property(row=>row.Code).HasMetadataKey("gen_code");
        faultyBuilder.Entity<GeneratedValueHolon>().Property(row=>row.Code).HasValueGeneratedOnAdd(_=>throw new InvalidOperationException("generator fault"));
        using var faultyWriter=CosmicSession.Create(manager,adapter,access,faultyBuilder.Build().Result).Result;
        var faulty=New("delta");
        Assert.IsFalse(faultyWriter.Add(faulty).IsError);
        var faulted=await Timed("generated-generator-fault",()=>faultyWriter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(faulted.IsError);
        var faultAbsent=await Timed("generated-fault-read",()=>reader.ReadAsync(
            reader.Query<GeneratedValueHolon>().Result.Where(row=>row.Id==faulty.Id).Result,trackResults:false));
        Assert.IsFalse(faultAbsent.IsError,faultAbsent.Message+faultAbsent.Exception);Assert.AreEqual(0,faultAbsent.Result.Count);
        owned.Remove(faulty.Id);
        checks++;

        return new NativeFixtureJourneyProof(checks,4,new Dictionary<string,bool>{
            ["modelAdmissionTypeChecked"]=true,["defaultsGeneratorComputedPublished"]=true,
            ["explicitValuesWinComputedRecomputes"]=true,["nativeStoreHoldsGeneratedValues"]=true,
            ["updateRecomputesWithoutReapplyingDefaults"]=true,["callerComputedOverrideReplaced"]=true,
            ["generatedValuesPassModelConstraints"]=true,["generatorFaultRefusesUnitOfWork"]=true});
    }
}
