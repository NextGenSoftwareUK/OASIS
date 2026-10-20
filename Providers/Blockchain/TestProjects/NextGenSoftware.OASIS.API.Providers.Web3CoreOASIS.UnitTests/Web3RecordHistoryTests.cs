using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class Web3RecordHistoryTests
{
    [TestMethod]
    public void DecryptedHistoryDocumentPreservesExactMetadata()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");var key=RandomNumberGenerator.GetBytes(32);
        try{
            Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",Convert.ToBase64String(key));
            var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545","unused","0xowned");
            var value=1.0000000000000000000000000001m;var source=new Holon{Id=Guid.NewGuid(),MetaData=new(){["precise"]=value}};
            var entity=new EntityOASIS{EntityId=(uint)HashUtility.GetNumericHash(source.Id.ToString()),ExternalId=Web3CoreOASISBaseProvider.PrivateExternalId(source.Id),Info=Encoding.UTF8.GetBytes(provider.EncodePrivateRecord("holon",source))};
            var method=typeof(Web3CoreOASISBaseProvider).GetMethod("DecryptPrivateDocument",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Holon));
            var decoded=((Holon Record,JObject Document))method.Invoke(provider,new object[]{"holon",entity})!;
            using var json=System.Text.Json.JsonDocument.Parse(decoded.Document.ToString());
            Assert.AreEqual(value,json.RootElement.GetProperty("MetaData").GetProperty("precise").GetDecimal());
            Assert.AreEqual(value,source.MetaData["precise"]);
        }finally{Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",previous);CryptographicOperations.ZeroMemory(key);}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ProfilePublicationRejectsNonIdentityCallerEdits(bool detail)
    {
        var oldKey=Environment.GetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");var key=RandomNumberGenerator.GetBytes(32);
        try{
            Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",Convert.ToBase64String(key));
            var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545","unused","0xowned");
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",flags)!.SetValue(provider,
                new Web3CoreOASIS("0x"+new string('1',64),"http://127.0.0.1:18545","0x0000000000000000000000000000000000000001",Web3CoreOASISBaseProviderHelper.Abi,31337));
            if(detail)await VerifyProfileEdit<IAvatarDetail>(provider,new AvatarDetail{Id=Guid.NewGuid(),IsNewHolon=true},"detail",row=>OasisJson.Deserialize<AvatarDetail>(OasisJson.Serialize(row)));
            else await VerifyProfileEdit<IAvatar>(provider,new Avatar{Id=Guid.NewGuid(),IsNewHolon=true},"avatar",row=>OasisJson.Deserialize<Avatar>(OasisJson.Serialize(row)));
        }finally{Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",oldKey);CryptographicOperations.ZeroMemory(key);}
    }

    private static async Task VerifyProfileEdit<T>(Web3CoreOASISBaseProvider provider,T source,string kind,Func<T,T> clone) where T:class,IHolonBase
    {
        var original=source.Id;var calls=0;
        Func<uint,byte[],byte[],Task<(Nethereum.RPC.Eth.DTOs.TransactionReceipt Receipt,IHolonBase Record)>> submit=(id,external,payload)=>{
            calls++;source.MetaData["concurrent edit"]="keep";
            IHolonBase committed=clone(source);committed.Version=9;
            return Task.FromResult((new Nethereum.RPC.Eth.DTOs.TransactionReceipt{TransactionHash="controlled-ack"},committed));
        };
        var method=typeof(Web3CoreOASISBaseProvider).GetMethod("SavePrivateProfileAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
        var result=await (Task<NextGenSoftware.OASIS.Common.OASISResult<T>>)method.Invoke(provider,new object[]{source,kind,clone,submit})!;
        Assert.IsTrue(result.IsError,"Nonidentity caller edits must reject acknowledgement publication.");Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(1,calls);Assert.AreEqual(original,source.Id);Assert.AreEqual(0,source.Version);Assert.IsTrue(source.IsNewHolon);
        Assert.AreEqual("keep",source.MetaData["concurrent edit"]);
        var evidence=result.Exception as Web3PartialMutationException;Assert.IsNotNull(evidence);Assert.AreEqual("controlled-ack",evidence.OriginalTransactionId);
    }

    [TestMethod]
    public void AcknowledgedPublicationFailureRetainsLastAcknowledgedTransaction()
    {
        var error=new Web3PartialMutationException(new[]{Guid.NewGuid()},new[]{"acknowledged-first","acknowledged-current"},new InvalidOperationException("caller edited after acknowledgement"));
        Assert.AreEqual("acknowledged-current",error.OriginalTransactionId);
        CollectionAssert.AreEqual(new[]{"acknowledged-first","acknowledged-current"},error.AcknowledgedTransactionIds.ToArray());
        var uncertain=new NativeTransactionFollowupException(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.PolygonOASIS,"uncertain-later",new IOException("lost reply"));
        Assert.AreEqual("uncertain-later",new Web3PartialMutationException(new[]{Guid.NewGuid()},new[]{"acknowledged-first"},uncertain).OriginalTransactionId);
        Assert.AreEqual(string.Empty,new Web3PartialMutationException(Array.Empty<Guid>(),Array.Empty<string>(),new InvalidOperationException()).OriginalTransactionId);
    }

    private sealed class NamedOwner:Holon
    {
        public List<Holon> Components {get;set;}=new();
        public override System.Collections.ObjectModel.ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<Holon>().ToList())}).ToArray();
    }

    [TestMethod]
    public void GraphPlanIncludesDeclaredNamedChildrenWithoutPublishingIdentities()
    {
        var source=new NamedOwner();var child=new Holon();source.Components.Add(child);
        var method=typeof(Web3CoreOASISBaseProvider).GetMethod("PlanWeb3Graph",BindingFlags.Static|BindingFlags.NonPublic)!;
        var plan=(System.Runtime.CompilerServices.ITuple)method.Invoke(null,new object[]{new IHolon[]{source},true,true,0,0})!;
        Assert.AreEqual(2,((System.Collections.IList)plan[1]!).Count);
        Assert.AreEqual(Guid.Empty,source.Id);Assert.AreEqual(Guid.Empty,child.Id);Assert.AreEqual(Guid.Empty,child.ParentHolonId);
    }

    [TestMethod]
    public void GraphPlanCapturesCanonicalCallerForPublication()
    {
        var source=new Holon{Name="captured"};
        var method=typeof(Web3CoreOASISBaseProvider).GetMethod("PlanWeb3Graph",BindingFlags.Static|BindingFlags.NonPublic)!;
        var plan=(System.Runtime.CompilerServices.ITuple)method.Invoke(null,new object[]{new IHolon[]{source},false,true,0,0})!;
        var node=((System.Collections.IList)plan[1]!)[0]!;
        var guard=(StoredRecordPublicationSnapshot<IHolon>)node.GetType().GetProperty("Publication")!.GetValue(node)!;
        source.MetaData["newer caller edit"]="preserve";
        Assert.ThrowsException<InvalidOperationException>(guard.ValidateUnchanged);Assert.AreEqual(Guid.Empty,source.Id);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task SelectedHistoryUsesCapturedRolesBeforeEncryption(bool history)
    {
        var originalKey=Environment.GetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");var key=RandomNumberGenerator.GetBytes(32);
        try{
            Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",Convert.ToBase64String(key));
            var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545","unused","0xowned");
            var source=new NamedOwner();source.Components.Add(new Holon());
            var method=typeof(Web3CoreOASISBaseProvider).GetMethod("PlanWeb3Graph",BindingFlags.Static|BindingFlags.NonPublic)!;
            var graph=(System.Runtime.CompilerServices.ITuple)method.Invoke(null,new object[]{new IHolon[]{source},true,true,0,0})!;
            var node=((System.Collections.IList)graph[1]!)[0]!;
            var plan=(PlannedHolonWrite)node.GetType().GetProperty("Write")!.GetValue(node)!;
            var expected=NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Serialize(plan.RelationshipManifest);
            source.Components.Clear();
            var staged=await provider.StagePrivateHistoryAsync("holon",plan.Snapshot,null,history,plan);
            Assert.AreEqual(expected,staged.Record.ChildRoleManifestJson);Assert.IsNull(plan.Snapshot.ChildRoleManifestJson);Assert.AreEqual(Guid.Empty,source.Id);
            var stored=new EntityOASIS{EntityId=(uint)HashUtility.GetNumericHash(staged.Record.Id.ToString()),ExternalId=Web3CoreOASISBaseProvider.PrivateExternalId(staged.Record.Id),Info=staged.Info};
            var decoded=provider.DecodePrivateRecord<Holon>("holon",staged.Record.Id,stored);
            Assert.AreEqual(expected,decoded.ChildRoleManifestJson);
            Assert.AreEqual(history?1:0,decoded.Version);
        }finally{Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",originalKey);CryptographicOperations.ZeroMemory(key);}
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RootOnlyHistoryPreservesObservedRolesIncludingNull(bool hasRoles)
    {
        var originalKey=Environment.GetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");var key=RandomNumberGenerator.GetBytes(32);
        try{
            Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",Convert.ToBase64String(key));
            var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545","unused","0xowned");var id=Guid.NewGuid();
            string Manifest()=>NextGenSoftware.OASIS.API.Core.Serialization.HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,id,new[]{new HolonChildRole("Components",new[]{Guid.NewGuid()})}));
            var observed=hasRoles?Manifest():null;
            var first=await provider.StagePrivateHistoryAsync("holon",new Holon{Id=id,ChildRoleManifestJson=observed},null,true);
            var stored=new EntityOASIS{EntityId=(uint)HashUtility.GetNumericHash(id.ToString()),ExternalId=Web3CoreOASISBaseProvider.PrivateExternalId(id),Info=first.Info};
            var proposal=new Holon{Id=id,Name="root update",ChildRoleManifestJson=Manifest()};var callerRoles=proposal.ChildRoleManifestJson;
            var second=await provider.StagePrivateHistoryAsync("holon",proposal,stored,false);
            Assert.AreEqual(2,second.Record.Version);Assert.AreEqual(first.Record.VersionId,second.Record.PreviousVersionId);
            Assert.AreEqual(callerRoles,proposal.ChildRoleManifestJson,"Detached staging must not mutate the caller.");
            CollectionAssert.AreEqual(first.Info,stored.Info);
            Assert.AreEqual(observed,second.Record.ChildRoleManifestJson,"Root-only save must use the authoritative observed membership, including null.");
        }finally{Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",originalKey);CryptographicOperations.ZeroMemory(key);}
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task EncryptedHistoryStagesDetachedAndRetainsArchiveWhenDisabled(int kind)
    {
        if(kind==0)await Check("avatar",new Avatar{Id=Guid.NewGuid()});
        else if(kind==1)await Check("detail",new AvatarDetail{Id=Guid.NewGuid()});
        else await Check("holon",new Holon{Id=Guid.NewGuid()});
    }
    private static async Task Check<T>(string kind,T proposal) where T:class,IHolonBase
    {
        var previousKey=Environment.GetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");
        var key=RandomNumberGenerator.GetBytes(32);
        try{
            Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",Convert.ToBase64String(key));
            var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545","unused","0xowned");
            var numeric=(uint)HashUtility.GetNumericHash(proposal.Id.ToString());
            var external=Web3CoreOASISBaseProvider.PrivateExternalId(proposal.Id);
            proposal.MetaData=new(){{"marker","private-first"}};
            var legacy=await provider.StagePrivateHistoryAsync(kind,proposal,null,false);
            Assert.AreEqual(0,legacy.Record.Version);Assert.AreEqual(0,proposal.Version);
            var first=await provider.StagePrivateHistoryAsync(kind,proposal,null,true);
            Assert.AreEqual(1,first.Record.Version);Assert.AreEqual(0,proposal.Version);
            Assert.IsFalse(Encoding.UTF8.GetString(first.Info).Contains("private-first"));
            var stored=new EntityOASIS{EntityId=numeric,ExternalId=external,Info=first.Info};
            var original=(byte[])first.Info.Clone();proposal.MetaData["marker"]="private-second";
            var second=await provider.StagePrivateHistoryAsync(kind,proposal,stored,false);
            Assert.AreEqual(2,second.Record.Version);Assert.AreEqual(first.Record.VersionId,second.Record.PreviousVersionId);
            Assert.AreEqual(0,proposal.Version);CollectionAssert.AreEqual(original,stored.Info);
            var context=(string)typeof(Web3CoreOASISBaseProvider).GetMethod("PrivateContext",BindingFlags.Instance|BindingFlags.NonPublic)!
                .Invoke(provider,new object[]{kind,new System.Numerics.BigInteger(numeric),external})!;
            using var codec=new EncryptedRecordCodec(key,compress:true);
            var document=JObject.Parse(codec.Decrypt(context,Encoding.UTF8.GetString(second.Info)));
            var history=new EmbeddedRecordHistory();
            var past=await history.ReadAsync(proposal.Id,document,1,OasisJson.Deserialize<T>);
            Assert.AreEqual("private-first",past.MetaData["marker"]);
            Assert.IsNull(await history.ReadAsync(proposal.Id,document,99,OasisJson.Deserialize<T>));
            document[EmbeddedRecordHistory.PropertyName]!["schema"]=42;
            var corrupt=new EntityOASIS{EntityId=numeric,ExternalId=external,Info=Encoding.UTF8.GetBytes(codec.Encrypt(context,document.ToString()))};
            Assert.ThrowsException<InvalidDataException>(()=>provider.DecodePrivateRecord<T>(kind,proposal.Id,corrupt));
            await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>provider.StagePrivateHistoryAsync(kind,proposal,corrupt,false));
            var foreign=new EntityOASIS{EntityId=numeric,ExternalId=new byte[32],Info=original};
            await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>provider.StagePrivateHistoryAsync(kind,proposal,foreign,true));
        }finally{Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",previousKey);CryptographicOperations.ZeroMemory(key);}
    }
}
