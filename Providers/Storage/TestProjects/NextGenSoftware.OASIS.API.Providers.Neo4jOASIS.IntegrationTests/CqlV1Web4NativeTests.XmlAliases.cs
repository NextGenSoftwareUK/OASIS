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
    public sealed class XmlAliasStock
    {public Guid Id {get;set;}public string Label {get;set;}="";public int Quantity {get;set;}public decimal Price {get;set;}public int? Optional {get;set;}public bool Enabled {get;set;}}
    public sealed class XmlAliasAssigned {public string Code {get;set;}="";public string Label {get;set;}="";}
    public sealed class XmlAliasLegacyHolon:Holon {public int ExtraScore {get;set;}}
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeXmlScalarAliasesAndInlineGeneratorsPreserveBindings(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyXmlAliases,
            nativeJourneySuffix:"-xml-aliases-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyXmlAliases(IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var timer=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}}
        var xml="""
            <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'><class name='Aliases'>
              <id name='Id' type='System.Guid' column='Id' generator='guid'/>
              <property name='Label' type='System.String' column='alias_label'/>
              <property name='Quantity' type='integer' column='alias_quantity'/>
              <property name='Price' type='System.Decimal' column='alias_price' precision='8' scale='2'/>
              <property name='Optional' type='$NULLABLE' column='alias_optional'/>
              <property name='Enabled' type='boolean' column='alias_enabled'/>
            </class></hibernate-mapping>
            """;
        xml=xml.Replace("$NULLABLE",typeof(int?).FullName!);
        var contract="M04_alias_"+Guid.NewGuid().ToString("N");var builder=new CosmicPocoModelBuilder<XmlAliasStock>(contract);
        var imported=builder.ImportNHibernateXml(xml,"Aliases");Assert.IsFalse(imported.IsError,imported.Message+imported.Exception);
        Assert.IsTrue(builder.ImportNHibernateXml(xml.Replace("generator='guid'/>","generator='guid'><generator class='assigned'/></id>"),"Aliases").IsError);
        Assert.IsTrue(builder.ImportNHibernateXml(xml.Replace("System.String","System.IO.FileInfo"),"Aliases").IsError);
        var model=builder.Build();Assert.IsFalse(model.IsError,model.Message+model.Exception);
        using var writer=CosmicPocoContext<XmlAliasStock>.Create(manager,adapter,access,model.Result).Result;
        var stock=new XmlAliasStock{Label="aliases",Quantity=7,Price=12.25m,Optional=null,Enabled=true};Assert.IsFalse(writer.Add(stock).IsError);
        var id=writer.Identity(stock).Result;owned.Add(id);Assert.AreEqual(Guid.Empty,stock.Id);
        var saved=await Timed("save-aliases",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);Assert.AreEqual(id,stock.Id);
        using var reader=CosmicPocoContext<XmlAliasStock>.Create(manager,adapter,access,model.Result).Result;
        var read=await Timed("query-project-aliases",()=>reader.Linq().Result.Where(row=>row.Quantity==7&&row.Optional==null&&row.Enabled==true)
            .Select(row=>new{row.Id,row.Label,row.Quantity,row.Price,row.Optional,row.Enabled}).ToCosmicListAsync());
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(id,read.Result.Single().Id);Assert.AreEqual(12.25m,read.Result[0].Price);
        const string assignedXml="""
            <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'><class name='AssignedAliases'>
              <id name='Code' type='System.String' column='native_code' generator='assigned'/>
              <property name='Label' type='string' column='native_label'/>
            </class></hibernate-mapping>
            """;
        var assignedBuilder=new CosmicPocoModelBuilder<XmlAliasAssigned>(contract+"_assigned");
        Assert.IsFalse(assignedBuilder.ImportNHibernateXml(assignedXml,"AssignedAliases").IsError);
        using var assigned=CosmicPocoContext<XmlAliasAssigned>.Create(manager,adapter,access,assignedBuilder.Build().Result).Result;
        var item=new XmlAliasAssigned{Code="external-7",Label="assigned aliases"};Assert.IsFalse(assigned.Add(item).IsError);owned.Add(assigned.Identity(item).Result);
        var assignedSaved=await Timed("save-inline-assigned",()=>assigned.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(assignedSaved.IsError,assignedSaved.Message+assignedSaved.Exception);Assert.IsTrue(assignedSaved.IsSaved);
        var found=await Timed("lookup-inline-assigned",()=>assigned.FindByPrimaryKeyAsync(new object[]{"external-7"}));
        Assert.IsFalse(found.IsError,found.Message+found.Exception);Assert.AreSame(item,found.Result);
        using var foreign=CosmicPocoContext<XmlAliasStock>.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,model.Result).Result;
        var hidden=await Timed("foreign-alias-visibility",()=>foreign.Linq().Result.ToCosmicListAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        const string legacyXml="""
            <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'><class name='LegacyAliases'>
              <id name='Id' type='System.Guid' generator='assigned'/>
              <property name='ExtraScore' type='int' column='legacy_score'/>
            </class></hibernate-mapping>
            """;
        var legacyBuilder=new CosmicModelBuilder();var legacyImport=legacyBuilder.ImportNHibernateXml(legacyXml,new Dictionary<string,Type>{{"LegacyAliases",typeof(XmlAliasLegacyHolon)}});
        Assert.IsFalse(legacyImport.IsError,legacyImport.Message+legacyImport.Exception);var legacyModel=legacyBuilder.Build();Assert.IsFalse(legacyModel.IsError,legacyModel.Message);
        using var legacy=CosmicSession.Create(manager,adapter,access,legacyModel.Result).Result;
        var oldStyle=new XmlAliasLegacyHolon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,IsActive=true,IsPublic=false,ExtraScore=11};owned.Add(oldStyle.Id);
        Assert.IsFalse(legacy.Add(oldStyle).IsError);
        var legacySaved=await Timed("save-holon-aliases",()=>legacy.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(legacySaved.IsError,legacySaved.Message+legacySaved.Exception);Assert.IsTrue(legacySaved.IsSaved);
        var legacyId=oldStyle.Id;
        var legacyRead=await Timed("query-holon-aliases",()=>legacy.ReadAsync(legacy.Query<XmlAliasLegacyHolon>().Result.Where(row=>row.Id==legacyId&&row.ExtraScore==11).Result));
        Assert.IsFalse(legacyRead.IsError,legacyRead.Message+legacyRead.Exception);Assert.AreEqual(1,legacyRead.Result.Count);
        return new NativeFixtureJourneyProof(7,3,new Dictionary<string,bool>{
            ["approvedClrTypeNames"]=true,["nhibernatePrimitiveAliases"]=true,["nullableClrBinding"]=true,
            ["inlineGeneratedGuid"]=true,["inlineAssignedLogicalKey"]=true,["atomicMixedGeneratorAndUnknownTypeRefusal"]=true,
            ["existingHolonXmlPathAndCallerPolicy"]=true});
    }
}
