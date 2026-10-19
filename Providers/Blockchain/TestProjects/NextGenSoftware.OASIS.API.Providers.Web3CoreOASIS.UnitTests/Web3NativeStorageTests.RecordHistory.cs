using System.Reflection;
using System.Text;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    [TestMethod]
    public async Task ActualV2ArbitrumFactoryPreservesEncryptedHistoryAndOriginalFaults()
    {
        var key=Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!;
        var web3=new Nethereum.Web3.Web3(new Nethereum.Web3.Accounts.Account(key,31337),"http://127.0.0.1:18545");
        var receipt=await web3.Eth.GetContractDeploymentHandler<Deployment>().SendRequestAndWaitForReceiptAsync(new Deployment{
            Gas=new Nethereum.Hex.HexTypes.HexBigInteger(8000000)});
        Assert.AreEqual(System.Numerics.BigInteger.One,receipt.Status.Value);
        var provider=(Web3CoreOASISBaseProvider)NextGenSoftware.OASIS.OASISBootLoader.EvmProviderFactory.CreateArbitrum(
            new NextGenSoftware.OASIS.API.DNA.ArbitrumOASISProviderSettings{ConnectionString="http://127.0.0.1:18545",ChainId=31337,
                ChainPrivateKey=key,ContractAddress=receipt.ContractAddress,IsVersionControlEnabled=true},
            NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,null!);
        Assert.IsInstanceOfType(provider,typeof(ArbitrumOASIS.ArbitrumOASIS_Web3Core));Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsTrue(provider.IsVersionControlEnabled);
        try{
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
            var native=(Web3CoreOASIS)typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
            await VerifyRecordHistoryAsync(provider,native);
            await VerifyHistoryResponseLossAsync(provider);
        }finally{await provider.DeActivateProviderAsync();}
    }
    private static async Task VerifyRecordHistoryAsync(Web3CoreOASISBaseProvider provider,Web3CoreOASIS native)
    {
        var originalOption=provider.IsVersionControlEnabled;
        var id=Guid.NewGuid();var holonId=Guid.NewGuid();var childId=Guid.NewGuid();
        var avatar=new Avatar{Id=id,IsActive=true,Username="history-"+id.ToString("N"),Email=id.ToString("N")+"@example.invalid"};
        var detail=new AvatarDetail{Id=id,IsActive=true,Username=avatar.Username,Email=id.ToString("N")+"@example.invalid"};
        var root=new Holon{Id=holonId,IsActive=true,CreatedByAvatarId=id};var child=new Holon{Id=childId,IsActive=true,CreatedByAvatarId=id};root.Children.Add(child);
        try{
            provider.IsVersionControlEnabled=true;
            foreach(var row in new IHolonBase[]{avatar,detail,root,child})row.MetaData=new(){{"history-marker","native-history-first"}};
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);
            var graph=await provider.SaveHolonAsync(root,saveChildren:true,saveChildrenOnProvider:true);Assert.IsFalse(graph.IsError,graph.Message);
            foreach(var row in new IHolonBase[]{avatar,detail,root,child})Assert.AreEqual(1,row.Version);
            var firstAvatarVersion=avatar.VersionId;
            provider.IsVersionControlEnabled=false;
            foreach(var row in new IHolonBase[]{avatar,detail,root,child})row.MetaData["history-marker"]="native-history-second";
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);
            graph=await provider.SaveHolonAsync(root,saveChildren:true,saveChildrenOnProvider:true);Assert.IsFalse(graph.IsError,graph.Message);
            foreach(var row in new IHolonBase[]{avatar,detail,root,child})Assert.AreEqual(2,row.Version);
            Assert.AreEqual(firstAvatarVersion,avatar.PreviousVersionId);
            var pair=await native.ReadAvatarPairAsync((uint)HashUtility.GetNumericHash(id.ToString()));
            await VerifyNativeHistory<Avatar>(provider,"avatar",id,pair.Avatar,2,false);
            await VerifyNativeHistory<AvatarDetail>(provider,"detail",id,pair.Detail,2,false);
            await VerifyNativeHistory<Holon>(provider,"holon",holonId,await native.GetHolonByIdAsync((uint)HashUtility.GetNumericHash(holonId.ToString())),2,false);
            await VerifyNativeHistory<Holon>(provider,"holon",childId,await native.GetHolonByIdAsync((uint)HashUtility.GetNumericHash(childId.ToString())),2,false);
            var oldAvatar=await provider.LoadAvatarAsync(id,version:1);Assert.IsFalse(oldAvatar.IsError,oldAvatar.Message);Assert.AreEqual("native-history-first",oldAvatar.Result.MetaData["history-marker"]);
            var oldDetail=await provider.LoadAvatarDetailByUsernameAsync(detail.Username,version:1);Assert.IsFalse(oldDetail.IsError,oldDetail.Message);Assert.AreEqual(1,oldDetail.Result.Version);
            var oldEmail=provider.LoadAvatarDetailByEmail(detail.Email,version:1);Assert.IsFalse(oldEmail.IsError,oldEmail.Message);Assert.AreEqual(1,oldEmail.Result.Version);
            var absent=await provider.LoadAvatarAsync(id,version:99);Assert.IsTrue(absent.IsError);Assert.AreEqual(Common.OASISResultErrorCodes.RecordNotFound,absent.ErrorCode);
            var oldCollection=await provider.LoadAllAvatarsAsync(version:1);Assert.IsFalse(oldCollection.IsError,oldCollection.Message);Assert.AreEqual(1,oldCollection.Result.Single(row=>row.Id==id).Version);
            child.MetaData["history-marker"]="native-history-third";
            Assert.IsFalse((await provider.SaveHolonAsync(child,saveChildren:false)).IsError);Assert.AreEqual(3,child.Version);
            var oldGraph=await provider.LoadHolonAsync(holonId,loadChildren:true,loadChildrenFromProvider:true,version:1);
            Assert.IsFalse(oldGraph.IsError,oldGraph.Message);Assert.AreEqual(1,oldGraph.Result.Version);
            Assert.AreEqual(3,oldGraph.Result.Children.Single().Version);Assert.AreEqual("native-history-third",oldGraph.Result.Children.Single().MetaData["history-marker"]);
            var metadata=await provider.LoadHolonsByMetaDataAsync("history-marker","native-history-first",loadChildrenFromProvider:true,version:1);
            Assert.IsFalse(metadata.IsError,metadata.Message);Assert.AreEqual(1,metadata.Result.Single(row=>row.Id==holonId).Version);
            Assert.AreEqual(3,metadata.Result.Single(row=>row.Id==holonId).Children.Single().Version);
            var search=await provider.SearchAsync(new Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,
                FilterByMetaData=new(){{"history-marker","native-history-first"}}},version:1);
            Assert.IsFalse(search.IsError,search.Message);Assert.AreEqual(1,search.Result.SearchResultHolons.Single(row=>row.Id==holonId).Version);
            Assert.AreEqual(3,search.Result.SearchResultHolons.Single(row=>row.Id==holonId).Children.Single().Version);
            Assert.AreEqual(1,search.Result.SearchResultAvatars.Single(row=>row.Id==id).Version);
            var exported=await provider.ExportAllDataForAvatarByIdAsync(id,version:1);Assert.IsFalse(exported.IsError,exported.Message);
            Assert.AreEqual(1,exported.Result.Single(row=>row.Id==holonId).Version);Assert.AreEqual(3,exported.Result.Single(row=>row.Id==holonId).Children.Single().Version);
            var emailExport=provider.ExportAllDataForAvatarByEmail(avatar.Email,version:1);Assert.IsFalse(emailExport.IsError,emailExport.Message);
            Assert.AreEqual(1,emailExport.Result.Single(row=>row.Id==holonId).Version);
            var parent=await provider.LoadHolonsForParentAsync(holonId,loadChildrenFromProvider:true,version:1);
            Assert.IsFalse(parent.IsError,parent.Message);Assert.AreEqual(1,parent.Result.Single().Version);Assert.AreEqual(childId,parent.Result.Single().Id);
            var deleted=await provider.DeleteAvatarAsync(id,softDelete:true);Assert.IsFalse(deleted.IsError,deleted.Message);
            pair=await native.ReadAvatarPairAsync((uint)HashUtility.GetNumericHash(id.ToString()));
            await VerifyNativeHistory<Avatar>(provider,"avatar",id,pair.Avatar,3,true);
            await VerifyNativeHistory<AvatarDetail>(provider,"detail",id,pair.Detail,3,true);
            Assert.IsTrue((await provider.LoadAvatarAsync(id)).IsError);
            oldAvatar=await provider.LoadAvatarAsync(id,version:1);Assert.IsFalse(oldAvatar.IsError,oldAvatar.Message);Assert.AreEqual(1,oldAvatar.Result.Version);
            Assert.IsFalse((await provider.DeleteAvatarAsync(id,softDelete:false)).IsError);
            Assert.IsFalse((await provider.DeleteHolonAsync(holonId)).IsError);
            Assert.IsFalse((await provider.DeleteHolonAsync(childId)).IsError);
            pair=await native.ReadAvatarPairAsync((uint)HashUtility.GetNumericHash(id.ToString()));
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(pair.Avatar));Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(pair.Detail));
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetHolonByIdAsync((uint)HashUtility.GetNumericHash(holonId.ToString()))));
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetHolonByIdAsync((uint)HashUtility.GetNumericHash(childId.ToString()))));
        }finally{provider.IsVersionControlEnabled=originalOption;}
    }

    private static async Task VerifyNativeHistory<T>(Web3CoreOASISBaseProvider provider,string kind,Guid id,EntityOASIS native,int version,bool deleted) where T:class,IHolonBase
    {
        var current=provider.DecodePrivateRecord<T>(kind,id,native);Assert.AreEqual(version,current.Version);Assert.AreEqual(deleted,current.IsDeleted);
        Assert.IsFalse(Encoding.UTF8.GetString(native.Info).Contains("native-history-first"));
        var context=(string)typeof(Web3CoreOASISBaseProvider).GetMethod("PrivateContext",BindingFlags.Instance|BindingFlags.NonPublic)!
            .Invoke(provider,new object[]{kind,native.EntityId,native.ExternalId})!;
        var key=Convert.FromBase64String(Environment.GetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY")!);
        try{
            using var codec=new EncryptedRecordCodec(key,compress:true);
            var document=JObject.Parse(codec.Decrypt(context,Encoding.UTF8.GetString(native.Info)));
            Assert.AreEqual(version-1,((JArray)document[EmbeddedRecordHistory.PropertyName]!["records"]!).Count);
            var history=new EmbeddedRecordHistory();
            var first=await history.ReadAsync(id,document,1,OasisJson.Deserialize<T>);
            Assert.AreEqual("native-history-first",first.MetaData["history-marker"]);Assert.IsFalse(first.IsDeleted);
            if(version==3){var second=await history.ReadAsync(id,document,2,OasisJson.Deserialize<T>);Assert.AreEqual("native-history-second",second.MetaData["history-marker"]);Assert.IsFalse(second.IsDeleted);}
        }finally{CryptographicOperations.ZeroMemory(key);}
    }
}
