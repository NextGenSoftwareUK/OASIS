using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using ProviderType=NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleNativeAcceptanceTests
{
    public sealed class NativeRoleOwner:Holon
    {
        public List<Holon> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,members=>Components=members.Cast<Holon>().ToList())}).ToArray();
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()=>base.GetChildRoleBindings()
            .Concat(new[]{HolonChildRoleBinding.Create<Holon>(nameof(Components),members=>Components=members.ToList(),
                source=>NextGenSoftware.OASIS.API.Core.Helpers.Mapper<IHolon,Holon>.MapBaseHolonProperties(source))}).ToArray();
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeNamedChildRolesSurviveGraphAndRootOnlySave(bool synchronous,bool conditional)
    {
        var connection=Environment.GetEnvironmentVariable("ORACLEDBOASIS_CONNECTIONSTRING")!;
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_ORACLE_TEST_WORK")!,$"named-child-roles-{synchronous}-{conditional}.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new NativeRoleOwner{Id=Guid.NewGuid(),ParentHolonId=Guid.NewGuid(),Name="owned named-role root",IsActive=true};
        var marker=root.Id.ToString("N");root.MetaData["oracle-role-fixture"]=marker;
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned named-role component",IsActive=true};root.Components.Add(child);
        var actor=Guid.NewGuid();var operations=conditional?new[]{Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()}:Array.Empty<Guid>();
        var expected=HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(root).CreateManifest(root.Id,row=>row.Id));
        var provider=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        var other=new OracleDBOASIS(connection){IsVersionControlEnabled=true};
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await other.ActivateProviderAsync()).IsError);
            await using var verifier=new OracleConnection(connection);await verifier.OpenAsync();
            foreach(var id in new[]{root.Id,child.Id})Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));
            foreach(var operation in operations)await AssertReceiptAbsent(verifier,operation);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=new[]{root.Id,child.Id},operations,actor,state="before-owned-named-role-fixture"}));file.Flush(true);
            }
            var requests=new List<ConditionalHolonWriteRequest>();
            if(conditional){
                var write=new ConditionalHolonWrite(operations[0],actor,ProviderType.OracleDBOASIS,new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(child.Id,null)});
                var request=new ConditionalHolonWriteRequest(write,ConditionalHolonOperation.Save,new[]{root});requests.Add(request);
                var saved=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
                Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            }else{
                var saved=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
                Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            }
            var loaded=await provider.LoadHolonAsync(root.Id,loadChildren:false);Assert.IsFalse(loaded.IsError,loaded.Exception?.ToString());
            var initialManifest=((Holon)loaded.Result).ChildRoleManifestJson;
            var manager=TypedOracleRoleManager(other);
            var typedObservations=new List<(string Route,bool Error,string Cause,int Children,int Components,string? Name,bool Changed)>();
            foreach(var route in new[]{"all","search","metadata","pairs","parent","parent-key"}){
                var pairs=new Dictionary<string,string>{{"oracle-role-fixture",marker}};
                var result=route switch{
                    "all"=>synchronous?manager.LoadAllHolons<NativeRoleOwner>(cache:false,providerType:ProviderType.OracleDBOASIS):await manager.LoadAllHolonsAsync<NativeRoleOwner>(providerType:ProviderType.OracleDBOASIS),
                    "search"=>synchronous?manager.SearchHolons<NativeRoleOwner>(root.Name,actor,searchOnlyForCurrentAvatar:false,providerType:ProviderType.OracleDBOASIS):await manager.SearchHolonsAsync<NativeRoleOwner>(root.Name,actor,searchOnlyForCurrentAvatar:false,providerType:ProviderType.OracleDBOASIS),
                    "metadata"=>synchronous?manager.LoadHolonsByMetaData<NativeRoleOwner>("oracle-role-fixture",marker,providerType:ProviderType.OracleDBOASIS):await manager.LoadHolonsByMetaDataAsync<NativeRoleOwner>("oracle-role-fixture",marker,providerType:ProviderType.OracleDBOASIS),
                    "pairs"=>synchronous?manager.LoadHolonsByMetaData<NativeRoleOwner>(pairs,NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,providerType:ProviderType.OracleDBOASIS):await manager.LoadHolonsByMetaDataAsync<NativeRoleOwner>(pairs,NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All,providerType:ProviderType.OracleDBOASIS),
                    "parent"=>synchronous?manager.LoadHolonsForParent<NativeRoleOwner>(root.ParentHolonId,providerType:ProviderType.OracleDBOASIS):await manager.LoadHolonsForParentAsync<NativeRoleOwner>(root.ParentHolonId,providerType:ProviderType.OracleDBOASIS),
                    _=>synchronous?manager.LoadHolonsForParent<NativeRoleOwner>(root.ParentHolonId.ToString(),providerType:ProviderType.OracleDBOASIS):await manager.LoadHolonsForParentAsync<NativeRoleOwner>(root.ParentHolonId.ToString(),providerType:ProviderType.OracleDBOASIS)
                };
                var typed=result.Result?.SingleOrDefault(row=>row.Id==root.Id);
                typedObservations.Add((route,result.IsError,result.Message+result.Exception,typed?.Children.Count??-1,typed?.Components.Count??-1,typed?.Components.FirstOrDefault()?.Name,typed?.HasHolonChanged()??true));
            }
            var childBefore=(await Read(verifier,"OASIS_HOLONS",child.Id))!;
            var scalar=new Holon{Id=root.Id,ParentHolonId=root.ParentHolonId,Name="owned named-role scalar update",IsActive=true};
            if(conditional){
                var write=new ConditionalHolonWrite(operations[1],actor,ProviderType.OracleDBOASIS,new[]{new HolonWriteCondition(root.Id,CosmicConcurrency.GetProviderRevisionId(loaded.Result,ProviderType.OracleDBOASIS))});
                var request=new ConditionalHolonWriteRequest(write,ConditionalHolonOperation.Save,new[]{scalar},saveChildren:false);requests.Add(request);
                var saved=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
                Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            }else{
                var saved=synchronous?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);
                Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            }
            var rootJson=(await Read(verifier,"OASIS_HOLONS",root.Id))!;
            var stored=StoredRecordJson.DeserializeStored<Holon>(rootJson)!;
            Assert.AreEqual(scalar.Name,stored.Name);Assert.AreEqual(childBefore,await Read(verifier,"OASIS_HOLONS",child.Id));
            var preservedManifest=stored.ChildRoleManifestJson;
            // An explicitly observed legacy-null manifest must beat stale caller membership.
            stored.ChildRoleManifestJson=null;var legacyJson=StoredRecordJson.Serialize(stored);
            await using(var legacy=new OracleCommand("UPDATE OASIS_HOLONS SET DATA_JSON=:Legacy WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true}){
                legacy.Parameters.Add("Legacy",OracleDbType.Clob).Value=legacyJson;legacy.Parameters.Add("Id",OracleDbType.Raw).Value=root.Id.ToByteArray();legacy.Parameters.Add("Expected",OracleDbType.Clob).Value=rootJson;Assert.AreEqual(1,await legacy.ExecuteNonQueryAsync());
            }
            scalar=new Holon{Id=root.Id,ParentHolonId=root.ParentHolonId,Name="owned named-role legacy update",IsActive=true,ChildRoleManifestJson=expected};
            if(conditional){
                var write=new ConditionalHolonWrite(operations[2],actor,ProviderType.OracleDBOASIS,new[]{new HolonWriteCondition(root.Id,CosmicConcurrency.GetProviderRevisionId(stored,ProviderType.OracleDBOASIS))});
                var request=new ConditionalHolonWriteRequest(write,ConditionalHolonOperation.Save,new[]{scalar},saveChildren:false);requests.Add(request);
                var saved=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                var beforeReplay=(await Read(verifier,"OASIS_HOLONS",root.Id))!;
                var replay=synchronous?other.ExecuteConditionalHolonWrite(request):await other.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(beforeReplay,await Read(verifier,"OASIS_HOLONS",root.Id));
                var replayed=(Holon)replay.Result.Single();var observedReplay=StoredRecordJson.DeserializeStored<Holon>(beforeReplay)!;
                Assert.AreEqual(root.Id,replayed.Id);Assert.AreEqual(observedReplay.VersionId,replayed.VersionId);Assert.AreEqual(scalar.Name,replayed.Name);Assert.IsNull(replayed.ChildRoleManifestJson);
            }else{
                var saved=synchronous?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            }
            rootJson=(await Read(verifier,"OASIS_HOLONS",root.Id))!;
            var legacyAfter=StoredRecordJson.DeserializeStored<Holon>(rootJson)!;Assert.AreEqual(scalar.Name,legacyAfter.Name);Assert.AreEqual(childBefore,await Read(verifier,"OASIS_HOLONS",child.Id));
            var receiptBodies=new List<(Guid Id,string Hash,string Json)>();
            foreach(var request in requests){
                await using var read=new OracleCommand("SELECT REQUEST_HASH,RESULT_JSON FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id",verifier){BindByName=true};read.Parameters.Add("Id",OracleDbType.Raw).Value=request.Write.OperationId.ToByteArray();
                await using var reader=await read.ExecuteReaderAsync();Assert.IsTrue(await reader.ReadAsync());
                var hash=ConditionalHolonPlan.Create(request,request.Write.Authority).Fingerprint;Assert.AreEqual(hash,reader.GetString(0));receiptBodies.Add((request.Write.OperationId,hash,reader.GetString(1)));Assert.IsFalse(await reader.ReadAsync());
            }
            // Known successful writes: independently verify exact originals and clean them
            // before asserting role selection, so a reproduced mapping defect leaks no fixtures.
            await using(var transaction=(OracleTransaction)await verifier.BeginTransactionAsync()){
                foreach(var row in new[]{(Id:root.Id,Json:rootJson),(Id:child.Id,Json:childBefore)}){
                    await using var delete=new OracleCommand("DELETE FROM OASIS_HOLONS WHERE ID=:Id AND DBMS_LOB.COMPARE(DATA_JSON,:Expected)=0",verifier){BindByName=true,Transaction=transaction};
                    delete.Parameters.Add("Id",OracleDbType.Raw).Value=row.Id.ToByteArray();delete.Parameters.Add("Expected",OracleDbType.Clob).Value=row.Json;Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
                }
                foreach(var receipt in receiptBodies){
                    await using var delete=new OracleCommand("DELETE FROM OASIS_WRITE_RECEIPTS WHERE OPERATION_ID=:Id AND REQUEST_HASH=:Hash AND DBMS_LOB.COMPARE(RESULT_JSON,:Expected)=0",verifier){BindByName=true,Transaction=transaction};
                    delete.Parameters.Add("Id",OracleDbType.Raw).Value=receipt.Id.ToByteArray();delete.Parameters.Add("Hash",OracleDbType.Varchar2).Value=receipt.Hash;delete.Parameters.Add("Expected",OracleDbType.Clob).Value=receipt.Json;Assert.AreEqual(1,await delete.ExecuteNonQueryAsync());
                }
                await transaction.CommitAsync();
            }
            foreach(var id in new[]{root.Id,child.Id})Assert.IsNull(await Read(verifier,"OASIS_HOLONS",id));foreach(var operation in operations)await AssertReceiptAbsent(verifier,operation);File.Delete(journal);
            Console.WriteLine(JsonSerializer.Serialize(new{typedRoutes=typedObservations.Select(row=>new{route=row.Route,error=row.Error,children=row.Children,components=row.Components,changed=row.Changed})}));
            Assert.AreEqual(expected,initialManifest,"Selected named memberships must be persisted with the original graph.");
            Assert.AreEqual(expected,preservedManifest,"A root-only write must preserve locked persisted membership, not replace it with caller absence.");
            Assert.IsNull(legacyAfter.ChildRoleManifestJson,"Persisted null is authoritative, even when the caller supplies stale membership.");
            foreach(var observation in typedObservations){Assert.IsFalse(observation.Error,observation.Route+": "+observation.Cause);Assert.AreEqual(0,observation.Children,observation.Route);Assert.AreEqual(1,observation.Components,observation.Route);Assert.AreEqual(child.Name,observation.Name,observation.Route);Assert.IsFalse(observation.Changed,observation.Route+" must establish tracking after role hydration.");}
        }finally{await provider.DeActivateProviderAsync();await other.DeActivateProviderAsync();}
    }

    private static HolonManager TypedOracleRoleManager(OracleDBOASIS provider)
    {
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};providers.RegisterProvider(provider);
        return new HolonManager(null,dna,providers);
    }
}
