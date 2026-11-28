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
    public class TphAnimalHolon:Holon { public string? Sound {get;set;} }
    public class TphDogHolon:TphAnimalHolon { public string? Breed {get;set;} }
    public sealed class TphPuppyHolon:TphDogHolon { public int AgeWeeks {get;set;} }
    public sealed class TphCatHolon:TphAnimalHolon { public bool Indoor {get;set;} }
    public sealed class TphUndeclaredHolon:TphAnimalHolon {}

    // M19: table-per-hierarchy inheritance with a stored discriminator.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeInheritanceDiscriminatorMaterializesDerivedTypes(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyInheritanceJourney,nativeJourneySuffix:"-inheritance-native.json");

    private static CosmicModelBuilder TphBuilder()
    {
        var builder=new CosmicModelBuilder();
        builder.Entity<TphAnimalHolon>().Property(row=>row.Sound).HasMetadataKey("tph_sound");
        builder.Entity<TphDogHolon>().Property(row=>row.Sound).HasMetadataKey("tph_sound");
        builder.Entity<TphDogHolon>().Property(row=>row.Breed).HasMetadataKey("tph_breed");
        builder.Entity<TphPuppyHolon>().Property(row=>row.Sound).HasMetadataKey("tph_sound");
        builder.Entity<TphPuppyHolon>().Property(row=>row.Breed).HasMetadataKey("tph_breed");
        builder.Entity<TphPuppyHolon>().Property(row=>row.AgeWeeks).HasMetadataKey("tph_age_weeks");
        builder.Entity<TphCatHolon>().Property(row=>row.Sound).HasMetadataKey("tph_sound");
        builder.Entity<TphCatHolon>().Property(row=>row.Indoor).HasMetadataKey("tph_indoor");
        builder.Entity<TphAnimalHolon>().HasDiscriminator("tph_kind")
            .HasValue<TphAnimalHolon>("animal").HasValue<TphDogHolon>("dog")
            .HasValue<TphPuppyHolon>("puppy").HasValue<TphCatHolon>("cat");
        return builder;
    }

    private static Task<NativeFixtureJourneyProof> VerifyInheritanceJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
        =>VerifyMappedInheritanceJourney(provider,owned,timings,TphBuilder);

    private static async Task<NativeFixtureJourneyProof> VerifyMappedInheritanceJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings,Func<CosmicModelBuilder> mapping)
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
        T New<T>(string name) where T:Holon,new()
        {
            var row=new T{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        var checks=0;

        // 1. Model admission: unique values, a declared root, one hierarchy per type, no key collisions.
        var duplicate=new CosmicModelBuilder();
        var root=duplicate.Entity<TphAnimalHolon>().HasDiscriminator("tph_kind").HasValue<TphDogHolon>("dog");
        Assert.ThrowsException<ArgumentException>(()=>root.HasValue<TphCatHolon>("dog"));
        Assert.ThrowsException<ArgumentException>(()=>new CosmicModelBuilder().Entity<TphDogHolon>().HasDiscriminator("tph_kind").HasValue<TphPuppyHolon>("puppy")
            .HasValue<TphPuppyHolon>("puppy2").HasValue<TphPuppyHolon>("puppy3").HasValue<TphPuppyHolon>(""));
        var nested=new CosmicModelBuilder();nested.Entity<TphAnimalHolon>().HasDiscriminator("tph_kind");
        Assert.ThrowsException<ArgumentException>(()=>nested.Entity<TphDogHolon>().HasDiscriminator("tph_kind"));
        var colliding=new CosmicModelBuilder();
        colliding.Entity<TphAnimalHolon>().Property(row=>row.Sound).HasMetadataKey("tph_kind");
        colliding.Entity<TphAnimalHolon>().HasDiscriminator("tph_kind");
        Assert.IsTrue(colliding.Build().IsError);
        var built=mapping().Build();Assert.IsFalse(built.IsError,built.Message);
        var model=built.Result;
        Assert.IsTrue(model.TryGetDiscriminator(typeof(TphPuppyHolon),out var key,out var puppyValue));
        Assert.AreEqual("tph_kind",key);Assert.AreEqual("puppy",puppyValue);
        checks++;

        // 2. A mixed unit of work stores each record's discriminator natively.
        var animal=New<TphAnimalHolon>("generic");animal.Sound="...";
        var dog=New<TphDogHolon>("rex");dog.Sound="woof";dog.Breed="lab";
        var puppy=New<TphPuppyHolon>("bit");puppy.Sound="yip";puppy.Breed="lab";puppy.AgeWeeks=9;
        var cat=New<TphCatHolon>("tom");cat.Sound="meow";cat.Indoor=true;
        using(var writer=CosmicSession.Create(manager,adapter,access,model).Result)
        {
            Assert.IsFalse(writer.Add(animal).IsError);Assert.IsFalse(writer.Add(dog).IsError);
            Assert.IsFalse(writer.Add(puppy).IsError);Assert.IsFalse(writer.Add(cat).IsError);
            var saved=await Timed("tph-create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        }
        using(var raw=CosmicSession.Create(manager,adapter,access).Result)
        {
            var ids=new[]{animal.Id,dog.Id,puppy.Id,cat.Id};
            var stored=await Timed("tph-raw-read",()=>raw.Linq<Holon>().Result.Where(row=>ids.Contains(row.Id)).AsCosmicNoTracking().ToCosmicListAsync());
            Assert.IsFalse(stored.IsError,stored.Message+stored.Exception);Assert.AreEqual(4,stored.Result.Count);
            var kinds=stored.Result.ToDictionary(row=>row.Id,row=>row.MetaData["tph_kind"]?.ToString());
            Assert.AreEqual("animal",kinds[animal.Id]);Assert.AreEqual("dog",kinds[dog.Id]);
            Assert.AreEqual("puppy",kinds[puppy.Id]);Assert.AreEqual("cat",kinds[cat.Id]);
        }
        checks++;

        // 3. Querying the root returns every concrete type with its derived values.
        using var reader=CosmicSession.Create(manager,adapter,access,model).Result;
        var rootLinq=reader.Linq<TphAnimalHolon>();Assert.IsFalse(rootLinq.IsError,rootLinq.ErrorCode+": "+rootLinq.Message+rootLinq.Exception);
        var all=await Timed("tph-root-query",()=>reader.Linq<TphAnimalHolon>().Result.Where(row=>row.Name!=null).ToCosmicListAsync());
        Assert.IsFalse(all.IsError,all.Message+all.Exception);Assert.AreEqual(4,all.Result.Count);
        var byId=all.Result.ToDictionary(row=>row.Id);
        Assert.AreEqual(typeof(TphAnimalHolon),byId[animal.Id].GetType());
        Assert.AreEqual("lab",((TphDogHolon)byId[dog.Id]).Breed);Assert.AreEqual(typeof(TphDogHolon),byId[dog.Id].GetType());
        Assert.AreEqual(9,((TphPuppyHolon)byId[puppy.Id]).AgeWeeks);
        Assert.IsTrue(((TphCatHolon)byId[cat.Id]).Indoor);Assert.AreEqual("meow",byId[cat.Id].Sound);
        checks++;

        // 4. Querying a mid-level type natively returns it and its subtypes only, with derived predicates.
        var dogs=await Timed("tph-dog-query",()=>reader.Linq<TphDogHolon>().Result.Where(row=>row.Breed=="lab").ToCosmicListAsync());
        Assert.IsFalse(dogs.IsError,dogs.Message+dogs.Exception);
        CollectionAssert.AreEquivalent(new[]{dog.Id,puppy.Id},dogs.Result.Select(row=>row.Id).ToArray());
        Assert.AreSame(byId[dog.Id],dogs.Result.Single(row=>row.Id==dog.Id),"Tracked hierarchy rows share session identity.");
        checks++;

        // 5. Leaf queries return exactly their type; counts are native.
        var cats=await Timed("tph-cat-query",()=>reader.Linq<TphCatHolon>().Result.AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(cats.IsError,cats.Message+cats.Exception);Assert.AreEqual(cat.Id,cats.Result.Single().Id);
        var puppies=await Timed("tph-puppy-query",()=>reader.Linq<TphPuppyHolon>().Result.Where(row=>row.AgeWeeks>4).AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(puppies.IsError,puppies.Message+puppies.Exception);Assert.AreEqual(puppy.Id,puppies.Result.Single().Id);
        checks++;

        // 6. The hierarchy restriction survives application-filter bypass.
        var filtered=mapping();filtered.Entity<TphDogHolon>().HasQueryFilter(row=>row.Breed=="husky");
        var filteredModel=filtered.Build().Result;
        using(var filteredReader=CosmicSession.Create(manager,adapter,access,filteredModel).Result)
        {
            var none=await Timed("tph-filtered-query",()=>filteredReader.Linq<TphDogHolon>().Result.AsCosmicNoTracking().ToCosmicListAsync());
            Assert.IsFalse(none.IsError,none.Message+none.Exception);Assert.AreEqual(0,none.Result.Count);
        }
        using(var bypass=CosmicSession.Create(manager,adapter,access,filteredModel.WithoutQueryFilters()).Result)
        {
            var bypassed=await Timed("tph-bypass-query",()=>bypass.Linq<TphDogHolon>().Result.AsCosmicNoTracking().ToCosmicListAsync());
            Assert.IsFalse(bypassed.IsError,bypassed.Message+bypassed.Exception);
            CollectionAssert.AreEquivalent(new[]{dog.Id,puppy.Id},bypassed.Result.Select(row=>row.Id).ToArray());
        }
        checks++;

        // 7. An undeclared concrete subtype is refused before any write.
        var mystery=New<TphUndeclaredHolon>("mystery");
        using(var refusing=CosmicSession.Create(manager,adapter,access,model).Result)
        {
            Assert.IsFalse(refusing.Add(mystery).IsError);
            var refused=await Timed("tph-undeclared-save",()=>refusing.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsTrue(refused.IsError);Assert.AreEqual("COSMIC_MODEL_DISCRIMINATOR_UNMAPPED",refused.ErrorCode);
        }
        var absent=await Timed("tph-undeclared-read",()=>reader.Linq<TphAnimalHolon>().Result.Where(row=>row.Id==mystery.Id).AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(absent.IsError,absent.Message+absent.Exception);Assert.AreEqual(0,absent.Result.Count);
        owned.Remove(mystery.Id);
        checks++;

        // 8. Updating a derived row through a root query keeps its type and discriminator.
        var tracked=(TphPuppyHolon)byId[puppy.Id];tracked.AgeWeeks=10;tracked.Sound="woof";
        var updated=await Timed("tph-update",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        using var verifier=CosmicSession.Create(manager,adapter,access,model).Result;
        var reread=await Timed("tph-update-read",()=>verifier.Linq<TphPuppyHolon>().Result.Where(row=>row.AgeWeeks==10).AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(reread.IsError,reread.Message+reread.Exception);
        Assert.AreEqual(puppy.Id,reread.Result.Single().Id);Assert.AreEqual("woof",reread.Result.Single().Sound);
        Assert.AreEqual("puppy",reread.Result.Single().MetaData["tph_kind"]?.ToString());
        checks++;

        return new NativeFixtureJourneyProof(checks,2,new Dictionary<string,bool>{
            ["modelAdmissionRefusesInvalidHierarchies"]=true,["discriminatorStoredNatively"]=true,
            ["rootQueryMaterializesConcreteTypes"]=true,["midLevelQueryReturnsSubtypesNatively"]=true,
            ["leafQueriesExact"]=true,["restrictionSurvivesFilterBypass"]=true,
            ["undeclaredSubtypeRefused"]=true,["derivedUpdateKeepsTypeAndDiscriminator"]=true});
    }
}
