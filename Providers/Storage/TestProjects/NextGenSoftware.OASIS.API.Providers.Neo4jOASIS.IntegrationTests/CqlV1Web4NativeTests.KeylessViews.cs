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
    public sealed class ViewOrderHolon:Holon
    {
        public string? Customer {get;set;}
        public int Quantity {get;set;}
        public bool Shipped {get;set;}
    }
    public sealed class ShippedOrderView
    {
        public string? Customer {get;set;}
        public int Quantity {get;set;}
    }
    public sealed class ReplacedSourceView { public int Value {get;set;} }

    // M23: keyless, read-only view entities defined by a model query.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeKeylessViewsRunDefiningQueriesReadOnly(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyKeylessViewJourney,nativeJourneySuffix:"-keyless-view-native.json");

    private static CosmicModelBuilder ViewBuilder(bool hiddenFilter)
    {
        var builder=new CosmicModelBuilder();
        builder.Entity<ViewOrderHolon>().Property(row=>row.Customer).HasMetadataKey("view_customer");
        builder.Entity<ViewOrderHolon>().Property(row=>row.Quantity).HasMetadataKey("view_quantity");
        builder.Entity<ViewOrderHolon>().Property(row=>row.Shipped).HasMetadataKey("view_shipped");
        if(hiddenFilter)builder.Entity<ViewOrderHolon>().HasQueryFilter(row=>row.Customer!="hidden");
        builder.KeylessEntity<ShippedOrderView>().ToQuery<ViewOrderHolon>(orders=>orders
            .Where(order=>order.Shipped)
            .Select(order=>new ShippedOrderView{Customer=order.Customer,Quantity=order.Quantity}));
        return builder;
    }

    private static async Task<NativeFixtureJourneyProof> VerifyKeylessViewJourney(
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

        // 1. Model admission: a holon cannot be keyless; one defining query per view.
        Assert.ThrowsException<ArgumentException>(()=>new CosmicModelBuilder().KeylessEntity<ViewOrderHolon>());
        var twice=ViewBuilder(false);
        Assert.ThrowsException<ArgumentException>(()=>twice.KeylessEntity<ShippedOrderView>()
            .ToQuery<ViewOrderHolon>(orders=>orders.Select(order=>new ShippedOrderView{Quantity=order.Quantity})));
        var model=ViewBuilder(true).Build();Assert.IsFalse(model.IsError,model.Message);
        CollectionAssert.AreEqual(new[]{typeof(ShippedOrderView)},model.Result.ViewTypes.ToArray());
        checks++;

        // 2. Seed source entities natively.
        ViewOrderHolon New(string customer,int quantity,bool shipped)
        {
            var row=new ViewOrderHolon{Id=Guid.NewGuid(),Name="order "+customer,Customer=customer,Quantity=quantity,Shipped=shipped,
                CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        var ada=New("ada",7,true);var bob=New("bob",3,true);var cy=New("cy",9,false);var hidden=New("hidden",5,true);
        using(var writer=CosmicSession.Create(manager,adapter,access,model.Result).Result)
        {
            foreach(var row in new[]{ada,bob,cy,hidden})Assert.IsFalse(writer.Add(row).IsError);
            var saved=await Timed("view-seed",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        }
        checks++;

        // 3. The view runs its defining query natively, under the model's query filter.
        using var reader=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var view=reader.View<ShippedOrderView>();Assert.IsFalse(view.IsError,view.ErrorCode+": "+view.Message);
        var rows=await Timed("view-read",()=>view.Result.ToCosmicListAsync());
        Assert.IsFalse(rows.IsError,rows.Message+rows.Exception);
        CollectionAssert.AreEquivalent(new[]{"ada:7","bob:3"},rows.Result.Select(row=>row.Customer+":"+row.Quantity).ToArray());
        checks++;

        // 4. Callers compose further operators onto the view.
        var top=await Timed("view-composed",()=>reader.View<ShippedOrderView>().Result
            .OrderByDescending(row=>row.Quantity).Take(1).ToCosmicListAsync());
        Assert.IsFalse(top.IsError,top.Message+top.Exception);
        Assert.AreEqual("ada",top.Result.Single().Customer);
        checks++;

        // 5. Views are untracked: a pending local edit to a tracked source entity does not leak into the view.
        var tracked=await Timed("view-tracked-source",()=>reader.Linq<ViewOrderHolon>().Result.Where(row=>row.Id==bob.Id).ToCosmicListAsync());
        Assert.IsFalse(tracked.IsError,tracked.Message+tracked.Exception);
        tracked.Result.Single().Quantity=99;
        var fresh=await Timed("view-after-local-edit",()=>reader.View<ShippedOrderView>().Result.ToCosmicListAsync());
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);
        Assert.AreEqual(3,fresh.Result.Single(row=>row.Customer=="bob").Quantity);
        Assert.AreEqual(CosmicEntityState.Modified,reader.State(tracked.Result.Single()).Result);
        checks++;

        // 6. Filter bypass reaches the view's source; another actor sees nothing.
        using(var bypass=CosmicSession.Create(manager,adapter,access,model.Result.WithoutQueryFilters()).Result)
        {
            var all=await Timed("view-bypass",()=>bypass.View<ShippedOrderView>().Result.ToCosmicListAsync());
            Assert.IsFalse(all.IsError,all.Message+all.Exception);Assert.AreEqual(3,all.Result.Count);
        }
        using(var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model.Result).Result)
        {
            var none=await Timed("view-foreign",()=>foreign.View<ShippedOrderView>().Result.ToCosmicListAsync());
            Assert.IsFalse(none.IsError,none.Message+none.Exception);Assert.AreEqual(0,none.Result.Count);
        }
        checks++;

        // 7. Undeclared views and model-less sessions are refused.
        Assert.AreEqual("COSMIC_VIEW_NOT_DECLARED",reader.View<ReplacedSourceView>().ErrorCode);
        using(var plain=CosmicSession.Create(manager,adapter,access).Result)
            Assert.AreEqual("COSMIC_VIEW_NOT_DECLARED",plain.View<ShippedOrderView>().ErrorCode);
        checks++;

        // 8. A definition must compose the supplied entity set; replacing it with local data is refused.
        var replaced=new CosmicModelBuilder();
        replaced.KeylessEntity<ReplacedSourceView>().ToQuery<ViewOrderHolon>(_=>new[]{new ReplacedSourceView{Value=1}}.AsQueryable());
        using(var replacing=CosmicSession.Create(manager,adapter,access,replaced.Build().Result).Result)
            Assert.AreEqual("COSMIC_VIEW_DEFINITION_INVALID",replacing.View<ReplacedSourceView>().ErrorCode);
        checks++;

        return new NativeFixtureJourneyProof(checks,1,new Dictionary<string,bool>{
            ["modelAdmissionKeylessOnly"]=true,["sourceSeededNatively"]=true,
            ["definingQueryRunsNativelyUnderFilters"]=true,["viewComposesFurtherOperators"]=true,
            ["viewRowsUntracked"]=true,["filterBypassAndCallerVisibility"]=true,
            ["undeclaredViewsRefused"]=true,["sourceReplacementRefused"]=true});
    }
}
