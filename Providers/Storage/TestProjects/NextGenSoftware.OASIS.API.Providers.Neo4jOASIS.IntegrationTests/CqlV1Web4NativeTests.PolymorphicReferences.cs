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
    public sealed class PolyCommentHolon:Holon
    {
        public Guid SubjectId {get;set;}
        public string? Text {get;set;}
        public string? Status {get;set;}
    }
    public sealed class PolyArticleHolon:Holon { public string? Title {get;set;} }
    public sealed class PolyVideoHolon:Holon { public int Seconds {get;set;} }
    public sealed class PolyUndeclaredHolon:Holon {}

    // A16: polymorphic ("any") associations whose target type is stored with the reference.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativePolymorphicReferencesLoadDeclaredTargetTypes(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyPolymorphicReferenceJourney,nativeJourneySuffix:"-polymorphic-reference-native.json");

    private static CosmicModel PolyModel(bool withFilter)
    {
        var builder=new CosmicModelBuilder();
        builder.Entity<PolyCommentHolon>().Property(row=>row.SubjectId).HasMetadataKey("poly_subject_id");
        builder.Entity<PolyCommentHolon>().Property(row=>row.Text).HasMetadataKey("poly_text");
        builder.Entity<PolyCommentHolon>().Property(row=>row.Status).HasMetadataKey("poly_status");
        builder.Entity<PolyCommentHolon>().Property(row=>row.Status).HasDefaultValue("new");
        builder.Entity<PolyArticleHolon>().Property(row=>row.Title).HasMetadataKey("poly_title");
        builder.Entity<PolyVideoHolon>().Property(row=>row.Seconds).HasMetadataKey("poly_seconds");
        builder.Entity<PolyCommentHolon>().HasPolymorphicReference(row=>row.SubjectId,"poly_subject_type")
            .HasTarget<PolyArticleHolon>("article").HasTarget<PolyVideoHolon>("video");
        if(withFilter)builder.Entity<PolyCommentHolon>().HasQueryFilter(row=>row.Text!="filtered");
        return builder.Build().Result;
    }

    private static async Task<NativeFixtureJourneyProof> VerifyPolymorphicReferenceJourney(
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
        T New<T>(string name,Guid owner) where T:Holon,new()
        {
            var row=new T{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=owner,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        var checks=0;

        // 1. Model admission: Guid references only; unique target types and values.
        var bad=new CosmicModelBuilder();
        Assert.ThrowsException<ArgumentException>(()=>bad.Entity<PolyCommentHolon>().HasPolymorphicReference(row=>row.Id,"poly_self"));
        var targets=new CosmicModelBuilder().Entity<PolyCommentHolon>().HasPolymorphicReference(row=>row.SubjectId,"poly_subject_type")
            .HasTarget<PolyArticleHolon>("article");
        Assert.ThrowsException<ArgumentException>(()=>targets.HasTarget<PolyVideoHolon>("article"));
        var model=PolyModel(false);
        checks++;

        // 2. References record their target type natively.
        var article=New<PolyArticleHolon>("article",actor);article.Title="COSMIC";
        var video=New<PolyVideoHolon>("video",actor);video.Seconds=42;
        var onArticle=New<PolyCommentHolon>("on article",actor);onArticle.Text="nice read";
        var onVideo=New<PolyCommentHolon>("on video",actor);onVideo.Text="nice clip";
        using(var writer=CosmicSession.Create(manager,adapter,access,model).Result)
        {
            foreach(var row in new IHolon[]{article,video,onArticle,onVideo})Assert.IsFalse(writer.Add((dynamic)row).IsError);
            Assert.IsFalse(writer.SetPolymorphicReference(onArticle,row=>row.SubjectId,article).IsError);
            Assert.IsFalse(writer.SetPolymorphicReference(onVideo,row=>row.SubjectId,video).IsError);
            var saved=await Timed("poly-create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        }
        using(var raw=CosmicSession.Create(manager,adapter,access).Result)
        {
            var ids=new[]{onArticle.Id,onVideo.Id};
            var stored=await Timed("poly-raw-read",()=>raw.Linq<Holon>().Result.Where(row=>ids.Contains(row.Id)).AsCosmicNoTracking().ToCosmicListAsync());
            Assert.IsFalse(stored.IsError,stored.Message+stored.Exception);
            var kinds=stored.Result.ToDictionary(row=>row.Id,row=>row.MetaData["poly_subject_type"]?.ToString());
            Assert.AreEqual("article",kinds[onArticle.Id]);Assert.AreEqual("video",kinds[onVideo.Id]);
        }
        checks++;

        // 3. Loading materializes each target as its declared concrete type, through the identity map.
        using var reader=CosmicSession.Create(manager,adapter,access,model).Result;
        var comments=await Timed("poly-comments",()=>reader.Linq<PolyCommentHolon>().Result.Where(row=>row.Text!=null).ToCosmicListAsync());
        Assert.IsFalse(comments.IsError,comments.Message+comments.Exception);Assert.AreEqual(2,comments.Result.Count);
        var readArticle=comments.Result.Single(row=>row.Id==onArticle.Id);
        var readVideo=comments.Result.Single(row=>row.Id==onVideo.Id);
        var loadedArticle=await Timed("poly-load-article",()=>reader.LoadPolymorphicReferenceAsync(readArticle,row=>row.SubjectId));
        Assert.IsFalse(loadedArticle.IsError,loadedArticle.Message+loadedArticle.Exception);
        Assert.AreEqual("COSMIC",((PolyArticleHolon)loadedArticle.Result).Title);
        var loadedVideo=await Timed("poly-load-video",()=>reader.LoadPolymorphicReferenceAsync(readVideo,row=>row.SubjectId));
        Assert.IsFalse(loadedVideo.IsError,loadedVideo.Message+loadedVideo.Exception);
        Assert.AreEqual(42,((PolyVideoHolon)loadedVideo.Result).Seconds);
        var again=await Timed("poly-load-again",()=>reader.LoadPolymorphicReferenceAsync(readVideo,row=>row.SubjectId));
        Assert.AreSame(loadedVideo.Result,again.Result);
        checks++;

        // 4. The stored target type is natively queryable.
        var videoComments=await Timed("poly-by-type",()=>reader.Linq<PolyCommentHolon>().Result
            .Where(row=>CosmicQueryMetadata.Value<string>(row,"poly_subject_type")=="video").AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(videoComments.IsError,videoComments.Message+videoComments.Exception);
        Assert.AreEqual(onVideo.Id,videoComments.Result.Single().Id);
        checks++;

        // 5. A reference assigned without its type is refused before any write.
        readArticle.SubjectId=video.Id;
        readArticle.MetaData.Remove("poly_subject_type");
        var refused=await Timed("poly-untyped-save",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(refused.IsError);Assert.AreEqual("COSMIC_MODEL_POLYMORPHIC_REFERENCE_INVALID",refused.ErrorCode);
        Assert.IsFalse((await reader.ReloadAsync(readArticle)).IsError);
        Assert.AreEqual(article.Id,readArticle.SubjectId);
        checks++;

        // 6. Undeclared target types and unsaved targets are refused at assignment.
        Assert.AreEqual("COSMIC_POLYMORPHIC_TARGET_UNDECLARED",
            reader.SetPolymorphicReference(readArticle,row=>row.SubjectId,new PolyUndeclaredHolon{Id=Guid.NewGuid()}).ErrorCode);
        Assert.AreEqual("COSMIC_POLYMORPHIC_TARGET_INVALID",
            reader.SetPolymorphicReference(readArticle,row=>row.SubjectId,new PolyVideoHolon()).ErrorCode);
        checks++;

        // 7. Clearing a reference removes the stored type; loading it returns nothing.
        Assert.IsFalse(reader.SetPolymorphicReference(readVideo,row=>row.SubjectId,null).IsError);
        var cleared=await Timed("poly-clear",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(cleared.IsError,cleared.Message+cleared.Exception);Assert.IsTrue(cleared.IsSaved);
        var empty=await reader.LoadPolymorphicReferenceAsync(readVideo,row=>row.SubjectId);
        Assert.IsFalse(empty.IsError);Assert.IsNull(empty.Result);
        var untyped=await Timed("poly-cleared-read",()=>reader.Linq<PolyCommentHolon>().Result
            .Where(row=>CosmicQueryMetadata.Value<string>(row,"poly_subject_type")=="video").AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(untyped.IsError,untyped.Message+untyped.Exception);Assert.AreEqual(0,untyped.Result.Count);
        checks++;

        // 8. Targets are read under the caller's access policy; filter-bypass models keep all model structure.
        var stranger=Guid.NewGuid();
        var hidden=New<PolyArticleHolon>("hidden",stranger);hidden.Title="private";
        using(var strangerWriter=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(stranger).Result,model).Result)
        {
            Assert.IsFalse(strangerWriter.Add(hidden).IsError);
            var savedHidden=await Timed("poly-stranger-target",()=>strangerWriter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(savedHidden.IsError,savedHidden.Message+savedHidden.Exception);
        }
        using(var bypass=CosmicSession.Create(manager,adapter,access,PolyModel(true).WithoutQueryFilters()).Result)
        {
            var pointing=New<PolyCommentHolon>("points at hidden",actor);pointing.Text="filtered";
            Assert.IsFalse(bypass.Add(pointing).IsError);
            Assert.IsFalse(bypass.SetPolymorphicReference(pointing,row=>row.SubjectId,hidden).IsError);
            var savedPointing=await Timed("poly-bypass-save",()=>bypass.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(savedPointing.IsError,savedPointing.Message+savedPointing.Exception);
            Assert.AreEqual("new",pointing.Status,"Filter-bypass models must keep model defaults.");
            var invisible=await Timed("poly-hidden-load",()=>bypass.LoadPolymorphicReferenceAsync(pointing,row=>row.SubjectId));
            Assert.IsFalse(invisible.IsError,invisible.Message+invisible.Exception);Assert.IsNull(invisible.Result);
        }
        checks++;

        return new NativeFixtureJourneyProof(checks,5,new Dictionary<string,bool>{
            ["modelAdmissionGuidReferencesOnly"]=true,["targetTypeStoredNatively"]=true,
            ["targetsMaterializeAsDeclaredTypes"]=true,["targetTypeNativelyQueryable"]=true,
            ["untypedReferenceRefused"]=true,["undeclaredOrUnsavedTargetRefused"]=true,
            ["clearedReferenceRemovesType"]=true,["targetsReadUnderCallerAccessAndBypassKeepsStructure"]=true});
    }
}
