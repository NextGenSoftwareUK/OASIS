using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeCosmicConditionalGraphReceiptsAndRaces(bool synchronous,bool history)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var first=new Provider(uri,"neo4j","oasis-local-test-only") {IsVersionControlEnabled=history};
        var second=new Provider(uri,"neo4j","oasis-local-test-only") {IsVersionControlEnabled=history};
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=driver.AsyncSession();
        var ids=new HashSet<string>();var operations=new HashSet<string>();var actor=Guid.NewGuid();var complete=false;
        Guid Id(){var id=Guid.NewGuid();ids.Add(id.ToString());return id;}
        Guid Op(){var id=Guid.NewGuid();operations.Add(id.ToString());return id;}
        Guid Token(IHolon value)=>CosmicConcurrency.GetProviderRevisionId(value,ProviderType.Neo4jOASIS)!.Value;
        ConditionalHolonWriteRequest Request(IHolon root,Guid? expected,ConditionalHolonOperation kind=ConditionalHolonOperation.Save)
            =>new(new ConditionalHolonWrite(Op(),actor,ProviderType.Neo4jOASIS,new[]{new HolonWriteCondition(root.Id,expected)}),kind,new[]{root},saveChildren:false);
        Task<OASISResult<IEnumerable<IHolon>>> Write(Provider provider,ConditionalHolonWriteRequest request)
            =>synchronous ? Task.Run(()=>provider.ExecuteConditionalHolonWrite(request)):provider.ExecuteConditionalHolonWriteAsync(request);
        async Task<string> Snapshot(Guid id)=>(await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS json",new{id=id.ToString()})).SingleAsync())["json"].As<string>();
        async Task<long> Count(string query,object parameters)=>(await(await inspection.RunAsync(query,parameters)).SingleAsync())["count"].As<long>();
        try
        {
            Assert.IsFalse((await first.ActivateProviderAsync()).IsError);Assert.IsFalse((await second.ActivateProviderAsync()).IsError);
            var child=new Holon {Id=Id(),Name="owned neo conditional child"};
            var root=new Holon {Id=Id(),Name="owned neo conditional root",Children=new List<IHolon>{child}};
            var create=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Op(),actor,ProviderType.Neo4jOASIS,
                new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(child.Id,null)}),ConditionalHolonOperation.Save,new[]{root});
            var saved=await Write(first,create);Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            var initial=saved.Result.Single();var initialChild=initial.Children.Single();
            Assert.AreEqual(1,initial.Version);Assert.AreEqual(actor,initial.CreatedByAvatarId);Assert.AreNotEqual(Guid.Empty,Token(initialChild));
            Assert.AreEqual(0,root.Version);Assert.IsNull(CosmicConcurrency.GetProviderRevisionId(root,ProviderType.Neo4jOASIS));
            var raw=await first.SaveHolonAsync(new Holon {Id=root.Id,Name="owned raw intervening root",Version=8,VersionId=Guid.NewGuid()},saveChildren:false);
            Assert.IsFalse(raw.IsError,raw.Message+raw.Exception);Assert.AreNotEqual(Token(initial),Token(raw.Result));
            await second.DeActivateProviderAsync();second=new Provider(uri,"neo4j","oasis-local-test-only") {IsVersionControlEnabled=history};
            Assert.IsFalse((await second.ActivateProviderAsync()).IsError);
            var replay=await Write(second,create);Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
            Assert.AreEqual(initial.VersionId,replay.Result.Single().VersionId);Assert.AreEqual(initial.Name,replay.Result.Single().Name);
            root.Name="changed owned intent";Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",(await Write(first,create)).ErrorCode);root.Name="owned neo conditional root";
            var rawChild=await first.SaveHolonAsync(new Holon {Id=child.Id,ParentHolonId=root.Id,Name="owned raw child",Version=4,VersionId=Guid.NewGuid()},saveChildren:false);
            Assert.IsFalse(rawChild.IsError,rawChild.Message+rawChild.Exception);
            var before=await Snapshot(root.Id);
            var stale=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Op(),actor,ProviderType.Neo4jOASIS,
                new[]{new HolonWriteCondition(root.Id,Token(raw.Result)),new HolonWriteCondition(child.Id,Token(initialChild))}),ConditionalHolonOperation.Save,new[]{root});
            var rejected=await Write(first,stale);Assert.AreEqual(CosmicConcurrencyException.Code,rejected.ErrorCode,rejected.Message+rejected.Exception);
            Assert.AreEqual(child.Id,((CosmicConcurrencyException)rejected.Exception).Conflicts.Single().HolonId);Assert.AreEqual(before,await Snapshot(root.Id));
            var updated=await Write(first,Request(new Holon {Id=root.Id,Name="owned conditional update",Version=999},Token(raw.Result)));
            Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);var current=updated.Result.Single();
            Assert.AreEqual(raw.Result.Version+1,current.Version);Assert.AreEqual(raw.Result.VersionId,current.PreviousVersionId);
            var competing=await Task.WhenAll(Write(first,Request(new Holon {Id=root.Id,Name="owned winner A"},Token(current))),
                Write(second,Request(new Holon {Id=root.Id,Name="owned winner B"},Token(current))));
            Assert.AreEqual(1,competing.Count(result=>!result.IsError));Assert.AreEqual(1,competing.Count(result=>result.IsError));
            CollectionAssert.Contains(new[]{CosmicConcurrencyException.Code,"COSMIC_CONCURRENT_WRITE"},competing.Single(result=>result.IsError).ErrorCode);
            current=competing.Single(result=>!result.IsError).Result.Single();
            var softRequest=Request(new Holon {Id=root.Id},Token(current),ConditionalHolonOperation.SoftDelete);
            var soft=await Write(first,softRequest);Assert.IsFalse(soft.IsError,soft.Message+soft.Exception);Assert.IsTrue(soft.IsDeleted);
            var tombstone=soft.Result.Single();Assert.IsTrue(tombstone.IsDeleted);Assert.AreEqual(current.Name,tombstone.Name);Assert.AreEqual(actor,tombstone.DeletedByAvatarId);
            var hardRequest=Request(new Holon {Id=root.Id},Token(tombstone),ConditionalHolonOperation.HardDelete);
            var hard=await Write(first,hardRequest);Assert.IsFalse(hard.IsError,hard.Message+hard.Exception);Assert.IsTrue(hard.IsDeleted);
            Assert.AreEqual(0L,await Count("MATCH (h:Holon {Id:$id}) RETURN count(h) AS count",new{id=root.Id.ToString()}));
            Assert.AreEqual(1L,await Count("MATCH (h:Holon {Id:$id}) RETURN count(h) AS count",new{id=child.Id.ToString()}));
            Assert.AreEqual(hard.Result.Single().VersionId,(await Write(second,hardRequest)).Result.Single().VersionId);
            Assert.AreEqual(CosmicConcurrencyException.Code,(await Write(second,Request(new Holon {Id=root.Id},Token(tombstone),ConditionalHolonOperation.HardDelete))).ErrorCode);
            var raceId=Id();var createRace=await Task.WhenAll(Write(first,Request(new Holon {Id=raceId,Name="owned create A"},null)),
                Write(second,Request(new Holon {Id=raceId,Name="owned create B"},null)));
            Assert.AreEqual(1,createRace.Count(result=>!result.IsError));Assert.AreEqual(1,createRace.Count(result=>result.IsError));
            var exact=Request(new Holon {Id=Id(),Name="owned exact replay"},null);
            var exactRace=await Task.WhenAll(Write(first,exact),Write(second,exact));
            Assert.IsTrue(exactRace.Any(result=>!result.IsError));
            var exactReplay=await Write(second,exact);Assert.IsFalse(exactReplay.IsError,exactReplay.Message+exactReplay.Exception);
            foreach(var acknowledgement in exactRace.Where(result=>!result.IsError))Assert.AreEqual(acknowledgement.Result.Single().VersionId,exactReplay.Result.Single().VersionId);
            var receipt=(await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r.ResultJson AS json",new{operation=exact.Write.OperationId.ToString()})).SingleAsync())["json"].As<string>();
            await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) SET r.ResultJson='invalid owned receipt'",new{operation=exact.Write.OperationId.ToString()})).ConsumeAsync();
            Assert.AreEqual("COSMIC_RECEIPT_INVALID",(await Write(first,exact)).ErrorCode);
            await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) SET r.ResultJson=$receipt",new{operation=exact.Write.OperationId.ToString(),receipt})).ConsumeAsync();
            var legacyId=Id();var legacySeed=await first.SaveHolonAsync(new Holon {Id=legacyId,Name="owned legacy token",Version=3,VersionId=Guid.NewGuid()},saveChildren:false);
            Assert.IsFalse(legacySeed.IsError,legacySeed.Message);
            await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) REMOVE h.ProviderRevisionId",new{id=legacyId.ToString()})).ConsumeAsync();
            var legacy=await first.LoadHolonAsync(legacyId,loadChildren:false);Assert.IsFalse(legacy.IsError,legacy.Message);Assert.AreEqual(Guid.Empty,Token(legacy.Result));
            var upgrade=await Write(second,Request(new Holon {Id=legacyId,Name="owned upgraded legacy token"},Guid.Empty));
            Assert.IsFalse(upgrade.IsError,upgrade.Message+upgrade.Exception);Assert.AreEqual(legacySeed.Result.Version+1,upgrade.Result.Single().Version);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings {LogSwitchingProviders=false}}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            NextGenSoftware.OASIS.API.Core.Managers.HolonManager Manager(Provider provider)
            {
                var providers=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna) {
                    IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
                providers.RegisterProvider(provider);return new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null,dna,providers);
            }
            var manager=Manager(first);var otherManager=Manager(second);
            var managedChild=new Holon {Name="owned managed neo child"};
            var managedRoot=new Holon {Name="owned managed neo root",Children=new List<IHolon>{managedChild}};
            managedChild.ParentHolon=managedRoot;
            var managedOptions=new CosmicWriteOptions(Op());
            var managed=synchronous ? manager.SaveHolon(managedRoot,actor,managedOptions,providerType:ProviderType.Neo4jOASIS)
                : await manager.SaveHolonAsync(managedRoot,actor,managedOptions,providerType:ProviderType.Neo4jOASIS);
            Assert.IsFalse(managed.IsError,managed.Message+managed.Exception);
            ids.Add(managedRoot.Id.ToString());ids.Add(managedChild.Id.ToString());
            Assert.AreSame(managedRoot,managed.Result);Assert.IsFalse(managedRoot.HasHolonChanged());Assert.AreNotEqual(Guid.Empty,Token(managedChild));
            var untouchedChild=await Snapshot(managedChild.Id);
            var rival=await otherManager.LoadHolonAsync(managedRoot.Id,loadChildren:false,providerType:ProviderType.Neo4jOASIS);
            Assert.IsFalse(rival.IsError,rival.Message);var originalRivalVersion=rival.Result.VersionId;
            managedRoot.Name="owned managed neo winner";
            var nextOptions=new CosmicWriteOptions(Op());
            var winner=synchronous ? manager.SaveHolon(managedRoot,actor,nextOptions,saveChildren:false,providerType:ProviderType.Neo4jOASIS)
                : await manager.SaveHolonAsync(managedRoot,actor,nextOptions,saveChildren:false,providerType:ProviderType.Neo4jOASIS);
            Assert.IsFalse(winner.IsError,winner.Message+winner.Exception);Assert.IsFalse(managedRoot.HasHolonChanged());
            rival.Result.Name="pending managed neo loser";var loserOptions=new CosmicWriteOptions(Op());
            var loser=synchronous ? otherManager.SaveHolon(rival.Result,actor,loserOptions,saveChildren:false,providerType:ProviderType.Neo4jOASIS)
                : await otherManager.SaveHolonAsync(rival.Result,actor,loserOptions,saveChildren:false,providerType:ProviderType.Neo4jOASIS);
            Assert.AreEqual(CosmicConcurrencyException.Code,loser.ErrorCode);Assert.AreEqual(originalRivalVersion,rival.Result.VersionId);Assert.IsTrue(rival.Result.HasHolonChanged());
            var softOptions=new CosmicWriteOptions(Op());
            var managedSoft=synchronous ? manager.DeleteHolon(managedRoot,actor,softOptions,providerType:ProviderType.Neo4jOASIS)
                : await manager.DeleteHolonAsync(managedRoot,actor,softOptions,providerType:ProviderType.Neo4jOASIS);
            Assert.IsFalse(managedSoft.IsError,managedSoft.Message+managedSoft.Exception);Assert.IsTrue(managedRoot.IsDeleted);Assert.IsFalse(managedRoot.HasHolonChanged());
            var hardOptions=new CosmicWriteOptions(Op());
            var managedHard=synchronous ? manager.DeleteHolon(managedRoot,actor,hardOptions,softDelete:false,providerType:ProviderType.Neo4jOASIS)
                : await manager.DeleteHolonAsync(managedRoot,actor,hardOptions,softDelete:false,providerType:ProviderType.Neo4jOASIS);
            Assert.IsFalse(managedHard.IsError,managedHard.Message+managedHard.Exception);Assert.AreEqual(untouchedChild,await Snapshot(managedChild.Id));
            var driverField=typeof(Provider).GetField("_driver",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
            var healthy=(IDriver)driverField.GetValue(first)!;var acknowledgedCommits=0;
            var lostResponse=new IOException("owned Neo conditional commit acknowledgement loss");
            var uncertainSource=new Holon {Name="owned uncertain neo root",Children=new List<IHolon>{new Holon{Name="owned uncertain neo child"}}};
            var uncertainOperation=Op();var uncertainOptions=new CosmicWriteOptions(uncertainOperation);
            driverField.SetValue(first,Neo4jCommitFaultProxy.Wrap(healthy,lostResponse,()=>acknowledgedCommits++));
            try
            {
                var uncertain=synchronous ? manager.SaveHolon(uncertainSource,actor,uncertainOptions,providerType:ProviderType.Neo4jOASIS)
                    : await manager.SaveHolonAsync(uncertainSource,actor,uncertainOptions,providerType:ProviderType.Neo4jOASIS);
                Assert.IsTrue(uncertain.IsError);Assert.IsFalse(uncertain.IsSaved);Assert.AreEqual("COSMIC_WRITE_RECONCILIATION_REQUIRED",uncertain.ErrorCode);
                var evidence=(NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jConditionalReconciliationException)uncertain.Exception;
                Assert.AreEqual(uncertainOperation,evidence.OperationId);Assert.AreSame(lostResponse,evidence.InnerException);
                Assert.AreEqual(string.Empty,evidence.OriginalTransactionId);Assert.AreEqual(1,acknowledgedCommits);
                Assert.AreEqual(Guid.Empty,uncertainSource.Id);Assert.AreEqual(Guid.Empty,uncertainSource.Children.Single().Id);Assert.AreEqual(0,uncertainSource.Version);
                // Independently inspect the original durable receipt before any explicit replay.
                var originalJson=(await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r.ResultJson AS json",
                    new{operation=uncertainOperation.ToString()})).SingleAsync())["json"].As<string>();
                Assert.AreEqual(uncertainOperation,Newtonsoft.Json.Linq.JObject.Parse(originalJson)["operation"]!.ToObject<Guid>());
                var original=ConditionalHolonReceipt.Read(originalJson).Single();
                foreach(var node in HolonGraphWritePlan.Create(new[]{original},true,true,0,0).Nodes)ids.Add(node.Snapshot.Id.ToString());
                Assert.AreEqual("owned uncertain neo root",original.Name);Assert.AreEqual(1,original.Version);
                var persisted=OasisJson.DeserializeStored<Holon>(await Snapshot(original.Id));Assert.AreEqual(original.VersionId,persisted.VersionId);
                var recovered=synchronous ? manager.SaveHolon(uncertainSource,actor,uncertainOptions,providerType:ProviderType.Neo4jOASIS)
                    : await manager.SaveHolonAsync(uncertainSource,actor,uncertainOptions,providerType:ProviderType.Neo4jOASIS);
                Assert.IsFalse(recovered.IsError,recovered.Message+recovered.Exception);Assert.AreEqual(original.Id,uncertainSource.Id);
                Assert.AreEqual(original.VersionId,uncertainSource.VersionId);Assert.IsFalse(uncertainSource.HasHolonChanged());Assert.AreEqual(1,acknowledgedCommits);
            }
            finally {driverField.SetValue(first,healthy);}
            // Exhaustion occurs after archiving the first selected record: every archive/claim must roll back.
            var exhaustedChild=new Holon {Id=Id(),Name="owned exhausted child"};var rollbackRoot=new Holon {Id=Id(),Name="owned rollback root",Children=new List<IHolon>{exhaustedChild}};
            var rollbackCreate=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Op(),actor,ProviderType.Neo4jOASIS,
                new[]{new HolonWriteCondition(rollbackRoot.Id,null),new HolonWriteCondition(exhaustedChild.Id,null)}),ConditionalHolonOperation.Save,new[]{rollbackRoot});
            var seeded=await Write(first,rollbackCreate);Assert.IsFalse(seeded.IsError,seeded.Message+seeded.Exception);
            var seededRoot=seeded.Result.Single();var exhausted=OasisJson.Project<Holon>(seededRoot.Children.Single());exhausted.Version=int.MaxValue;
            CosmicConcurrency.SetProviderRevisionId(exhausted,ProviderType.Neo4jOASIS,Guid.NewGuid());
            await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) SET h.SnapshotJson=$json,h.ProviderRevisionId=$token",
                new{id=exhausted.Id.ToString(),json=OasisJson.Serialize(exhausted),token=Token(exhausted).ToString()})).ConsumeAsync();
            var rootBefore=await Snapshot(rollbackRoot.Id);
            var historyBefore=await Count("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id}) RETURN count(v) AS count",new{id=rollbackRoot.Id.ToString()});
            var rollbackRequest=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Op(),actor,ProviderType.Neo4jOASIS,
                new[]{new HolonWriteCondition(rollbackRoot.Id,Token(seededRoot)),new HolonWriteCondition(exhausted.Id,Token(exhausted))}),ConditionalHolonOperation.Save,new[]{rollbackRoot});
            var failed=await Write(first,rollbackRequest);Assert.IsTrue(failed.IsError);Assert.AreEqual("COSMIC_CONDITIONAL_WRITE_FAILED",failed.ErrorCode);
            Assert.AreEqual(rootBefore,await Snapshot(rollbackRoot.Id));
            Assert.AreEqual(historyBefore,await Count("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id}) RETURN count(v) AS count",new{id=rollbackRoot.Id.ToString()}));
            Assert.AreEqual(0L,await Count("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN count(r) AS count",new{operation=rollbackRequest.Write.OperationId.ToString()}));
            complete=true;
        }
        finally
        {
            Console.WriteLine("Owned Neo conditional fixture: "+OasisJson.Serialize(new{ids=ids.ToArray(),operations=operations.ToArray(),actor,complete}));
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=ids.ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v",new{ids=ids.ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations DETACH DELETE r",new{operations=operations.ToArray()})).ConsumeAsync();
            }
            await first.DeActivateProviderAsync();await second.DeActivateProviderAsync();
        }
    }
}
