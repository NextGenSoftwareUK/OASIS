using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow("avatar")][DataRow("avatar-detail")][DataRow("holon")]
    public async Task NativeCheckedHistoryPersistsEncryptedArchiveWithoutPublishingCaller(string kind)
    {
        var (provider,address)=await CreateActivatedProviderAsync();
        try {
            if(kind=="avatar")await VerifyNativeHistory(provider,address,kind,new Avatar {Id=Guid.NewGuid(),Username="native-private-marker"});
            else if(kind=="avatar-detail")await VerifyNativeHistory(provider,address,kind,new AvatarDetail {Id=Guid.NewGuid(),Username="native-private-marker"});
            else await VerifyNativeHistory(provider,address,kind,new Core.Holons.Holon {Id=Guid.NewGuid(),Name="native-private-marker",Children=new List<IHolon>{new Core.Holons.Holon {Id=Guid.NewGuid(),Name="native-child"}}});
        }finally{await provider.DeActivateProviderAsync();}
    }

    private static async Task VerifyNativeHistory<T>(EthereumOASIS provider,string address,string kind,T source) where T:class,IHolonBase
    {
        var method=typeof(EthereumOASIS).GetMethod("UpsertStorageHistoryAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.MakeGenericMethod(typeof(T));
        Task<(TransactionReceipt Receipt,T Record)> Save(bool enabled)=>(Task<(TransactionReceipt,T)>)method.Invoke(provider,new object[]{kind,source,enabled})!;
        var first=await Save(true);Assert.AreEqual(BigInteger.One,first.Receipt.Status.Value);Assert.AreEqual(1,first.Record.Version);
        source.Description="native-second-marker";
        var second=await Save(false);Assert.AreEqual(BigInteger.One,second.Receipt.Status.Value);Assert.AreEqual(2,second.Record.Version);
        Assert.AreEqual(first.Record.VersionId,second.Record.PreviousVersionId);Assert.AreEqual(0,source.Version);Assert.AreEqual(Guid.Empty,source.VersionId);
        if(kind=="avatar"){
            provider.IsVersionControlEnabled=true;
            var saved=await provider.SaveAvatarAsync((IAvatar)source);Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(3,source.Version);
            provider.IsVersionControlEnabled=false;
            var synced=provider.SaveAvatar((IAvatar)source);Assert.IsFalse(synced.IsError,synced.Message);Assert.AreEqual(4,source.Version);
        }else if(kind=="avatar-detail"){
            provider.IsVersionControlEnabled=true;
            var saved=await provider.SaveAvatarDetailAsync((IAvatarDetail)source);Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(3,source.Version);
            provider.IsVersionControlEnabled=false;
            var synced=provider.SaveAvatarDetail((IAvatarDetail)source);Assert.IsFalse(synced.IsError,synced.Message);Assert.AreEqual(4,source.Version);
        }else{
            provider.IsVersionControlEnabled=true;
            var saved=await provider.SaveHolonAsync((IHolon)source);Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(3,source.Version);
            provider.IsVersionControlEnabled=false;
            var synced=provider.SaveHolon((IHolon)source);Assert.IsFalse(synced.IsError,synced.Message);Assert.AreEqual(4,source.Version);
            Assert.AreEqual(2,((IHolon)source).Children.Single().Version);
        }
        var service=new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        var id=new BigInteger(HashUtility.GetNumericHash(source.Id.ToString()));
        var ciphertext=kind switch {
            "avatar"=>(await service.GetAvatarByIdQueryAsync(id)).ReturnValue1.Info,
            "avatar-detail"=>(await service.GetAvatarDetailByIdQueryAsync(id)).ReturnValue1.Info,
            _=>(await service.GetHolonByIdQueryAsync(id)).ReturnValue1.Info
        };
        Assert.IsFalse(ciphertext.Contains("native-private-marker"));Assert.IsFalse(ciphertext.Contains("native-second-marker"));
        var key=PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY");
        try {
            using var codec=new EncryptedRecordCodec(key,compress:true);
            var document=JObject.Parse(codec.Decrypt("oasis/ethereum/storage/v1/"+kind+"/"+id.ToString(System.Globalization.CultureInfo.InvariantCulture),ciphertext));
            var history=new EmbeddedRecordHistory();
            var latest=await history.ReadAsync(source.Id,document,0,OasisJson.DeserializeStored<T>);
            var archived=await history.ReadAsync(source.Id,document,1,OasisJson.DeserializeStored<T>);
            Assert.AreEqual(4,latest.Version);Assert.AreEqual("native-second-marker",latest.Description);Assert.AreEqual(1,archived.Version);Assert.IsNull(archived.Description);
            if(kind=="holon"){var children=((IHolon)archived).Children;Assert.AreEqual(1,children.Count());Assert.AreEqual("native-child",children.Single().Name);}
            else {
                provider.IsVersionControlEnabled=true;
                if(kind=="avatar")Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=source.Id})).IsError);
                else Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar{Id=source.Id})).IsError);
                provider.IsVersionControlEnabled=false;
                var deleted=await provider.DeleteAvatarAsync(source.Id,softDelete:true);Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.Result);
                var avatarRow=(await service.GetAvatarByIdQueryAsync(id)).ReturnValue1;
                var detailRow=(await service.GetAvatarDetailByIdQueryAsync(id)).ReturnValue1;
                var avatarDocument=JObject.Parse(codec.Decrypt("oasis/ethereum/storage/v1/avatar/"+id.ToString(System.Globalization.CultureInfo.InvariantCulture),avatarRow.Info));
                var detailDocument=JObject.Parse(codec.Decrypt("oasis/ethereum/storage/v1/avatar-detail/"+id.ToString(System.Globalization.CultureInfo.InvariantCulture),detailRow.Info));
                var avatar=await history.ReadAsync(source.Id,avatarDocument,0,OasisJson.DeserializeStored<Avatar>);
                var detail=await history.ReadAsync(source.Id,detailDocument,0,OasisJson.DeserializeStored<AvatarDetail>);
                Assert.AreEqual(kind=="avatar"?5:2,avatar.Version);Assert.AreEqual(kind=="avatar-detail"?5:2,detail.Version);
                Assert.IsFalse(avatar.IsActive);Assert.IsFalse(detail.IsActive);Assert.AreEqual(avatar.DeletedDate,detail.DeletedDate);
                Assert.IsTrue((await history.ReadAsync(source.Id,avatarDocument,1,OasisJson.DeserializeStored<Avatar>)).IsActive);
                Assert.IsTrue((await history.ReadAsync(source.Id,detailDocument,1,OasisJson.DeserializeStored<AvatarDetail>)).IsActive);
                foreach(var historical in new[]{provider.LoadAvatar(source.Id,1),await provider.LoadAvatarAsync(source.Id,1)}){
                    Assert.IsFalse(historical.IsError,historical.Message);Assert.AreEqual(1,historical.Result.Version);Assert.IsTrue(historical.Result.IsActive);
                }
                foreach(var historical in new[]{provider.LoadAvatarDetail(source.Id,1),await provider.LoadAvatarDetailAsync(source.Id,1)}){
                    Assert.IsFalse(historical.IsError,historical.Message);Assert.AreEqual(1,historical.Result.Version);Assert.IsTrue(historical.Result.IsActive);
                }
                var missingAvatar=await provider.LoadAvatarAsync(source.Id,99);Assert.IsTrue(missingAvatar.IsError);Assert.IsNull(missingAvatar.Result);Assert.IsFalse(missingAvatar.IsLoaded);Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missingAvatar.ErrorCode);
                var missingDetail=provider.LoadAvatarDetail(source.Id,99);Assert.IsTrue(missingDetail.IsError);Assert.IsNull(missingDetail.Result);Assert.IsFalse(missingDetail.IsLoaded);Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missingDetail.ErrorCode);
                var hard=await provider.DeleteAvatarAsync(source.Id,softDelete:false);Assert.IsFalse(hard.IsError,hard.Message);Assert.IsTrue(hard.Result);
                Assert.AreEqual(BigInteger.Zero,(await service.GetAvatarByIdQueryAsync(id)).ReturnValue1.EntityId);
                Assert.AreEqual(BigInteger.Zero,(await service.GetAvatarDetailByIdQueryAsync(id)).ReturnValue1.EntityId);
            }
        }finally{CryptographicOperations.ZeroMemory(key);}
    }
}
