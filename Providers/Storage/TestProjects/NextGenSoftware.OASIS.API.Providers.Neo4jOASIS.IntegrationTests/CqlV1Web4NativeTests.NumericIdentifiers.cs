using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    public sealed class XmlNumericStock {public long Number {get;set;}public string Label {get;set;}="";}
    private sealed class TerminalNumericBlock:ICosmicSequenceStorageProvider
    {
        private int calls;
        public Task<OASISResult<CosmicSequenceBlock>> ReserveSequenceBlockAsync(string name,int blockSize=1,CancellationToken token=default)
            =>Task.FromResult(++calls==1?new OASISResult<CosmicSequenceBlock>(new(name,long.MaxValue,long.MaxValue))
                :CosmicSequences.Error("COSMIC_SEQUENCE_EXHAUSTED","Boundary control exhausted."));
    }
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeXmlSequenceGenerationAllocatesUniqueBusinessIdentifiers(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,nativeJourney:VerifyXmlNumericGeneration,
            nativeJourneySuffix:"-numeric-identifiers-native.json");
    private static async Task<NativeFixtureJourneyProof> VerifyXmlNumericGeneration(IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        Assert.IsInstanceOfType<ICosmicSequenceStorageProvider>(provider);
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        async Task<TValue> Timed<TValue>(string name,Func<Task<TValue>> action)
        {var timer=System.Diagnostics.Stopwatch.StartNew();try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}}
        var contract="M04_num_"+Guid.NewGuid().ToString("N");var sequence="m04_seq_"+Guid.NewGuid().ToString("N");
        var xml="""
            <hibernate-mapping xmlns='urn:nhibernate-mapping-2.2'><class name='NumericStock'>
              <id name='Number' type='Int64' column='native_number'><generator class='sequence'><param name='sequence'>$NAME</param></generator></id>
              <property name='Label' type='String' column='native_label'/>
            </class></hibernate-mapping>
            """;
        xml=xml.Replace("$NAME",sequence);
        var builder=new CosmicPocoModelBuilder<XmlNumericStock>(contract);var imported=builder.ImportNHibernateXml(xml,"NumericStock");
        Assert.IsFalse(imported.IsError,imported.Message+imported.Exception);var model=builder.Build();Assert.IsFalse(model.IsError,model.Message+model.Exception);
        Assert.AreEqual(sequence,model.Result.IdentifierGeneration.SequenceName);Assert.AreEqual(1,model.Result.IdentifierGeneration.BlockSize);
        using var first=CosmicPocoContext<XmlNumericStock>.Create(manager,adapter,access,model.Result).Result;
        using var second=CosmicPocoContext<XmlNumericStock>.Create(manager,adapter,access,model.Result).Result;
        var a=new XmlNumericStock{Label="first"};var b=new XmlNumericStock{Label="second"};
        var syncRefusal=first.Add(a);Assert.IsTrue(syncRefusal.IsError);Assert.AreEqual("COSMIC_NATIVE_IDENTIFIER_ASYNC_REQUIRED",syncRefusal.ErrorCode);
        var registeredA=await Timed("reserve-first-key",()=>first.AddAsync(a));Assert.IsFalse(registeredA.IsError,registeredA.Message+registeredA.Exception);owned.Add(first.Identity(a).Result);
        var registeredB=await Timed("reserve-second-key",()=>second.AddAsync(b));Assert.IsFalse(registeredB.IsError,registeredB.Message+registeredB.Exception);owned.Add(second.Identity(b).Result);
        Assert.AreEqual(1L,a.Number);Assert.AreEqual(2L,b.Number);
        var savedA=await Timed("save-first-key",()=>first.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(savedA.IsError,savedA.Message+savedA.Exception);Assert.IsTrue(savedA.IsSaved);
        var savedB=await Timed("save-second-key",()=>second.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(savedB.IsError,savedB.Message+savedB.Exception);Assert.IsTrue(savedB.IsSaved);
        using var reader=CosmicPocoContext<XmlNumericStock>.Create(manager,adapter,access,model.Result).Result;
        var found=await Timed("lookup-generated-key",()=>reader.FindByPrimaryKeyAsync(new object[]{1L}));
        Assert.IsFalse(found.IsError,found.Message+found.Exception);Assert.AreEqual("first",found.Result.Label);
        var readonlyBuilder=new CosmicPocoModelBuilder<XmlNumericStock>(contract);
        Assert.IsFalse(readonlyBuilder.ImportNHibernateXml(xml.Replace("<class name='NumericStock'>","<class name='NumericStock' mutable='false'>"),"NumericStock").IsError);
        using var readonlyContext=CosmicPocoContext<XmlNumericStock>.Create(manager,adapter,access,readonlyBuilder.Build().Result).Result;
        var readonlyRefusal=await Timed("refuse-readonly-allocation",()=>readonlyContext.AddAsync(new XmlNumericStock()));Assert.IsTrue(readonlyRefusal.IsError);
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        var cancelledAdd=await Timed("refuse-cancelled-allocation",()=>first.AddAsync(new XmlNumericStock(),cancelled.Token));Assert.IsTrue(cancelledAdd.IsError);
        var unused=new XmlNumericStock{Label="unused reservation"};
        var third=await Timed("reserve-after-refusals",()=>first.AddAsync(unused));Assert.IsFalse(third.IsError,third.Message+third.Exception);
        owned.Add(first.Identity(unused).Result);Assert.AreEqual(3L,unused.Number);Assert.IsFalse(first.Remove(unused).IsError);
        var blockName="m04_block_"+Guid.NewGuid().ToString("N");
        var blockModel=new CosmicPocoModelBuilder<XmlNumericStock>(contract+"_block").UseSequence(row=>row.Number,blockName,2).Build();
        Assert.IsFalse(blockModel.IsError,blockModel.Message+blockModel.Exception);
        using var blockA=CosmicPocoContext<XmlNumericStock>.Create(manager,adapter,access,blockModel.Result).Result;
        using var blockB=CosmicPocoContext<XmlNumericStock>.Create(manager,adapter,access,blockModel.Result).Result;
        var c=new XmlNumericStock{Label="block A1"};var d=new XmlNumericStock{Label="block A2"};var e=new XmlNumericStock{Label="block B"};
        var cAdded=await Timed("reserve-block-a1",()=>blockA.AddAsync(c));Assert.IsFalse(cAdded.IsError,cAdded.Message+ cAdded.Exception);owned.Add(blockA.Identity(c).Result);
        var dAdded=await Timed("reserve-block-a2",()=>blockA.AddAsync(d));Assert.IsFalse(dAdded.IsError,dAdded.Message+ dAdded.Exception);owned.Add(blockA.Identity(d).Result);
        var eAdded=await Timed("reserve-block-b",()=>blockB.AddAsync(e));Assert.IsFalse(eAdded.IsError,eAdded.Message+ eAdded.Exception);owned.Add(blockB.Identity(e).Result);
        CollectionAssert.AreEqual(new[]{1L,2L,3L},new[]{c.Number,d.Number,e.Number});
        var blockSavedA=await Timed("save-block-a",()=>blockA.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(blockSavedA.IsError,blockSavedA.Message+blockSavedA.Exception);Assert.IsTrue(blockSavedA.IsSaved);
        var blockSavedB=await Timed("save-block-b",()=>blockB.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(blockSavedB.IsError,blockSavedB.Message+blockSavedB.Exception);Assert.IsTrue(blockSavedB.IsSaved);
        // Boundary-only allocator control; all entity and sequence operations above use real native providers.
        var terminal=new CosmicHiLoGenerator(new TerminalNumericBlock(),"terminal",1);
        var maximum=await terminal.NextAsync();Assert.IsFalse(maximum.IsError);Assert.AreEqual(long.MaxValue,maximum.Result);
        var exhausted=await terminal.NextAsync();Assert.IsTrue(exhausted.IsError);Assert.AreEqual("COSMIC_SEQUENCE_EXHAUSTED",exhausted.ErrorCode);
        return new NativeFixtureJourneyProof(8,5,new Dictionary<string,bool>{
            ["xmlNativeSequencePolicy"]=true,["crossContextUniqueIdentifiers"]=true,["nativeReservationSeparateFromEntityAcknowledgement"]=true,
            ["generatedLogicalKeyLookup"]=true,["readonlyAndCancellationBeforeAllocation"]=true,["fluentNativeBlockAllocation"]=true,
            ["syncZeroKeyRefusedExplicitly"]=true,["terminalHiLoBlockDoesNotWrap"]=true});
    }
}
