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
    public sealed class XmlVersionHolon:Holon {public int Score {get;set;}}
    private const string VersionXml="""
        <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'>
          <class name='Versioned' optimistic-lock='version'>
            <id name='Id' type='Guid' generator='assigned'/>
            <version name='Version' column='Version' type='Int32' unsaved-value='0' generated='never'/>
            <property name='Score' column='xml_version_score' type='Int32'/>
          </class>
        </hibernate-mapping>
        """;
    private static Dictionary<string,Type> VersionContracts()=>new(){{"Versioned",typeof(XmlVersionHolon)}};
    [TestMethod] public void XmlVersionDeclarationsPreserveCanonicalConcurrencyAndAtomicImport()
    {
        var builder=new CosmicModelBuilder();
        foreach(var xml in new[]{VersionXml.Replace("column='Version'","column='renamed'"),
            VersionXml.Replace("unsaved-value='0'","unsaved-value='7'"),VersionXml.Replace("generated='never'","generated='always'"),
            VersionXml.Replace("optimistic-lock='version'","optimistic-lock='none'")})
        {Assert.IsTrue(builder.ImportNHibernateXml(xml,VersionContracts()).IsError);Assert.IsTrue(builder.Build().IsError);}
        var valid=builder.ImportNHibernateXml(VersionXml,VersionContracts());Assert.IsFalse(valid.IsError,valid.Message+valid.Exception);
        Assert.IsFalse(builder.Build().IsError);
        var columnElement=VersionXml.Replace("column='Version' ","").Replace("generated='never'/>","generated='never'><column name='Version' not-null='true'/></version>");
        var alternate=new CosmicModelBuilder();Assert.IsFalse(alternate.ImportNHibernateXml(columnElement,VersionContracts()).IsError);
    }
    [DataTestMethod] [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeXmlVersionMappingRejectsStaleWrites(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyNativeXmlVersion,
            nativeJourneySuffix:"-xml-version-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyNativeXmlVersion(IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var builder=new CosmicModelBuilder();var imported=builder.ImportNHibernateXml(VersionXml,VersionContracts());
        Assert.IsFalse(imported.IsError,imported.Message+imported.Exception);var model=builder.Build();Assert.IsFalse(model.IsError,model.Message);
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> read)
        {var watch=System.Diagnostics.Stopwatch.StartNew();try{return await read();}finally{timings.Add(name,watch.ElapsedMilliseconds);}}
        using var writer=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var entity=new XmlVersionHolon{Id=Guid.NewGuid(),Name="xml version",Score=10,CreatedByAvatarId=actor,
            IsActive=true,IsPublic=false,IsNewHolon=true};owned.Add(entity.Id);var id=entity.Id;
        Assert.IsFalse(writer.Add(entity).IsError);var created=await Timed("create-versioned-xml",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);var revision=entity.Version;Assert.IsTrue(revision>0);
        using var stale=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var old=await Timed("read-stale-version",()=>stale.ReadAsync(stale.Query<XmlVersionHolon>().Result.Where(row=>row.Id==id).Result));
        Assert.IsFalse(old.IsError,old.Message+old.Exception);Assert.AreEqual(1,old.Result.Count);Assert.AreEqual(revision,old.Result[0].Version);
        entity.Score=20;var winner=await Timed("write-next-version",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(winner.IsError,winner.Message+winner.Exception);Assert.IsTrue(winner.IsSaved);Assert.AreEqual(revision+1,entity.Version);
        old.Result[0].Score=99;var conflict=await Timed("refuse-stale-xml-version",()=>stale.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(conflict.IsError);Assert.IsFalse(conflict.IsSaved);
        using var fresh=CosmicSession.Create(manager,adapter,access,model.Result).Result;
        var rows=await Timed("read-preserved-winner",()=>fresh.ReadAsync(fresh.Query<XmlVersionHolon>().Result.Where(row=>row.Id==id).Result,trackResults:false));
        Assert.IsFalse(rows.IsError,rows.Message+rows.Exception);Assert.AreEqual(1,rows.Result.Count);Assert.AreEqual(20,rows.Result[0].Score);Assert.AreEqual(revision+1,rows.Result[0].Version);
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model.Result).Result;
        var hidden=await Timed("foreign-version-policy",()=>foreign.ReadAsync(foreign.Query<XmlVersionHolon>().Result.Where(row=>row.Id==id).Result,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        return new NativeFixtureJourneyProof(5,2,new Dictionary<string,bool>{["xmlCanonicalVersionAck"]=true,["nativeVersionIncrement"]=true,
            ["nativeStaleWriteRejected"]=true,["winnerPreserved"]=true,["callerPolicyPreserved"]=true});
    }
}
