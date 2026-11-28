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
    public sealed class XmlScalarCollectionHolon:Holon
    {
        public List<string?> Tags {get;set;}=new();
        public long[] Numbers {get;set;}=Array.Empty<long>();
        public HashSet<int> Seen {get;set;}=new();
    }
    [DataTestMethod] [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeXmlScalarBagsAndSetsUseOwnedNativeCollections(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyXmlScalarCollections,
            nativeJourneySuffix:"-xml-scalar-collection-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyXmlScalarCollections(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;var actor=Guid.NewGuid();
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var watch=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,watch.ElapsedMilliseconds);}}
        const string xml="""
            <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'><class name='XmlCollections'>
              <bag name='Tags'><element type='String'/></bag>
              <bag name='Numbers'><element type='Int64'/></bag>
              <set name='Seen'><element type='Int32'/></set>
            </class></hibernate-mapping>
            """;
        var contracts=new Dictionary<string,Type>{{"XmlCollections",typeof(XmlScalarCollectionHolon)}};
        var builder=new CosmicModelBuilder();var imported=builder.ImportNHibernateXml(xml,contracts);
        Assert.IsFalse(imported.IsError,imported.Message+imported.Exception);
        Assert.IsTrue(builder.ImportNHibernateXml(xml.Replace("type='Int64'","type='Guid'"),contracts).IsError);
        Assert.IsTrue(new CosmicModelBuilder().ImportNHibernateXml(xml.Replace("<set name='Seen'>","<bag name='Seen'>").Replace("</set>","</bag>"),contracts).IsError);
        Assert.IsTrue(new CosmicModelBuilder().ImportNHibernateXml(xml.Replace("<bag name='Tags'>","<bag name='Tags' table='ignored'>"),contracts).IsError);
        var model=builder.Build();Assert.IsFalse(model.IsError,model.Message+model.Exception);
        using var writer=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var row=new XmlScalarCollectionHolon{Id=Guid.NewGuid(),Name="xml collections",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true,
            Tags=new(){"a",null,"a"},Numbers=new[]{9007199254740993L,2L},Seen=new(){1,1,2}};
        owned.Add(row.Id);Assert.IsFalse(writer.Add(row).IsError);
        var saved=await Timed("xml-collection-create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        Assert.IsTrue(row.MetaData.ContainsKey("Tags"));Assert.IsFalse(row.MetaData["Tags"] is string);
        using var reader=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var query=reader.Query<XmlScalarCollectionHolon>().Result.Where(x=>x.Id==row.Id).Result;
        var loaded=await Timed("xml-collection-roundtrip",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);Assert.AreEqual(1,loaded.Result.Count);
        CollectionAssert.AreEqual(new string?[]{"a",null,"a"},loaded.Result[0].Tags);
        CollectionAssert.AreEqual(row.Numbers,loaded.Result[0].Numbers);Assert.IsTrue(loaded.Result[0].Seen.SetEquals(new[]{1,2}));
        var version=row.Version;row.Tags.Clear();row.Numbers=Array.Empty<long>();row.Seen.Add(3);
        var updated=await Timed("xml-collection-update",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);Assert.AreEqual(version+1,row.Version);
        var changed=await Timed("xml-collection-updated-read",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(changed.IsError,changed.Message+changed.Exception);Assert.AreEqual(0,changed.Result.Single().Tags.Count);
        Assert.AreEqual(0,changed.Result[0].Numbers.Length);Assert.IsTrue(changed.Result[0].Seen.SetEquals(new[]{1,2,3}));
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model.Result).Result;
        var hidden=await Timed("xml-collection-foreign",()=>foreign.ReadAsync(foreign.Query<XmlScalarCollectionHolon>().Result.Where(x=>x.Id==row.Id).Result,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        return new NativeFixtureJourneyProof(6,2,new Dictionary<string,bool>{["xmlOwnedBagAndSetMapping"]=true,["orderedDuplicatesAndNulls"]=true,
            ["exactInt64AndSetElements"]=true,["acknowledgedCollectionUpdate"]=true,["atomicUnsupportedMappingRefusal"]=true,["callerIsolation"]=true});
    }
}
